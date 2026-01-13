using Application.Abstractions.Messaging;
using Application.LgdAlgorithmResults.GetLgdAlgorithmResult;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LgdAlgorithmResults;

/// <summary>
/// Endpoint to retrieve the latest LGD Algorithm Result
/// Returns the entire JSON data from the database
/// </summary>
internal sealed class GetLgdAlgorithmResult : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("lgd-algorithm-results", async (
            IQueryHandler<GetLgdAlgorithmResultQuery, LgdAlgorithmResultResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetLgdAlgorithmResultQuery();

            Result<LgdAlgorithmResultResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.LGDSetupAccess)
        .WithTags(Tags.LgdAlgorithmResults)
        .WithName("GetLgdAlgorithmResult")
        .WithDescription("Retrieves the latest LGD Algorithm Result with complete JSON data")
        .Produces<LgdAlgorithmResultResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}