using Application.Abstractions.Messaging;
using Application.PDAlgorithmResults.GetSelectedMethodologies;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PDAlgorithmResults;

/// <summary>
/// Endpoint to retrieve all selected methodologies with pagination and search
/// </summary>
internal sealed class GetSelectedMethodologies : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("pd-algorithm-results/selected-methodologies", async (
            int? pageNumber,
            int? pageSize,
            string? productCategory,
            string? segment,
            string? search,
            IQueryHandler<GetSelectedMethodologiesQuery, GetSelectedMethodologiesResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetSelectedMethodologiesQuery(
                PageNumber: pageNumber ?? 1,
                PageSize: pageSize ?? 10,
                ProductCategory: productCategory,
                Segment: segment,
                Search: search
            );

            Result<GetSelectedMethodologiesResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.PDAlgorithmResults)
        .WithName("GetSelectedMethodologies")
        .WithSummary("Get all selected methodologies with full data, pagination and search")
        .WithDescription(@"
Retrieves all selected methodologies from PD Algorithm Results with full table data.

**Query Parameters:**
- `pageNumber` (default: 1): Page number
- `pageSize` (default: 10): Results per page  
- `productCategory` (optional): Filter by category (e.g., 'BULLET')
- `segment` (optional): Filter by segment (e.g., 'CORPORATE')
- `search` (optional): Search across all fields (case-insensitive)

**Response includes full methodology data:**
- MarginalPdsAfterEfa table
- ExtrapolatedCumulativePdsAfterEfa table
- ExtrapolatedCumulativePdsBeforeEfa table

Each table contains: title, columnHeaders, and rows with ageBucket and pdValuesByYear.
")
        .Produces<GetSelectedMethodologiesResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
