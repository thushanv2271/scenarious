using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class SyncAfterPipelineImplementation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // This migration was intended to sync after pipeline implementation but 
        // all tables and relationships already exist from previous migrations.
        // Making this a no-op to avoid conflicts.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No-op migration - nothing to rollback
    }
}
