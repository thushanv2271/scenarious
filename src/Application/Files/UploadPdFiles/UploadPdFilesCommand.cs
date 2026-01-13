using Application.Abstractions.Messaging;

namespace Application.Files.UploadPdFiles;

/// <summary>
/// Command to upload PD files and store them in the collective_impairment_configs JSON
/// </summary>
public sealed record UploadPdFilesCommand(
    Guid UploadedBy,
    List<PdFileUploadData> Files,
    string TimePeriod
) : ICommand<UploadPdFilesResponse>;

/// <summary>
/// File upload data
/// </summary>
public sealed record PdFileUploadData(
    string FileName,
    string ContentType,
    byte[] Content
);

/// <summary>
/// Response for file upload operation - MATCHES POST /files response format
/// </summary>
public sealed record UploadPdFilesResponse(
    IReadOnlyList<UploadedFileInfo> UploadedFiles,
    int TotalFiles,
    long TotalSize
);

/// <summary>
/// Information about an uploaded file - MATCHES POST /files response format
/// </summary>
public sealed record UploadedFileInfo(
    Guid Id,
    Uri Url,
    string StoredFileName,
    string OriginalFileName,
    long Size
);
