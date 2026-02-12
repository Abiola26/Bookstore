using Bookstore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Bookstore.Domain.ValueObjects;
using System.Linq;

namespace Bookstore.Infrastructure.Persistence;

public class BookStoreDbContext : DbContext
{
    public BookStoreDbContext(DbContextOptions<BookStoreDbContext> options) : base(options) { }

    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookStoreDbContext).Assembly);

        // Register owned types or shared configurations if needed
        // Ensure Money owned type is treated consistently where used
        modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(Money))
            .ToList();

        // Apply global query filter for soft-delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Bookstore.Domain.Entities.BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(BookStoreDbContext).GetMethod(nameof(ApplyIsDeletedQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, new object[] { modelBuilder });
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    private static void ApplyIsDeletedQueryFilter<TEntity>(ModelBuilder builder) where TEntity : Bookstore.Domain.Entities.BaseEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }
}
