using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Files.ProcessMultipleFiles;

public sealed record ProcessMultipleFilesCommand(
    string CollectiveImpairmentType,
    string TimePeriod
) : ICommand<ProcessMultipleFilesResponse>;
