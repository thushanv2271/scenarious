namespace Application.Scenarios.Create;

public sealed record UploadedFileDetailResponse(
    Guid Id,
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long Size,
    Uri Url,  // Changed from string to Uri
    Guid UploadedBy,
    DateTimeOffset UploadedAt
);
