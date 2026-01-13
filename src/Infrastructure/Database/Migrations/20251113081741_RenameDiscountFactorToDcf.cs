using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class RenameDiscountFactorToDcf : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "discount_factor",
            schema: "public",
            table: "lgd_details",
            newName: "dcf");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "dcf",
            schema: "public",
            table: "lgd_details",
            newName: "discount_factor");
    }
}

