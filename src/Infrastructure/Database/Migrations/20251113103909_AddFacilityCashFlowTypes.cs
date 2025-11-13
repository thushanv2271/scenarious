using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddFacilityCashFlowTypes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "facility_cash_flow_types",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                facility_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                segment_id = table.Column<Guid>(type: "uuid", nullable: false),
                scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                cash_flow_type = table.Column<int>(type: "integer", nullable: false),
                configuration = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_facility_cash_flow_types", x => x.id);
                table.ForeignKey(
                    name: "fk_facility_cash_flow_types_scenarios",
                    column: x => x.scenario_id,
                    principalSchema: "public",
                    principalTable: "scenarios",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_facility_cash_flow_types_segments",
                    column: x => x.segment_id,
                    principalSchema: "public",
                    principalTable: "segments",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_facility_cash_flow_types_facility_number",
            schema: "public",
            table: "facility_cash_flow_types",
            column: "facility_number");

        migrationBuilder.CreateIndex(
            name: "ix_facility_cash_flow_types_is_active",
            schema: "public",
            table: "facility_cash_flow_types",
            column: "is_active",
            filter: "is_active = true");

        migrationBuilder.CreateIndex(
            name: "ix_facility_cash_flow_types_scenario_id",
            schema: "public",
            table: "facility_cash_flow_types",
            column: "scenario_id");

        migrationBuilder.CreateIndex(
            name: "ix_facility_cash_flow_types_segment_id",
            schema: "public",
            table: "facility_cash_flow_types",
            column: "segment_id");

        migrationBuilder.CreateIndex(
            name: "uq_facility_scenario_active",
            schema: "public",
            table: "facility_cash_flow_types",
            columns: ["facility_number", "scenario_id", "is_active"],
            unique: true,
            filter: "is_active = true");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "facility_cash_flow_types",
            schema: "public");
    }
}
