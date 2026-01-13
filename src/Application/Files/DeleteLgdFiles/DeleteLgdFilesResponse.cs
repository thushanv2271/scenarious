namespace Application.Files.DeleteLgdFiles;

/// <summary>
/// Response for deleting LGD files.
/// </summary>
/// <param name="DeletedFiles">List of file IDs that were successfully deleted.</param>
/// <param name="FailedFiles">List of file IDs that failed to delete with error messages.</param>
/// <param name="TotalDeleted">Total number of files successfully deleted.</param>
/// <param name="TotalFailed">Total number of files that failed to delete.</param>
public sealed record DeleteLgdFilesResponse(
    List<string> DeletedFiles,
    Dictionary<string, string> FailedFiles,
    int TotalDeleted,
    int TotalFailed
);
