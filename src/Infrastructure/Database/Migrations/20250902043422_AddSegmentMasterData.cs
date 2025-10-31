using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddSegmentMasterData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "segment_masters",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                segment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                sub_segments = table.Column<List<string>>(type: "text[]", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_segment_masters", x => x.id));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "segment_masters",
            schema: "public");
    }
}
