using Application.Abstractions.Messaging;

namespace Application.Files.CleanupLgdFiles;

/// <summary>
/// Command to cleanup LGD files outside the specified time period range.
/// </summary>
/// <param name="TimePeriodFrom">Starting year of the valid range (e.g., "2027").</param>
/// <param name="TimePeriodTo">Ending year of the valid range (e.g., "2023"). If null or empty, performs full cleanup.</param>
/// <param name="CleanedBy">User ID who is performing the cleanup.</param>
public sealed record CleanupLgdFilesCommand(
    string TimePeriodFrom,
    string? TimePeriodTo,
    Guid CleanedBy
) : ICommand<CleanupLgdFilesResponse>;
