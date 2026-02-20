using Bookstore.Application.Repositories;
using Bookstore.Application.Services;
using Bookstore.Infrastructure.Services;
using Bookstore.Infrastructure.Persistence.Repositories;
using Bookstore.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Database - PostgreSQL Configuration
        services.AddDbContext<BookStoreDbContext>(options =>
            options.UseNpgsql(connectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(30);
                sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
            }));

        // Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderItemRepository, OrderItemRepository>();

        // Services
        // Password hasher
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        // Email sender (SMTP with fallback to logging)
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        // File storage
        services.AddScoped<Bookstore.Application.Services.IFileStorageService, Bookstore.Infrastructure.Services.LocalFileStorageService>();
        // Rate limit service (uses IDistributedCache)
        services.AddScoped<Bookstore.Application.Services.IRateLimitService, Bookstore.Infrastructure.Services.DistributedRateLimitService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IShoppingCartService, ShoppingCartService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
