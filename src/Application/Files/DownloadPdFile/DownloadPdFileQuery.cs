using Application.Abstractions.Messaging;

namespace Application.Files.DownloadPdFile;

/// <summary>
/// Query to download a PD file by upload ID with error indicators in CSV
/// </summary>
public sealed record DownloadPdFileQuery(
    string UploadId,
    string TimePeriod
) : IQuery<DownloadPdFileResponse>;

/// <summary>
/// Response containing file data for download
/// </summary>
public sealed record DownloadPdFileResponse(
    string FileName,
    string ContentType,
    byte[] Content,
    bool HasErrors,
    int TotalErrors
);
