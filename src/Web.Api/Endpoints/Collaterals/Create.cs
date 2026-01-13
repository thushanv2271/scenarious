using Application.Abstractions.Messaging;
using Application.Collaterals.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Collaterals;

internal sealed class Create : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("collaterals", async (
            CreateCollateralRequest request,
            ICommandHandler<CreateCollateralCommand, CreateCollateralResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateCollateralCommand(
                request.Names
            );

            Result<CreateCollateralResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Collaterals");
    }
}