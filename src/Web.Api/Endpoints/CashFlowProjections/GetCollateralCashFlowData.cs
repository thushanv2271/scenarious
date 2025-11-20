using Application.Abstractions.Messaging;
using Application.CashFlowProjections.GetCollateralCashFlowData;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.CashFlowProjections;

/// <summary>
/// Endpoint to retrieve collateral and last quarter cash flow data for a facility
/// </summary>
internal sealed class GetCollateralCashFlowData : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("facilities/{facilityNumber}/collateral-cashflow-data", async (
            string facilityNumber,
            IQueryHandler<GetCollateralCashFlowDataQuery, CollateralCashFlowDataResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCollateralCashFlowDataQuery(facilityNumber);

            Result<CollateralCashFlowDataResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.EclAnalysisAccess)
        .WithTags(Tags.CashFlowProjections)
        .WithName("GetCollateralCashFlowData")
        .Produces<CollateralCashFlowDataResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
