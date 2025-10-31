using Application.Abstractions.Messaging;
using Application.EfaConfigs.Create;
using SharedKernel;
using System.Security.Claims;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.EfaConfigs;

/// <summary>
/// Endpoint for creating one or more EFA configurations.
/// and invokes the corresponding command handler.
/// </summary>
internal sealed class Create : IEndpoint
{
    /// <summary>
    /// DTO representing a single EFA configuration item in the request payload.
    /// </summary>
    public sealed record EfaConfigurationItemDto(int Year, decimal EfaRate);

    /// <summary>
    /// Maps the HTTP POST endpoint for creating EFA configurations.
    /// </summary>
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("efa-configurations", async (
            List<EfaConfigurationItemDto> request,
            HttpContext httpContext,
            ICommandHandler<CreateEfaConfigurationCommand, CreateEfaConfigurationResponse> handler,
            CancellationToken cancellationToken) =>
        {
            // Extract and validate user ID from token
            string? userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                var failureResult = Result.Failure<CreateEfaConfigurationResponse>(new Error(
                    "InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failureResult);
            }

            // Map DTOs to domain items
            var items = request
                .Select(i => new EfaConfigurationItem(i.Year, i.EfaRate))
                .ToList();

            // Create the command
            var command = new CreateEfaConfigurationCommand(items, userId);

            // Execute the command
            Result<CreateEfaConfigurationResponse> result = await handler.Handle(command, cancellationToken);

            // Return success or failure response
            return result.Match(
                response => Results.Ok(response.Created.Concat(response.Updated).ToList()),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionCreate)
        .WithTags("EFA Configurations");
    }
}
