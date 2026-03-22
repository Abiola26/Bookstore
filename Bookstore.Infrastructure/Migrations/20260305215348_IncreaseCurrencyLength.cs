using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Infrastructure.Migrations;

/// <inheritdoc />
public partial class IncreaseCurrencyLength : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "TotalPrice_Currency",
            table: "ShoppingCarts",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(3)",
            oldMaxLength: 3);

        migrationBuilder.AlterColumn<string>(
            name: "UnitPrice_Currency",
            table: "ShoppingCartItems",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(3)",
            oldMaxLength: 3);

        migrationBuilder.AlterColumn<string>(
            name: "TotalAmountCurrency",
            table: "Orders",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(3)",
            oldMaxLength: 3);

        migrationBuilder.AlterColumn<string>(
            name: "UnitPriceCurrency",
            table: "OrderItems",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(3)",
            oldMaxLength: 3);

        migrationBuilder.AlterColumn<string>(
            name: "Currency",
            table: "Books",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(3)",
            oldMaxLength: 3);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "TotalPrice_Currency",
            table: "ShoppingCarts",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(10)",
            oldMaxLength: 10);

        migrationBuilder.AlterColumn<string>(
            name: "UnitPrice_Currency",
            table: "ShoppingCartItems",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(10)",
            oldMaxLength: 10);

        migrationBuilder.AlterColumn<string>(
            name: "TotalAmountCurrency",
            table: "Orders",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(10)",
            oldMaxLength: 10);

        migrationBuilder.AlterColumn<string>(
            name: "UnitPriceCurrency",
            table: "OrderItems",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(10)",
            oldMaxLength: 10);

        migrationBuilder.AlterColumn<string>(
            name: "Currency",
            table: "Books",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(10)",
            oldMaxLength: 10);
    }
}
