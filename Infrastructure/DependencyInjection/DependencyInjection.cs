using Application.Abstractions.QueryRepositories;
using Domain.Repositories;
using Domain.SeedWork;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.QueryRepositories;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<AuditableEntityInterceptor>();

            // Bu template PostgreSQL (Npgsql) için sabitlenmiştir. Farklı bir sağlayıcı gerekiyorsa
            // burayı ve ilgili NuGet paketini bilinçli olarak değiştirin.
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
                options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            });

            services.AddScoped<IUnitOfWork>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));

            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderQueryRepository, OrderQueryRepository>();

            // 5. Diğer servisler (Email vb.)
            // services.AddTransient<IEmailService, EmailService>();

            return services;
        }
    }
}
