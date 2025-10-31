namespace Application.Files.UploadFile;

public sealed class FileStorageOptions
{
    public required string RootPath { get; init; }
    public string? PublicBaseUrl { get; init; }
    public string? RequestPath { get; init; } // e.g. "/static/uploads"
}
