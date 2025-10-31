using System;

namespace Application.Files.UploadFile;

public sealed record UploadFileResponse(
    Guid Id,
    Uri Url,
    string StoredFileName,
    long Size
);
