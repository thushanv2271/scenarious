using Application.Abstractions.Messaging;
using Application.PDAlgorithmResults.GetPDAlgorithmResult;

namespace Application.PDAlgorithmResults.GetPDAlgorithmResultById;

/// <summary>
/// Query to retrieve a specific PD Algorithm Result by ID
/// Optional filters: productCategory, segment
/// </summary>
public sealed record GetPDAlgorithmResultByIdQuery(
    Guid Id,
    string? ProductCategory = null,
    string? Segment = null)
    : IQuery<PDAlgorithmResultResponse>;
