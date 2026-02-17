using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(u => u.EmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.EmailConfirmationToken)
            .HasMaxLength(500);
        builder.Property(u => u.EmailConfirmationTokenExpiresAt);

        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(500);

        builder.Property(u => u.PasswordResetTokenExpiresAt);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(UserRole.User);

        builder.HasMany(u => u.Orders)
            .WithOne(o => o.User)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();
        builder.Property(u => u.CreatedBy).HasMaxLength(100);
        builder.Property(u => u.UpdatedBy).HasMaxLength(100);
        builder.Property(u => u.IsDeleted).HasDefaultValue(false);
        builder.Property(u => u.RowVersion).IsRowVersion();
    }
}
