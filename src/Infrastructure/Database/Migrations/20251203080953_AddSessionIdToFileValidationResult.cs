using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddSessionIdToFileValidationResult : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "session_id",
            schema: "public",
            table: "file_validation_results",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "session_id",
            schema: "public",
            table: "file_validation_results");
    }
}
