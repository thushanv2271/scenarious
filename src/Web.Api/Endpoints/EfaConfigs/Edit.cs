using Application.Abstractions.Messaging;
using Application.EfaConfigs.Edit;
using SharedKernel;
using System.Security.Claims;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.EfaConfigs;

/// <summary>
/// Endpoint for editing an existing EFA configuration.
/// and invokes the corresponding command handler.
/// </summary>
internal sealed class Edit : IEndpoint
{
    /// <summary>
    /// DTO representing the request payload for editing an EFA configuration.
    /// </summary>

    public sealed record EditRequest(int Year, decimal EfaRate);

    /// <summary>
    /// Maps the HTTP PUT endpoint for updating an EFA configuration.
    /// </summary>
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("efa-configurations/{id:guid}", async (
            Guid id,
            EditRequest request,
            HttpContext httpContext,
            ICommandHandler<EditEfaConfigurationCommand, EditEfaConfigurationResponse> handler,
            CancellationToken cancellationToken) =>
        {
            // Extract and validate user ID from token
            string? userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                var failureResult = Result.Failure<EditEfaConfigurationResponse>(new Error(
                    "InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failureResult);
            }

            // Create the edit command
            var command = new EditEfaConfigurationCommand(
                id,
                request.Year,
                request.EfaRate,
                userId);

            // Execute the command
            Result<EditEfaConfigurationResponse> result = await handler.Handle(command, cancellationToken);

            // Return success or failure response
            return result.Match(
                response => Results.Ok(response),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionEdit)
        .WithTags("EFA Configurations");
    }
}
