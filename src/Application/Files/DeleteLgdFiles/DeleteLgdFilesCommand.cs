using Application.Abstractions.Messaging;

namespace Application.Files.DeleteLgdFiles;

/// <summary>
/// Command to delete one or multiple LGD files.
/// </summary>
/// <param name="FileIds">List of file IDs to delete from the JSON configuration.</param>
/// <param name="Year">The year the files belong to.</param>
/// <param name="FacilityStatus">The facility status (OpenFacility or ClosedFacility).</param>
/// <param name="DeletedBy">User ID who is deleting the files.</param>
public sealed record DeleteLgdFilesCommand(
    List<string> FileIds,
    string Year,
    string FacilityStatus,
    Guid DeletedBy
) : ICommand<DeleteLgdFilesResponse>;
