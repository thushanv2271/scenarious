using System;

namespace Application.Files.UploadFile;

public sealed record UploadFileResponse(
    IReadOnlyList<UploadedFileInfo> UploadedFiles,
    int TotalFiles,
    long TotalSize
);

public sealed record UploadedFileInfo(
    Guid Id,
    Uri Url,
    string StoredFileName,
    string OriginalFileName,
    long Size
);
