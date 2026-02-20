using Bookstore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Infrastructure.Persistence.Configurations;

public class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Id)
            .ValueGeneratedNever();

        builder.Property(sc => sc.UserId)
            .IsRequired();

        builder.Property(sc => sc.LastModified)
            .IsRequired();

        builder.Property(sc => sc.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(sc => sc.CreatedAt)
            .IsRequired();

        builder.Property(sc => sc.UpdatedAt)
            .IsRequired();

        // Configure Money owned type for TotalPrice
        builder.OwnsOne(sc => sc.TotalPrice, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("TotalPrice_Amount")
                .HasPrecision(19, 2)
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("TotalPrice_Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Configure relationships
        builder.HasOne(sc => sc.User)
            .WithMany()
            .HasForeignKey(sc => sc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(sc => sc.Items)
            .WithOne(sci => sci.ShoppingCart)
            .HasForeignKey(sci => sci.ShoppingCartId)
            .OnDelete(DeleteBehavior.Cascade);

        // Create unique constraint on UserId to ensure one cart per user
        builder.HasIndex(sc => sc.UserId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.ToTable("ShoppingCarts");
    }
}

public class ShoppingCartItemConfiguration : IEntityTypeConfiguration<ShoppingCartItem>
{
    public void Configure(EntityTypeBuilder<ShoppingCartItem> builder)
    {
        builder.HasKey(sci => sci.Id);

        builder.Property(sci => sci.Id)
            .ValueGeneratedNever();

        builder.Property(sci => sci.ShoppingCartId)
            .IsRequired();

        builder.Property(sci => sci.BookId)
            .IsRequired();

        builder.Property(sci => sci.Quantity)
            .IsRequired();

        builder.Property(sci => sci.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(sci => sci.CreatedAt)
            .IsRequired();

        builder.Property(sci => sci.UpdatedAt)
            .IsRequired();

        // Configure Money owned type for UnitPrice
        builder.OwnsOne(sci => sci.UnitPrice, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("UnitPrice_Amount")
                .HasPrecision(19, 2)
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("UnitPrice_Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Configure relationships
        builder.HasOne(sci => sci.ShoppingCart)
            .WithMany(sc => sc.Items)
            .HasForeignKey(sci => sci.ShoppingCartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sci => sci.Book)
            .WithMany()
            .HasForeignKey(sci => sci.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        // Create unique constraint on ShoppingCartId and BookId
        builder.HasIndex(sci => new { sci.ShoppingCartId, sci.BookId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.ToTable("ShoppingCartItems");
    }
}
