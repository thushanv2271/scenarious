namespace Domain.Files;

/// <summary>
/// Represents a file that has been uploaded, including metadata such as its original name, storage details, and upload
/// information .
/// </summary>
/// <remarks>This class provides metadata about an uploaded file, including its original and stored file names,
/// MIME type, size, and paths for physical storage and public access.  It also includes information about the user who
/// uploaded the file and the timestamp of the upload.</remarks>
public sealed class UploadedFile
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string OriginalFileName { get; init; }
    public required string StoredFileName { get; init; }
    public required string ContentType { get; init; }
    public long Size { get; init; }

    public required string PhysicalPath { get; init; }
    public required string PublicUrl { get; init; }

    public Guid UploadedBy { get; init; }
    public DateTimeOffset UploadedAt { get; init; } = DateTimeOffset.UtcNow;
}
