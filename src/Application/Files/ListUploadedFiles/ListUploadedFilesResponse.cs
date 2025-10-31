namespace Application.Files.ListUploadedFiles;

public sealed record ListUploadedFilesResponse(
    Guid Id,
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long Size,
    Uri Url,
    Guid UploadedBy,
    DateTimeOffset UploadedAt
);
