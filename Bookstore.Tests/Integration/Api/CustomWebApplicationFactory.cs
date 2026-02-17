using Bookstore.Infrastructure;
using Bookstore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bookstore.Tests.Integration.Api;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Aggressively remove all EF Core related services to avoid provider conflicts
            var efDescriptors = services.Where(d =>
                d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                d.ImplementationType?.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true).ToList();

            foreach (var d in efDescriptors)
            {
                services.Remove(d);
            }

            // Also specifically remove the DbContext and its options
            services.RemoveAll(typeof(DbContextOptions<BookStoreDbContext>));
            services.RemoveAll(typeof(BookStoreDbContext));

            // Re-add the Infrastructure services (except the DbContext which we override)
            // Wait, we still need repositories and services. 
            // Since we removed everything starting with Microsoft.EntityFrameworkCore, 
            // we might have removed things we need? No, repositories are Bookstore.Infrastructure.

            // Add DbContext using an in-memory database for testing.
            services.AddDbContext<BookStoreDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Build the service provider.
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context (BookStoreDbContext).
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<BookStoreDbContext>();

                // Ensure the database is created.
                db.Database.EnsureCreated();
            }
        });
    }
}
