using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddImparements : Migration
{
    private static readonly string[] columns = new[] { "segment_id", "scenario_name" };

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "product_categories",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_product_categories", x => x.id));

        migrationBuilder.CreateTable(
            name: "segments",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                product_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_segments", x => x.id);
                table.ForeignKey(
                    name: "fk_segments_product_categories_product_category_id",
                    column: x => x.product_category_id,
                    principalSchema: "public",
                    principalTable: "product_categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "scenarios",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                segment_id = table.Column<Guid>(type: "uuid", nullable: false),
                scenario_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                probability = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                contractual_cash_flows_enabled = table.Column<bool>(type: "boolean", nullable: false),
                last_quarter_cash_flows_enabled = table.Column<bool>(type: "boolean", nullable: false),
                other_cash_flows_enabled = table.Column<bool>(type: "boolean", nullable: false),
                collateral_value_enabled = table.Column<bool>(type: "boolean", nullable: false),
                uploaded_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_scenarios", x => x.id);
                table.ForeignKey(
                    name: "fk_scenarios_segments_segment_id",
                    column: x => x.segment_id,
                    principalSchema: "public",
                    principalTable: "segments",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_scenarios_uploaded_files_uploaded_file_id",
                    column: x => x.uploaded_file_id,
                    principalSchema: "public",
                    principalTable: "UploadedFiles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "ix_product_categories_name",
            schema: "public",
            table: "product_categories",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_scenarios_segment_id",
            schema: "public",
            table: "scenarios",
            column: "segment_id");

        migrationBuilder.CreateIndex(
            name: "ix_scenarios_segment_id_scenario_name",
            schema: "public",
            table: "scenarios",
            columns: columns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_scenarios_uploaded_file_id",
            schema: "public",
            table: "scenarios",
            column: "uploaded_file_id");

        migrationBuilder.CreateIndex(
            name: "ix_segments_product_category_id",
            schema: "public",
            table: "segments",
            column: "product_category_id");

        migrationBuilder.CreateIndex(
            name: "ix_segments_product_category_id_name",
            schema: "public",
            table: "segments",
            columns: ["product_category_id", "name"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "scenarios",
            schema: "public");

        migrationBuilder.DropTable(
            name: "segments",
            schema: "public");

        migrationBuilder.DropTable(
            name: "product_categories",
            schema: "public");
    }
}
