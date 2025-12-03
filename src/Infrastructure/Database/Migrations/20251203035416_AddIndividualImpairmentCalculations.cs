using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddIndividualImpairmentCalculations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "individual_impairment_calculations",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                facility_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                customer_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                calculation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                interest_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                amortized_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                sum_of_pv_of_cash_flows = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                impairment_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                impairment_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                scenario_details_json = table.Column<string>(type: "jsonb", nullable: false),
                calculated_by = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_individual_impairment_calculations", x => x.id));

        migrationBuilder.CreateIndex(
            name: "ix_individual_impairment_calculations_calculation_date",
            schema: "public",
            table: "individual_impairment_calculations",
            column: "calculation_date");

        migrationBuilder.CreateIndex(
            name: "ix_individual_impairment_calculations_customer_date",
            schema: "public",
            table: "individual_impairment_calculations",
            columns: ["customer_number", "calculation_date"]);

        migrationBuilder.CreateIndex(
            name: "ix_individual_impairment_calculations_customer_number",
            schema: "public",
            table: "individual_impairment_calculations",
            column: "customer_number");

        migrationBuilder.CreateIndex(
            name: "ix_individual_impairment_calculations_facility_number",
            schema: "public",
            table: "individual_impairment_calculations",
            column: "facility_number");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "individual_impairment_calculations",
            schema: "public");
    }
}
