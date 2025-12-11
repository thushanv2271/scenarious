using Application.Abstractions.Messaging;

namespace Application.PDAlgorithmResults.GetSelectedMethodologies;

/// <summary>
/// Query to retrieve all selected methodologies from PD Algorithm Results with pagination and search
/// </summary>
public sealed record GetSelectedMethodologiesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? ProductCategory = null,
    string? Segment = null,
    string? Search = null
) : IQuery<GetSelectedMethodologiesResponse>;
