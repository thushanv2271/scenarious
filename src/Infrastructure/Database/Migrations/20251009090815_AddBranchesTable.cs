using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddBranchesTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "branches",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                branch_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                branch_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                contact_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                address = table.Column<string>(type: "TEXT", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_branches", x => x.id);
                table.ForeignKey(
                    name: "fk_branches_organizations_organization_id",
                    column: x => x.organization_id,
                    principalSchema: "public",
                    principalTable: "organizations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_branches_branch_code",
            schema: "public",
            table: "branches",
            column: "branch_code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_branches_is_active",
            schema: "public",
            table: "branches",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_branches_organization_id",
            schema: "public",
            table: "branches",
            column: "organization_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "branches",
            schema: "public");
    }
}
