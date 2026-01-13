using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Files.CleanupLgdFiles;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for cleaning up LGD files outside the specified time period range.
/// </summary>
/// <remarks>
/// This endpoint removes all files and folders for years outside the specified time period range.
/// It cleans up:
/// - Physical files from both pending and processed directories
/// - Year nodes from JSON configuration
/// - Empty folders
/// The time period range is inclusive (e.g., from 2027 to 2023 includes 2027, 2026, 2025, 2024, and 2023).
/// </remarks>  d
internal sealed class CleanupLgdFiles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lgd-files/cleanup", async (
            HttpContext context,
            ICommandHandler<CleanupLgdFilesCommand, CleanupLgdFilesResponse> handler,
            [FromBody] CleanupLgdFilesRequest request,
            CancellationToken cancellationToken) =>
        {
            // Validate request - only timePeriodFrom is required
            Error? validationResult = request.TimePeriodFrom switch
            {
                null or "" => new Error("TimePeriodFrom.Required", "Time period from is required", ErrorType.Validation),
                _ => null
            };

            if (validationResult is not null)
            {
                return CustomResults.Problem(Result.Failure<CleanupLgdFilesResponse>(validationResult));
            }

            // Extract UserId from token
            Guid? userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value switch
            {
                { } value when Guid.TryParse(value, out Guid guid) => guid,
                _ => (Guid?)null
            };

            if (userId is null)
            {
                var failure = Result.Failure<CleanupLgdFilesResponse>(new Error(
                    "Authentication.InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            // Create command - timePeriodTo can be null for full cleanup
            var command = new CleanupLgdFilesCommand(
                TimePeriodFrom: request.TimePeriodFrom!,
                TimePeriodTo: request.TimePeriodTo,
                CleanedBy: userId.Value
            );

            Result<CleanupLgdFilesResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("LGD Files")
        .WithName("CleanupLgdFiles")
        .WithSummary("Cleanup LGD files outside time period range")
        .WithDescription("Removes all files, folders, and JSON configuration for years outside the specified time period range. The range is inclusive (e.g., from 2027 to 2023 includes years 2027, 2026, 2025, 2024, and 2023). If 'timePeriodTo' is null or empty, performs a FULL CLEANUP removing ALL years and files.");
    }
}

/// <summary>
/// Request model for cleaning up LGD files.
/// </summary>
/// <param name="TimePeriodFrom">Starting year of the valid range (e.g., "2027").</param>
/// <param name="TimePeriodTo">Ending year of the valid range (e.g., "2023").</param>
public sealed record CleanupLgdFilesRequest(
    string? TimePeriodFrom,
    string? TimePeriodTo
);
