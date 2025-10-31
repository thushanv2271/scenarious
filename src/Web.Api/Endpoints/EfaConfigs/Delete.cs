using Application.Abstractions.Messaging;
using Application.EfaConfigs.Delete;
using SharedKernel;
using System.Security.Claims;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.EfaConfigs;

/// <summary>
/// Endpoint for deleting a single EFA configuration by its ID.
/// and invokes the corresponding command handler.
/// </summary>
internal sealed class Delete : IEndpoint
{
    /// <summary>
    /// Maps the HTTP DELETE endpoint for removing an EFA configuration.
    /// </summary>
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Single delete endpoint
        app.MapDelete("efa-configurations/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            ICommandHandler<DeleteEfaConfigurationCommand, DeleteEfaConfigurationResponse> handler,
            CancellationToken cancellationToken) =>
        {
            // Extract and validate user ID from token
            string? userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                var failureResult = Result.Failure<DeleteEfaConfigurationResponse>(new Error(
                    "InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failureResult);
            }

            // Create the delete command
            var command = new DeleteEfaConfigurationCommand(id, userId);

            // Execute the command
            Result<DeleteEfaConfigurationResponse> result = await handler.Handle(command, cancellationToken);

            // Return success or failure response
            return result.Match(
                response => Results.Ok(response),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionDelete)
        .WithTags("EFA Configurations");
    }
}
