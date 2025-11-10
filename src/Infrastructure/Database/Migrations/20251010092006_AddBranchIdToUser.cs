using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddBranchIdToUser : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_users_branches_branch_id",
            schema: "public",
            table: "users");

        migrationBuilder.AddForeignKey(
            name: "fk_users_branches_branch_id",
            schema: "public",
            table: "users",
            column: "branch_id",
            principalSchema: "public",
            principalTable: "branches",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_users_branches_branch_id",
            schema: "public",
            table: "users");

        migrationBuilder.AddForeignKey(
            name: "fk_users_branches_branch_id",
            schema: "public",
            table: "users",
            column: "branch_id",
            principalSchema: "public",
            principalTable: "branches",
            principalColumn: "id");
    }
}
