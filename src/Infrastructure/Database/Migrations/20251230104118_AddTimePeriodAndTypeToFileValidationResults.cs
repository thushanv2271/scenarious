using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddTimePeriodAndTypeToFileValidationResults : Migration
{
    private static readonly string[] Columns = ["time_period", "collective_impairment_type"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "collective_impairment_type",
            schema: "public",
            table: "file_validation_results",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "time_period",
            schema: "public",
            table: "file_validation_results",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_file_validation_results_filename",
            schema: "public",
            table: "file_validation_results",
            column: "filename");

        migrationBuilder.CreateIndex(
            name: "ix_file_validation_results_status",
            schema: "public",
            table: "file_validation_results",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_file_validation_results_time_period_type",
            schema: "public",
            table: "file_validation_results",
            columns: Columns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_file_validation_results_filename",
            schema: "public",
            table: "file_validation_results");

        migrationBuilder.DropIndex(
            name: "ix_file_validation_results_status",
            schema: "public",
            table: "file_validation_results");

        migrationBuilder.DropIndex(
            name: "ix_file_validation_results_time_period_type",
            schema: "public",
            table: "file_validation_results");

        migrationBuilder.DropColumn(
            name: "collective_impairment_type",
            schema: "public",
            table: "file_validation_results");

        migrationBuilder.DropColumn(
            name: "time_period",
            schema: "public",
            table: "file_validation_results");
    }
}
