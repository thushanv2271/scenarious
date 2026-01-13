using Application.Abstractions.Messaging;
using Application.Collaterals.GetAll;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Collaterals;

internal sealed class GetAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("collaterals", async (
            IQueryHandler<GetAllCollateralsQuery, List<CollateralResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAllCollateralsQuery();

            Result<List<CollateralResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Collaterals");
    }
}