using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddRiskTables2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "customer_risk_evaluations",
            schema: "public",
            columns: table => new
            {
                evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                evaluation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                evaluated_by = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_customer_risk_evaluations", x => x.evaluation_id));

        migrationBuilder.CreateTable(
            name: "risk_indicators",
            schema: "public",
            columns: table => new
            {
                indicator_id = table.Column<Guid>(type: "uuid", nullable: false),
                category = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                possible_values = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Yes,No,N/A"),
                display_order = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_risk_indicators", x => x.indicator_id));

        migrationBuilder.CreateTable(
            name: "customer_risk_indicator_evaluations",
            schema: "public",
            columns: table => new
            {
                eval_detail_id = table.Column<Guid>(type: "uuid", nullable: false),
                evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                indicator_id = table.Column<Guid>(type: "uuid", nullable: false),
                value = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_customer_risk_indicator_evaluations", x => x.eval_detail_id);
                table.ForeignKey(
                    name: "fk_customer_risk_indicator_evaluations_customer_risk_evaluatio",
                    column: x => x.evaluation_id,
                    principalSchema: "public",
                    principalTable: "customer_risk_evaluations",
                    principalColumn: "evaluation_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_customer_risk_indicator_evaluations_risk_indicators_indicat",
                    column: x => x.indicator_id,
                    principalSchema: "public",
                    principalTable: "risk_indicators",
                    principalColumn: "indicator_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_customer_risk_evaluations_customer_date",
            schema: "public",
            table: "customer_risk_evaluations",
            columns: ["customer_number", "evaluation_date"]);

        migrationBuilder.CreateIndex(
            name: "ix_customer_risk_evaluations_customer_number",
            schema: "public",
            table: "customer_risk_evaluations",
            column: "customer_number");

        migrationBuilder.CreateIndex(
            name: "ix_customer_risk_indicator_evaluations_evaluation_id",
            schema: "public",
            table: "customer_risk_indicator_evaluations",
            column: "evaluation_id");

        migrationBuilder.CreateIndex(
            name: "ix_customer_risk_indicator_evaluations_indicator_id",
            schema: "public",
            table: "customer_risk_indicator_evaluations",
            column: "indicator_id");

        migrationBuilder.CreateIndex(
            name: "ix_customer_risk_indicator_evaluations_unique",
            schema: "public",
            table: "customer_risk_indicator_evaluations",
            columns: ["evaluation_id", "indicator_id"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_risk_indicators_category_display_order",
            schema: "public",
            table: "risk_indicators",
            columns: ["category", "display_order"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "customer_risk_indicator_evaluations",
            schema: "public");

        migrationBuilder.DropTable(
            name: "customer_risk_evaluations",
            schema: "public");

        migrationBuilder.DropTable(
            name: "risk_indicators",
            schema: "public");
    }
}
