using Application.Abstractions.Messaging;

namespace Application.LgdAlgorithmResults.UpdateLgdAlgorithmResult;

/// <summary>
/// Command to update the LGD Algorithm Result JSON data
/// </summary>
public sealed record UpdateLgdAlgorithmResultCommand(
    string LgdAlgorithmResultData)
    : ICommand<Guid>;