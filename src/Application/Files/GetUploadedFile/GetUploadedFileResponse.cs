namespace Application.Files.GetUploadedFile;

public sealed record GetUploadedFileResponse(
    Guid Id,
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long Size,
    Uri Url,
    Guid UploadedBy,
    DateTimeOffset UploadedAt
);
