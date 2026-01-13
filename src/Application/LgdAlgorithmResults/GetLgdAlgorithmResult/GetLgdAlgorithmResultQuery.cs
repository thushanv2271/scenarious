using Application.Abstractions.Messaging;

namespace Application.LgdAlgorithmResults.GetLgdAlgorithmResult;

/// <summary>
/// Query to retrieve the latest LGD Algorithm Result
/// Returns the entire JSON data from the database
/// </summary>
public sealed record GetLgdAlgorithmResultQuery
    : IQuery<LgdAlgorithmResultResponse>;