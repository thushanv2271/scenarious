namespace Web.Api.Endpoints.Scenarios;

public sealed record UploadFileRequest(
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long Size,
    Uri Url,
    Guid UploadedBy
);
