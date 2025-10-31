namespace Application.Scenarios.GetAll;

/// <summary>
/// Uploaded file information
/// </summary>
public sealed record UploadedFileInfo
{
    public Guid Id { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string StoredFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Url { get; init; } = string.Empty;
    public Guid UploadedBy { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
