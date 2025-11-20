using Application.Abstractions.Messaging;
using Application.CashFlowProjections.GetContractualCashFlows;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.CashFlowProjections;

/// <summary>
/// Endpoint to retrieve contractual cash flows for a facility
/// </summary>
internal sealed class GetContractualCashFlows : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("facilities/{facilityNumber}/contractual-cashflows", async (
            string facilityNumber,
            IQueryHandler<GetContractualCashFlowsQuery, ContractualCashFlowsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetContractualCashFlowsQuery(facilityNumber);

            Result<ContractualCashFlowsResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.EclAnalysisAccess)
        .WithTags(Tags.CashFlowProjections)
        .WithName("GetContractualCashFlows")
        .Produces<ContractualCashFlowsResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
