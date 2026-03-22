using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Infrastructure.Migrations;

/// <inheritdoc />
public partial class RefactorPublicationDate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "Books",
            keyColumn: "Id",
            keyValue: new Guid("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"),
            column: "PublicationDate",
            value: null);

        migrationBuilder.UpdateData(
            table: "Books",
            keyColumn: "Id",
            keyValue: new Guid("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"),
            column: "PublicationDate",
            value: null);

        migrationBuilder.UpdateData(
            table: "Books",
            keyColumn: "Id",
            keyValue: new Guid("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"),
            column: "PublicationDate",
            value: null);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "Books",
            keyColumn: "Id",
            keyValue: new Guid("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"),
            column: "PublicationDate",
            value: null);

        migrationBuilder.UpdateData(
            table: "Books",
            keyColumn: "Id",
            keyValue: new Guid("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"),
            column: "PublicationDate",
            value: null);

        migrationBuilder.UpdateData(
            table: "Books",
            keyColumn: "Id",
            keyValue: new Guid("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"),
            column: "PublicationDate",
            value: null);
    }
}
