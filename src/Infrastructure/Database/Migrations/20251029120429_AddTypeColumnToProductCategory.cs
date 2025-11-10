using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddTypeColumnToProductCategory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_product_categories_name",
            schema: "public",
            table: "product_categories");

        migrationBuilder.AddColumn<string>(
            name: "type",
            schema: "public",
            table: "product_categories",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "ix_product_categories_type_name",
            schema: "public",
            table: "product_categories",
            columns: ["type", "name"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_product_categories_type_name",
            schema: "public",
            table: "product_categories");

        migrationBuilder.DropColumn(
            name: "type",
            schema: "public",
            table: "product_categories");

        migrationBuilder.CreateIndex(
            name: "ix_product_categories_name",
            schema: "public",
            table: "product_categories",
            column: "name",
            unique: true);
    }
}

