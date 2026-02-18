using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bookstore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("2d2d2d1d-2d2d-2d2d-2d2d-2d2d2d2d2d2d"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Technology", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("3d3d3d1d-3d3d-3d3d-3d3d-3d3d3d3d3d3d"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Science", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("6e6f6a7b-8c9d-0e1f-2a3b-4c5d6e7f8a9b"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Fiction", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiresAt", "EmailConfirmed", "FullName", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiresAt", "PhoneNumber", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "user@bookstore.com", null, null, true, "John Doe", "$2a$11$8k7.Y8R3h2Bf8WvE1H6eNuI7F1Z1e1K1W1S1a1P1l1y1C1o1r1K1.", null, null, null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiresAt", "EmailConfirmed", "FullName", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiresAt", "PhoneNumber", "Role", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "admin@bookstore.com", null, null, true, "System Admin", "$2a$11$8k7.Y8R3h2Bf8WvE1H6eNuI7F1Z1e1K1W1S1a1P1l1y1C1o1r1K1.", null, null, null, "Admin", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });

            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "Id", "Author", "CategoryId", "CoverImageUrl", "CreatedAt", "CreatedBy", "Description", "ISBN", "Language", "Pages", "PublicationDate", "Publisher", "Title", "TotalQuantity", "UpdatedAt", "UpdatedBy", "Price", "Currency" },
                values: new object[,]
                {
                    { new Guid("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"), "F. Scott Fitzgerald", new Guid("6e6f6a7b-8c9d-0e1f-2a3b-4c5d6e7f8a9b"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Seed", "A story of wealth, love, and the American Dream.", "9780743273565", "English", 180, null, null, "The Great Gatsby", 10, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 15.99m, "USD" },
                    { new Guid("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"), "Robert C. Martin", new Guid("2d2d2d1d-2d2d-2d2d-2d2d-2d2d2d2d2d2d"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Seed", "A Handbook of Agile Software Craftsmanship.", "9780132350884", "English", 464, null, null, "Clean Code", 5, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 45.50m, "USD" },
                    { new Guid("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"), "Stephen Hawking", new Guid("3d3d3d1d-3d3d-3d3d-3d3d-3d3d3d3d3d3d"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Seed", "From the Big Bang to Black Holes.", "9780553380163", "English", 212, null, null, "A Brief History of Time", 8, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 20.00m, "USD" }
                });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "IdempotencyKey", "Status", "UpdatedAt", "UpdatedBy", "UserId", "TotalAmount", "TotalAmountCurrency" },
                values: new object[] { new Guid("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Seed", "SEED-ORDER-001", 4, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"), 15.99m, "USD" });

            migrationBuilder.InsertData(
                table: "OrderItems",
                columns: new[] { "Id", "BookId", "CreatedAt", "CreatedBy", "OrderId", "Quantity", "UpdatedAt", "UpdatedBy", "UnitPrice", "UnitPriceCurrency" },
                values: new object[] { new Guid("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1"), new Guid("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Seed", new Guid("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 15.99m, "USD" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: new Guid("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"));

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: new Guid("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"));

            migrationBuilder.DeleteData(
                table: "OrderItems",
                keyColumn: "Id",
                keyValue: new Guid("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a"));

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: new Guid("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("2d2d2d1d-2d2d-2d2d-2d2d-2d2d2d2d2d2d"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("3d3d3d1d-3d3d-3d3d-3d3d-3d3d3d3d3d3d"));

            migrationBuilder.DeleteData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: new Guid("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("6e6f6a7b-8c9d-0e1f-2a3b-4c5d6e7f8a9b"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"));
        }
    }
}
