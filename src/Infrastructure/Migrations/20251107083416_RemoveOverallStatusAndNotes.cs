using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class RemoveOverallStatusAndNotes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "notes",
            schema: "public",
            table: "customer_risk_indicator_evaluations");

        migrationBuilder.DropColumn(
            name: "overall_status",
            schema: "public",
            table: "customer_risk_evaluations");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "notes",
            schema: "public",
            table: "customer_risk_indicator_evaluations",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "overall_status",
            schema: "public",
            table: "customer_risk_evaluations",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "");
    }
}
