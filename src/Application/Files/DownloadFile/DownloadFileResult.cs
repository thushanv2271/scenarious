namespace Application.Files.DownloadFile;

public sealed record DownloadFileResult(
    string PhysicalPath,
    string OriginalFileName,
    string ContentType,
    long Size
);
