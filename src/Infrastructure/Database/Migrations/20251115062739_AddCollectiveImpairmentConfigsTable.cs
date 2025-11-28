using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddCollectiveImpairmentConfigsTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "collective_impairment_configs",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                parameter = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                config_json = table.Column<string>(type: "text", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_collective_impairment_configs", x => x.id));

        migrationBuilder.CreateIndex(
            name: "ix_collective_impairment_configs_parameter",
            schema: "public",
            table: "collective_impairment_configs",
            column: "parameter");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "collective_impairment_configs",
            schema: "public");
    }
}
