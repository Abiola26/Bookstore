using Bookstore.Application.Repositories;
using Bookstore.Application.Services;
using Bookstore.Infrastructure.Services;
using Bookstore.Infrastructure.Persistence.Repositories;
using Bookstore.Infrastructure.Persistence;
using Bookstore.Infrastructure.Persistence.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, bool isTesting = false)
    {
        // Database Configuration
        if (!isTesting)
        {
            services.AddSingleton<UpdateTimestampInterceptor>();

            services.AddDbContext<BookStoreDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<UpdateTimestampInterceptor>();
                options.UseNpgsql(connectionString, sqlOptions =>
                {
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
                }).AddInterceptors(interceptor);
            });
        }

        // Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderItemRepository, OrderItemRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();

        // Services
        // Password hasher
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        // JWT Provider
        services.AddSingleton<IJwtProvider, JwtProvider>();
        // Email sender (SMTP with fallback to logging)
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        // File storage
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        // Rate limit service (uses IDistributedCache)
        services.AddScoped<IRateLimitService, DistributedRateLimitService>();
        services.AddHttpClient<IPaystackService, PaystackService>();

        return services;
    }
}
