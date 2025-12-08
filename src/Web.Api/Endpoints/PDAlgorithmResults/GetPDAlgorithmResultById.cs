using Application.Abstractions.Messaging;
using Application.PDAlgorithmResults.GetPDAlgorithmResult;
using Application.PDAlgorithmResults.GetPDAlgorithmResultById;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PDAlgorithmResults;

/// <summary>
/// Endpoint to retrieve a specific PD Algorithm Result by ID
/// Supports optional productCategory and segment query parameters
/// </summary>
internal sealed class GetPDAlgorithmResultById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("pd-algorithm-results/{id:guid}", async (
            Guid id,
            [FromQuery] string? productCategory,
            [FromQuery] string? segment,
            IQueryHandler<GetPDAlgorithmResultByIdQuery, PDAlgorithmResultResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPDAlgorithmResultByIdQuery(id, productCategory, segment);

            Result<PDAlgorithmResultResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags(Tags.PDAlgorithmResults)
        .WithName("GetPDAlgorithmResultById")
        .WithDescription("Retrieves a specific PD Algorithm Result by ID. Optional filters: productCategory, segment")
        .Produces<PDAlgorithmResultResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}
