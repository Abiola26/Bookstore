using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bookstore.Infrastructure.Migrations;

/// <inheritdoc />
public partial class ReseedWithAdminOnlyFixed : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Seed Categories
        migrationBuilder.InsertData(
            table: "Categories",
            columns: new[] { "Id", "CreatedAt", "CreatedBy", "Name", "UpdatedAt", "UpdatedBy", "IsDeleted" },
            values: new object[,]
            {
                { new Guid("6e6f6a7b-8c9d-0e1f-2a3b-4c5d6e7f8a9b"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Fiction", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false },
                { new Guid("2d2d2d1d-2d2d-2d2d-2d2d-2d2d2d2d2d2d"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Technology", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false },
                { new Guid("3d3d3d1d-3d3d-3d3d-3d3d-3d3d3d3d3d3d"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Science", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false }
            });

        // Seed Admin User with correct password hash
        migrationBuilder.InsertData(
            table: "Users",
            columns: new[] { "Id", "CreatedAt", "CreatedBy", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiresAt", "EmailConfirmed", "FullName", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiresAt", "PhoneNumber", "Role", "UpdatedAt", "UpdatedBy", "IsDeleted" },
            values: new object[] { new Guid("d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "admin@bookstore.com", null, null, true, "System Admin", "$2a$11$TYzEUYgUCR2I.TDbTxrBvuKrLZ3e6mNKKmV2oKSELgKzKTMHB.K7y", null, null, null, "Admin", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false });

        // Seed Books
        migrationBuilder.InsertData(
            table: "Books",
            columns: new[] { "Id", "Author", "AverageRating", "CategoryId", "CoverImageUrl", "CreatedAt", "CreatedBy", "Description", "ISBN", "Language", "Pages", "PublicationDate", "Publisher", "ReviewCount", "Title", "TotalQuantity", "UpdatedAt", "UpdatedBy", "IsDeleted", "Price", "Currency" },
            values: new object[,]
            {
                { new Guid("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"), "F. Scott Fitzgerald", 0m, new Guid("6e6f6a7b-8c9d-0e1f-2a3b-4c5d6e7f8a9b"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Seed", "A story of wealth, love, and the American Dream.", "9780743273565", "English", 180, null, null, 0, "The Great Gatsby", 10, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, 15.99m, "USD" },
                { new Guid("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"), "Robert C. Martin", 0m, new Guid("2d2d2d1d-2d2d-2d2d-2d2d-2d2d2d2d2d2d"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Seed", "A Handbook of Agile Software Craftsmanship.", "9780132350884", "English", 464, null, null, 0, "Clean Code", 5, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, 45.50m, "USD" },
                { new Guid("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"), "Stephen Hawking", 0m, new Guid("3d3d3d1d-3d3d-3d3d-3d3d-3d3d3d3d3d3d"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Seed", "From the Big Bang to Black Holes.", "9780553380163", "English", 212, null, null, 0, "A Brief History of Time", 8, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, 20.00m, "USD" }
            });

        // Seed Order
        migrationBuilder.InsertData(
            table: "Orders",
            columns: new[] { "Id", "CreatedAt", "CreatedBy", "IdempotencyKey", "Status", "UpdatedAt", "UpdatedBy", "UserId", "TotalAmount", "TotalAmountCurrency", "IsDeleted" },
            values: new object[] { new Guid("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Seed", "SEED-ORDER-001", 4, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a"), 15.99m, "USD", false });

        // Seed OrderItem
        migrationBuilder.InsertData(
            table: "OrderItems",
            columns: new[] { "Id", "BookId", "CreatedAt", "CreatedBy", "OrderId", "Quantity", "UpdatedAt", "UpdatedBy", "UnitPrice", "UnitPriceCurrency", "IsDeleted" },
            values: new object[] { new Guid("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1"), new Guid("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Seed", new Guid("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 15.99m, "USD", false });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Delete all seed data in reverse order
        migrationBuilder.DeleteData(
            table: "OrderItems",
            keyColumn: "Id",
            keyValue: new Guid("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1"));

        migrationBuilder.DeleteData(
            table: "Orders",
            keyColumn: "Id",
            keyValue: new Guid("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1"));

        migrationBuilder.DeleteData(
            table: "Books",
            keyColumn: "Id",
            keyValue: new Guid("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"));

        migrationBuilder.DeleteData(
            table: "Books",
            keyColumn: "Id",
            keyValue: new Guid("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"));

        migrationBuilder.DeleteData(
            table: "Books",
            keyColumn: "Id",
            keyValue: new Guid("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"));

        migrationBuilder.DeleteData(
            table: "Users",
            keyColumn: "Id",
            keyValue: new Guid("d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a"));

        migrationBuilder.DeleteData(
            table: "Categories",
            keyColumn: "Id",
            keyValue: new Guid("6e6f6a7b-8c9d-0e1f-2a3b-4c5d6e7f8a9b"));

        migrationBuilder.DeleteData(
            table: "Categories",
            keyColumn: "Id",
            keyValue: new Guid("2d2d2d1d-2d2d-2d2d-2d2d-2d2d2d2d2d2d"));

        migrationBuilder.DeleteData(
            table: "Categories",
            keyColumn: "Id",
            keyValue: new Guid("3d3d3d1d-3d3d-3d3d-3d3d-3d3d3d3d3d3d"));
    }
}

