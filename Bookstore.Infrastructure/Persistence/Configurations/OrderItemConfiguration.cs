using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.OrderId).IsRequired();
        builder.Property(oi => oi.BookId).IsRequired();

        builder.Property(oi => oi.Quantity).IsRequired();

        // Owned Money mapping
        builder.OwnsOne<Money>(nameof(OrderItem.UnitPrice), m =>
        {
            m.Property(p => p.Amount)
                .HasColumnName("UnitPrice")
                .HasPrecision(18, 2)
                .IsRequired();

            m.Property(p => p.Currency)
                .HasColumnName("UnitPriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.Book)
            .WithMany(b => b.OrderItems)
            .HasForeignKey(oi => oi.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(oi => new { oi.OrderId, oi.BookId }).HasDatabaseName("IX_OrderItems_OrderId_BookId");

        builder.Property(oi => oi.CreatedAt).IsRequired();
        builder.Property(oi => oi.UpdatedAt).IsRequired();
        builder.Property(oi => oi.CreatedBy).HasMaxLength(100);
        builder.Property(oi => oi.UpdatedBy).HasMaxLength(100);
        builder.Property(oi => oi.IsDeleted).HasDefaultValue(false);
        builder.Property(oi => oi.RowVersion).IsRowVersion();
    }
}
