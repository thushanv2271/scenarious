using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddVCLgdTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "vc_lgd_file_details",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                year = table.Column<int>(type: "integer", nullable: false),
                part = table.Column<int>(type: "integer", nullable: false),
                period = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_vc_lgd_file_details", x => x.id));

        migrationBuilder.CreateTable(
            name: "vc_lgd_details",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                vc_lgd_file_details_id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                facility_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                branch = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                product_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                segment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                earning_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                nature = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                grant_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                maturity_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                interest_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                installment_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                days_past_due = table.Column<int>(type: "integer", nullable: false),
                limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                total_os = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                undisbursed_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                interest_in_suspense = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                collateral_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                collateral_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                rescheduled = table.Column<bool>(type: "boolean", nullable: false),
                restructured = table.Column<bool>(type: "boolean", nullable: false),
                no_of_times_restructured = table.Column<int>(type: "integer", nullable: false),
                upgraded_to_delinquency_bucket = table.Column<bool>(type: "boolean", nullable: false),
                individually_impaired = table.Column<bool>(type: "boolean", nullable: false),
                bucketing_in_individual_assessment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                period = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                first_npl_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                total_outstanding_as_at_first_npl_date = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                receipt_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                closure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                cashflow = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                dcf = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                discounted_cashflows = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_vc_lgd_details", x => x.id);
                table.ForeignKey(
                    name: "fk_vc_lgd_details_vc_lgd_file_details_vc_lgd_file_details_id",
                    column: x => x.vc_lgd_file_details_id,
                    principalSchema: "public",
                    principalTable: "vc_lgd_file_details",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_vc_lgd_details_vc_lgd_file_details_id",
            schema: "public",
            table: "vc_lgd_details",
            column: "vc_lgd_file_details_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "vc_lgd_details",
            schema: "public");

        migrationBuilder.DropTable(
            name: "vc_lgd_file_details",
            schema: "public");
    }
}

