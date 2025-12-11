using Application.Abstractions.Messaging;
using Application.Files.ProcessMultipleFiles;

namespace Application.Files.ProcessMultipleFilesByIds;

public sealed record ProcessMultipleFilesByIdsCommand(
    Guid[] FileIds,
    string CollectiveImpairmentType,
    string TimePeriod
) : ICommand<ProcessMultipleFilesResponse>;