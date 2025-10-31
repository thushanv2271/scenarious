using Application.Abstractions.Messaging;
using Application.PD.PDSetup_TempStore;
using SharedKernel;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PD;

internal sealed class PDSetupTempStore : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("pd/setup/temp-store", async (
            JsonObject stepsJson,
            HttpContext httpContext,
            ICommandHandler<PDSetupTempStoreCommand> handler,
            CancellationToken cancellationToken) =>
        {
            // Extract UserId from token claims
            string? userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                // Use Result.Failure so CustomResults.Problem can accept it
                var failureResult = Result.Failure(new Error(
                    "InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failureResult);
            }

            PDSetupTempStoreCommand command = new(stepsJson, userId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                    () => Results.Ok(),
                    error => CustomResults.Problem(error)
                );

        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("PD Setup");
    }
}
