using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class ReplaceDpBucketWithBucketLabel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "dp_bucket",
            schema: "public",
            table: "loan_details");

        migrationBuilder.AddColumn<string>(
            name: "bucket_label",
            schema: "public",
            table: "loan_details",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "bucket_label",
            schema: "public",
            table: "loan_details");

        migrationBuilder.AddColumn<int>(
            name: "dp_bucket",
            schema: "public",
            table: "loan_details",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
