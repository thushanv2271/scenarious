using Application.Abstractions.Messaging;

namespace Application.Files.ProcessPdFilesByIds;

/// <summary>
/// Command to process PD files by their upload IDs and store validation results in JSON
/// </summary>
public sealed record ProcessPdFilesByIdsCommand(
    List<string> UploadIds,
    string TimePeriod
) : ICommand<ProcessPdFilesByIdsResponse>;

/// <summary>
/// Response for file processing operation
/// </summary>
public sealed record ProcessPdFilesByIdsResponse(
    List<ProcessedFileInfo> ProcessedFiles,
    int TotalProcessed,
    int TotalWithErrors,
    int TotalSuccessful
);

/// <summary>
/// Information about a processed file
/// </summary>
public sealed record ProcessedFileInfo(
    string UploadId,
    string FileName,
    string Status,
    int TotalRows,
    int TotalErrors,
    bool HasErrors
);
