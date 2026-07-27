using Application.Common;
using Application.DependencyInjection;
using Infrastructure.DependencyInjection;
using Integration.DependencyInjection;
using Hangfire;
using Hangfire.PostgreSql;
using Serilog;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using WebApi.Contracts;
using WebApi.Filters;
using WebApi.Hubs;
using WebApi.Middleware;
using WebApi.Resources;
using WebApi.Services;
using Application.Abstractions.Services;

// Serilog'un kendi iç hatalarını (ör. Elasticsearch sink'i cluster'a bağlanamıyorsa) stderr'e yazdır.
// Bunsuz, bir sink sessizce loglamayı bırakabilir ve fark etmek çok zor olur (bkz. Structure.md).
Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine($"[Serilog SelfLog] {msg}"));

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    // .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day) // Günlük dosyalara loglama
    // Daha gelişmiş yapılandırma için appsettings.json kullanılabilir (aşağıda)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var corsSettings = builder.Configuration.GetSection("CorsSettings");
    var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? new string[0]; // Null kontrolü

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigins", // Politikaya bir isim veriyoruz
            policyBuilder =>
            {
                if (allowedOrigins.Any()) // Eğer appsettings'de origin tanımlanmışsa
                {
                    policyBuilder.WithOrigins(allowedOrigins)
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                }
                else if (builder.Environment.IsDevelopment())
                {
                    policyBuilder.AllowAnyOrigin()
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                }
            });
    });

    // --- Serilog'u ASP.NET Core loglama sistemine entegre et ---
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration) // appsettings.json'dan oku
        .ReadFrom.Services(services) // DI servislerini kullan (örn: IHttpContextAccessor)
        .Enrich.FromLogContext() // Log Context'ten gelen bilgileri ekle (CorrelationId dahil)
        .WriteTo.Console()); // Konsola yaz (appsettings'de de olabilir)
                             // .WriteTo.File(...) / .WriteTo.Elasticsearch(...) appsettings'de tanımlı

    // --- HANGFIRE KAYITLARI ---
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("HangfireConnection")))
    );
    builder.Services.AddHangfireServer();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddIntegrationServices(builder.Configuration);
    builder.Services.AddDistributedMemoryCache();
    #region Redis Info
    // Production için (Redis'e geçmek istersen):
    // 1. Microsoft.Extensions.Caching.StackExchangeRedis paketini ekle
    // 2. builder.Services.AddDistributedMemoryCache(); satırını comment'le
    // 3. builder.Services.AddStackExchangeRedisCache(options =>
    //    {
    //        options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    //        options.InstanceName = "ProjectBase_"; // Cache key'lerine ön ek
    //    });
    // 4. appsettings.json'a "RedisConnection": "localhost:6379" gibi bir connection string ekle.
    #endregion
    builder.Services.AddSignalR();
    builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

    // --- LOCALIZATION (i18n) ---
    // Handler/domain kodu asla dile özel string üretmemeli; sadece Application.Common.ErrorCodes
    // döndürmeli. Gerçek çeviri burada, tek bir yerde, WebApi/Resources/SharedResource.*.resx
    // üzerinden yapılır. Bkz. GlobalExceptionHandlingMiddleware.
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

    var supportedCultures = new[] { new CultureInfo("tr"), new CultureInfo("en") };
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.DefaultRequestCulture = new RequestCulture("tr");
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;
        // Accept-Language header'ı otomatik olarak devreye girer (varsayılan provider'lardan biri).
    });

    // --- AUTHENTICATION / AUTHORIZATION ---
    // appsettings'deki Auth:Authority / Auth:ApiName daha önce tanımlıydı ama hiç kullanılmıyordu.
    // Artık gerçekten bir JWT Bearer şemasına bağlanıyor.
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Auth:Authority"];
            options.Audience = builder.Configuration["Auth:ApiName"];
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

            options.Events = new JwtBearerEvents
            {
                // Varsayılan davranış boş gövdeli 401/403 döner; GlobalExceptionHandlingMiddleware
                // ile aynı ErrorResponse şeklini burada da üretiyoruz ki client tek bir hata
                // sözleşmesiyle uğraşsın.
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    await WriteAuthErrorResponseAsync(context.HttpContext, HttpStatusCode.Unauthorized, ErrorCodes.Unauthorized);
                },
                OnForbidden = async context =>
                {
                    await WriteAuthErrorResponseAsync(context.HttpContext, HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        // Güvenli varsayılan: yeni bir controller/endpoint eklenip [Authorize] konması unutulursa
        // sessizce herkese açık kalmasın. İstisnalar [AllowAnonymous] ile açıkça işaretlenmeli.
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ProjectBase API", Version = "v1" }); // İsteğe bağlı API başlığı

        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        // Eğer Application katmanındaki DTO'larda da yorumlar varsa, onun XML dosyasını da ekleyebilirsiniz:
        var appXmlFilename = $"{typeof(Application.DependencyInjection.DependencyInjection).Assembly.GetName().Name}.xml";
        var appXmlPath = Path.Combine(AppContext.BaseDirectory, appXmlFilename);
        if (File.Exists(appXmlPath))
        {
            options.IncludeXmlComments(appXmlPath);
        }
        // Gerekirse Domain için de eklenebilir.
    });

    builder.Services.AddTransient<CorrelationIdMiddleware>();
    builder.Services.AddTransient<GlobalExceptionHandlingMiddleware>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    // En dışta: her isteğin bir correlation id'si olsun ve tüm loglara/hata response'larına
    // bu id eklensin (auth/hata dahil, o yüzden ikisinden de önce).
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseCors("AllowSpecificOrigins");
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    app.UseRequestLocalization();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSerilogRequestLogging();
    app.MapControllers();
    app.MapHub<NotificationHub>("/notification-hub");
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static async Task WriteAuthErrorResponseAsync(HttpContext httpContext, HttpStatusCode statusCode, string errorCode)
{
    var localizer = httpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
    var traceId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var id)
        ? id?.ToString() ?? httpContext.TraceIdentifier
        : httpContext.TraceIdentifier;

    var response = new ErrorResponse(
        Success: false,
        ErrorCode: errorCode,
        Message: localizer[errorCode].Value,
        TraceId: traceId,
        StatusCode: (int)statusCode);

    httpContext.Response.StatusCode = (int)statusCode;
    httpContext.Response.ContentType = "application/json";
    await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    }));
}
