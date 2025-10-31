using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddOrganizationTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "organizations",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                contact_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                address = table.Column<string>(type: "text", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_organizations", x => x.id));

        migrationBuilder.CreateIndex(
            name: "IX_Organizations_Code",
            schema: "public",
            table: "organizations",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Organizations_Email",
            schema: "public",
            table: "organizations",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Organizations_IsActive",
            schema: "public",
            table: "organizations",
            column: "is_active");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "organizations",
            schema: "public");
    }
}
