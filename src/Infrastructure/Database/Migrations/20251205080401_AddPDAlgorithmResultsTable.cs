using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddPDAlgorithmResultsTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "pd_algorithm_results",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                pd_algorithm_result_data = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_pd_algorithm_results", x => x.id));

        migrationBuilder.CreateIndex(
            name: "ix_pd_algorithm_results_created_at",
            schema: "public",
            table: "pd_algorithm_results",
            column: "created_at",
            descending: Array.Empty<bool>());
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "pd_algorithm_results",
            schema: "public");
    }
}
