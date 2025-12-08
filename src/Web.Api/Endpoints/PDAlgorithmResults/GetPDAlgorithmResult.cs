using Application.Abstractions.Messaging;
using Application.PDAlgorithmResults.GetPDAlgorithmResult;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PDAlgorithmResults;

/// <summary>
/// Endpoint to retrieve the latest PD Algorithm Result
/// Supports optional productCategory and segment query parameters
/// </summary>
internal sealed class GetPDAlgorithmResult : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("pd-algorithm-results", async (
            [FromQuery] string? productCategory,
            [FromQuery] string? segment,
            IQueryHandler<GetPDAlgorithmResultQuery, PDAlgorithmResultResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPDAlgorithmResultQuery(productCategory, segment);

            Result<PDAlgorithmResultResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags(Tags.PDAlgorithmResults)
        .WithName("GetPDAlgorithmResult")
        .WithDescription("Retrieves the latest PD Algorithm Result. Optional filters: productCategory, segment")
        .Produces<PDAlgorithmResultResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}
