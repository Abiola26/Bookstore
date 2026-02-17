using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Persistence.Configurations.OwnedTypeConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Infrastructure.Persistence.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Description)
            .IsRequired();

        // Map ISBN value object as string column
        builder.Property(b => b.ISBN)
            .HasConversion(v => v.ToString(), v => (Bookstore.Domain.ValueObjects.ISBN) v)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("ISBN");

        builder.HasIndex(b => b.ISBN)
            .IsUnique()
            .HasDatabaseName("IX_Books_ISBN");

        builder.Property(b => b.Publisher)
            .HasMaxLength(150);

        builder.Property(b => b.PublicationDate);

        // Owned value object mapping for Money
        builder.OwnsOne<Money>(nameof(Book.Price), m =>
        {
            m.Property(p => p.Amount)
                .HasColumnName("Price")
                .HasPrecision(18, 2)
                .IsRequired();

            m.Property(p => p.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(b => b.Author)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(b => b.Pages);

        builder.Property(b => b.Language)
            .HasMaxLength(50);

        builder.Property(b => b.CoverImageUrl);

        builder.Property(b => b.TotalQuantity)
            .IsRequired();

        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.OrderItems)
            .WithOne(oi => oi.Book)
            .HasForeignKey(oi => oi.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();

        // Audit & concurrency mappings
        builder.Property(b => b.CreatedBy).HasMaxLength(100);
        builder.Property(b => b.UpdatedBy).HasMaxLength(100);
        builder.Property(b => b.IsDeleted).HasDefaultValue(false);
        builder.Property(b => b.RowVersion).IsRowVersion();

        // Use string-based index definition for properties that use conversions
        // (EF.Property in an anonymous expression is not a valid member access for HasIndex)
        builder.HasIndex(new[] { nameof(Book.CategoryId), "ISBN" });

        builder.HasIndex(b => b.Title);
    }
}

