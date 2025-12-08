using Application.Abstractions.Messaging;

namespace Application.PDAlgorithmResults.GetPDAlgorithmResult;

/// <summary>
/// Query to retrieve the latest PD Algorithm Result
/// Optional filters:
/// - productCategory: filter by specific product category
/// - segment: filter by specific segment (requires productCategory)
/// </summary>
public sealed record GetPDAlgorithmResultQuery(
    string? ProductCategory = null,
    string? Segment = null)
    : IQuery<PDAlgorithmResultResponse>;
