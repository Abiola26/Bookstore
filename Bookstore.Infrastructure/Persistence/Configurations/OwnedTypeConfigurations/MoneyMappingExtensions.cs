using System;
using System.Linq.Expressions;
using Bookstore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Infrastructure.Persistence.Configurations.OwnedTypeConfigurations;

public static class MoneyMappingExtensions
{
    // Reusable mapping for Money value object as an owned type
    public static void OwnsMoney<TEntity>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, Money>> navigation) where TEntity : class
    {
        builder.OwnsOne(navigation, m =>
        {
            m.Property(p => p.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            m.Property(p => p.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });
    }
}
