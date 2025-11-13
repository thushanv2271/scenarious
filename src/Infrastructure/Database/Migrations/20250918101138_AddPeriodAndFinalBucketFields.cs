using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddPeriodAndFinalBucketFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "final_bucket",
            schema: "public",
            table: "loan_details",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "period",
            schema: "public",
            table: "file_details",
            type: "text",
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "final_bucket",
            schema: "public",
            table: "loan_details");

        migrationBuilder.DropColumn(
            name: "period",
            schema: "public",
            table: "file_details");
    }
}
