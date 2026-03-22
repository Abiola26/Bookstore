using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Infrastructure.Migrations;

/// <inheritdoc />
public partial class FilteredISBNIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Books_ISBN",
            table: "Books");

        migrationBuilder.CreateIndex(
            name: "IX_Books_ISBN",
            table: "Books",
            column: "ISBN",
            unique: true,
            filter: "\"IsDeleted\" = false");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Books_ISBN",
            table: "Books");

        migrationBuilder.CreateIndex(
            name: "IX_Books_ISBN",
            table: "Books",
            column: "ISBN",
            unique: true);
    }
}
