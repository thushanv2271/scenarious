using Application.Abstractions.Messaging;
using Application.CashFlowProjections.GetCustomerFacilities;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.CashFlowProjections;

/// <summary>
/// Endpoint to retrieve all facilities for a customer
/// </summary>
internal sealed class GetCustomerFacilities : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("customers/{customerNumber}/facilities", async (
            string customerNumber,
            IQueryHandler<GetCustomerFacilitiesQuery, List<CustomerFacilityResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCustomerFacilitiesQuery(customerNumber);

            Result<List<CustomerFacilityResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.EclAnalysisAccess)
        .WithTags(Tags.CashFlowProjections)
        .WithName("GetCustomerFacilities")
        .Produces<List<CustomerFacilityResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
