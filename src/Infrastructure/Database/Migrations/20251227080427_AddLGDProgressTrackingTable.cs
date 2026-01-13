using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddLGDProgressTrackingTable : Migration
{
    private static readonly string[] SessionStepSubtaskColumns = { "session_id", "step_order", "sub_task_order" };
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "lgd_progress_tracking",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                session_id = table.Column<Guid>(type: "uuid", nullable: false),
                step_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                step_order = table.Column<int>(type: "integer", nullable: false),
                sub_task_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                sub_task_order = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_lgd_progress_tracking", x => x.id));

        migrationBuilder.CreateIndex(
            name: "ix_lgd_progress_tracking_is_active",
            schema: "public",
            table: "lgd_progress_tracking",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_lgd_progress_tracking_session_id",
            schema: "public",
            table: "lgd_progress_tracking",
            column: "session_id");

        migrationBuilder.CreateIndex(
            name: "ix_lgd_progress_tracking_session_step_subtask",
            schema: "public",
            table: "lgd_progress_tracking",
            columns: SessionStepSubtaskColumns);

        migrationBuilder.CreateIndex(
            name: "ix_lgd_progress_tracking_status",
            schema: "public",
            table: "lgd_progress_tracking",
            column: "status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "lgd_progress_tracking",
            schema: "public");
    }
}

