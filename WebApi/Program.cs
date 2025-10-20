﻿using Application.Abstractions;
using Application.DependencyInjection;
using Infrastructure.DependencyInjection;
using Integration.DependencyInjection;
using Hangfire;
using Hangfire.PostgreSql;
using Serilog;
using System.Reflection;
using WebApi.Hubs;
using WebApi.Middleware;
using WebApi.Services;

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
        .Enrich.FromLogContext() // Log Context'ten gelen bilgileri ekle
        .WriteTo.Console()); // Konsola yaz (appsettings'de de olabilir)
                             // .WriteTo.File(...) // Dosyaya yaz (appsettings'de de olabilir)

    // --- HANGFIRE KAYITLARI ---
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")))
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

    builder.Services.AddTransient<GlobalExceptionHandlingMiddleware>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors("AllowSpecificOrigins");
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSerilogRequestLogging();
    app.MapControllers();
    app.MapHub<NotificationHub>("/notification-hub");
    // Hangfire Dashboard'u /hangfire adresinde aktif et
    // TODO: Production'da buraya bir yetkilendirme filtresi eklenmelidir!
    // Örn: app.MapHangfireDashboard("/hangfire", new DashboardOptions
    // {
    //    Authorization = new [] { new MyHangfireAuthorizationFilter() }
    // });
    app.MapHangfireDashboard("/hangfire");

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

