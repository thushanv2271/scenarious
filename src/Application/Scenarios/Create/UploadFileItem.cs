namespace Application.Scenarios.Create;

/// <summary>
/// Represents file upload information for a scenario.
/// </summary>
public sealed record UploadFileItem(
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long Size,
    Uri Url,
    Guid UploadedBy
);
