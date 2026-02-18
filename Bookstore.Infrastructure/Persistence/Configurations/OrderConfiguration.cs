using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId)
            .IsRequired();

        // Owned Money mapping for TotalAmount
        builder.OwnsOne<Money>(nameof(Order.TotalAmount), m =>
        {
            m.Property(p => p.Amount)
                .HasColumnName("TotalAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            m.Property(p => p.Currency)
                .HasColumnName("TotalAmountCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(o => o.Status)
            .IsRequired();

        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();
        builder.Property(o => o.CreatedBy).HasMaxLength(100);
        builder.Property(o => o.UpdatedBy).HasMaxLength(100);
        builder.Property(o => o.IsDeleted).HasDefaultValue(false);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.Property(o => o.IdempotencyKey)
            .HasColumnName("IdempotencyKey")
            .HasMaxLength(100);

        builder.HasIndex(o => o.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        builder.HasIndex(o => new { o.UserId, o.Status });
        builder.HasIndex(o => o.CreatedAt);
    }
}
