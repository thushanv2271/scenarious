using Application.Abstractions.Messaging;

namespace Application.Files.DownloadLgdFile;

/// <summary>
/// Query to download an LGD file by its ID from the JSON configuration.
/// </summary>
/// <param name="FileId">The unique identifier of the file in the JSON configuration.</param>
/// <param name="Year">The year the file belongs to.</param>
/// <param name="FacilityStatus">The facility status (OpenFacility or ClosedFacility).</param>
public sealed record DownloadLgdFileQuery(
    string FileId,
    string Year,
    string FacilityStatus
) : IQuery<DownloadLgdFileResult>;
