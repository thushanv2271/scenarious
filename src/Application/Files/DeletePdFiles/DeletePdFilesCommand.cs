using Application.Abstractions.Messaging;

namespace Application.Files.DeletePdFiles;

/// <summary>
/// Command to delete PD files by upload IDs
/// </summary>
public sealed record DeletePdFilesCommand(
    List<string> UploadIds,
    string TimePeriod,
    Guid DeletedBy
) : ICommand<DeletePdFilesResponse>;

/// <summary>
/// Response for delete operation
/// </summary>
public sealed record DeletePdFilesResponse(
    List<string> DeletedFiles,
    Dictionary<string, string> FailedFiles,
    int TotalDeleted,
    int TotalFailed
);
