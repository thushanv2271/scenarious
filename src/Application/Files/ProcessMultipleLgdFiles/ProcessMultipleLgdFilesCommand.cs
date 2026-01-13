using Application.Abstractions.Messaging;

namespace Application.Files.ProcessMultipleLgdFiles;

/// <summary>
/// Command to process multiple LGD files with validation and analysis.
/// </summary>
public sealed record ProcessMultipleLgdFilesCommand(
    string Year,
    string FacilityStatus
) : ICommand<ProcessMultipleLgdFilesResponse>;
