using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddBranchIdToUsers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "branch_id",
            schema: "public",
            table: "users",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_users_branch_id",
            schema: "public",
            table: "users",
            column: "branch_id");

        migrationBuilder.AddForeignKey(
            name: "fk_users_branches_branch_id",
            schema: "public",
            table: "users",
            column: "branch_id",
            principalSchema: "public",
            principalTable: "branches",
            principalColumn: "id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_users_branches_branch_id",
            schema: "public",
            table: "users");

        migrationBuilder.DropIndex(
            name: "ix_users_branch_id",
            schema: "public",
            table: "users");

        migrationBuilder.DropColumn(
            name: "branch_id",
            schema: "public",
            table: "users");
    }
}
