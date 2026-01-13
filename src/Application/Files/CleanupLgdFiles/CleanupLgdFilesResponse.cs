namespace Application.Files.CleanupLgdFiles;

/// <summary>
/// Response for LGD files cleanup operation.
/// </summary>
/// <param name="DeletedYears">List of years that were removed.</param>
/// <param name="TotalFilesDeleted">Total number of files deleted.</param>
/// <param name="TotalFoldersDeleted">Total number of folders deleted.</param>
/// <param name="TotalValidationRecordsDeleted">Total number of file validation records deleted from database.</param>
/// <param name="RemainingYears">List of years that remain after cleanup.</param>
/// <param name="UpdatedTimePeriod">The updated time period in the configuration.</param>
public sealed record CleanupLgdFilesResponse(
    List<string> DeletedYears,
    int TotalFilesDeleted,
    int TotalFoldersDeleted,
    int TotalValidationRecordsDeleted,
    List<string> RemainingYears,
    TimePeriodInfo UpdatedTimePeriod
);

/// <summary>
/// Time period information.
/// </summary>
/// <param name="From">Starting year.</param>
/// <param name="To">Ending year.</param>
public sealed record TimePeriodInfo(
    string From,
    string To
);
