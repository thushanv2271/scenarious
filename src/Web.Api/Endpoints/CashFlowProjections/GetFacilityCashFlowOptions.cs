using Application.Abstractions.Messaging;
using Application.CashFlowProjections.GetFacilityCashFlowOptions;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.CashFlowProjections;

/// <summary>
/// Endpoint to retrieve scenarios and cash flow options for a facility
/// </summary>
internal sealed class GetFacilityCashFlowOptions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("facilities/{facilityNumber}/cashflow-options", async (
            string facilityNumber,
            IQueryHandler<GetFacilityCashFlowOptionsQuery, FacilityCashFlowOptionsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetFacilityCashFlowOptionsQuery(facilityNumber);

            Result<FacilityCashFlowOptionsResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.EclAnalysisAccess)
        .WithTags(Tags.CashFlowProjections)
        .WithName("GetFacilityCashFlowOptions")
        .Produces<FacilityCashFlowOptionsResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
