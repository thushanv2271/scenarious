namespace Application.Files.UploadLgdFiles;

public sealed record UploadLgdFilesResponse(
    IReadOnlyList<LgdUploadedFileInfo> UploadedFiles,
    int TotalFiles,
    long TotalSize,
    string Year,
    string FacilityStatus
);

public sealed record LgdUploadedFileInfo(
    string Id,
    string Name,
    string Size,
    string Type,
    string Status,
    bool IsValidated,
    FileValidationResult? ValidationResult,
    Guid UploadId,
    string StoredFileName,
    DateTimeOffset UploadedAt
);

/// <summary>
/// Validation result information for a file
/// </summary>
public sealed record FileValidationResult(
    int TotalRows,
    int TotalErrors,
    string Status
);
