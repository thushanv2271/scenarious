using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class RenameUploadedFilesTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "UploadedFiles",
            schema: "public",
            newName: "uploaded_files",
            newSchema: "public");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "uploaded_files",
            schema: "public",
            newName: "UploadedFiles",
            newSchema: "public");
    }
}
