using Application.Abstractions.Messaging;

namespace Application.Files.UploadLgdFiles;

/// <summary>
/// Represents a command to upload LGD files with facility status classification.
/// </summary>
/// <param name="UploadedBy">The unique identifier of the user uploading the files.</param>
/// <param name="Files">The files being uploaded, including metadata and content.</param>
/// <param name="Year">The year this file belongs to (e.g., "2025", "2026").</param>
/// <param name="FacilityStatus">The facility status classification (OpenFacility or ClosedFacility).</param>
/// <param name="TimePeriod">The time period configuration with from and to years.</param>
/// <param name="FinancialYearEnd">The financial year end date (e.g., "2025-12-31").</param>
public sealed record UploadLgdFilesCommand(
    Guid UploadedBy,
    IReadOnlyList<LgdFileUploadData> Files,
    string Year,
    string FacilityStatus,
    TimePeriodRequest? TimePeriod,
    string? FinancialYearEnd
) : ICommand<UploadLgdFilesResponse>;

/// <summary>
/// Represents the data for a single LGD file upload.
/// </summary>
/// <param name="FileName">The name of the file being uploaded, including its extension.</param>
/// <param name="ContentType">The MIME type of the file.</param>
/// <param name="Content">The binary content of the file as a byte array.</param>
public sealed record LgdFileUploadData(
    string FileName,
    string ContentType,
    byte[] Content
);

/// <summary>
/// Represents the time period range for LGD file uploads.
/// </summary>
/// <param name="From">The starting year of the time period.</param>
/// <param name="To">The ending year of the time period.</param>
public sealed record TimePeriodRequest(
    string From,
    string To
);
