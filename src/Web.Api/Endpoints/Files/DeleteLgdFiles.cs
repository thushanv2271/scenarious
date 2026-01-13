using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Files.DeleteLgdFiles;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for deleting one or multiple LGD files.
/// </summary>
/// <remarks>
/// This endpoint deletes LGD files from:
/// - Physical file system (both pending and processed directories)
/// - JSON configuration in collective_impairment_configs table
/// - File validation results from the database
/// Supports deletion of single or multiple files in one request.
/// </remarks>
internal sealed class DeleteLgdFiles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lgd-files", async (
            HttpContext context,
            ICommandHandler<DeleteLgdFilesCommand, DeleteLgdFilesResponse> handler,
            [FromBody] DeleteLgdFilesRequest request,
            CancellationToken cancellationToken) =>
        {
            // Validate request
            Error? validationResult = (request.FileIds?.Count ?? 0, request.Year, request.FacilityStatus) switch
            {
                (0, _, _) => new Error("FileIds.Empty", "No file IDs provided", ErrorType.Validation),
                (_, null or "", _) => new Error("Year.Required", "Year is required", ErrorType.Validation),
                (_, _, null or "") => new Error("FacilityStatus.Required", "Facility status is required", ErrorType.Validation),
                _ => null
            };

            if (validationResult is not null)
            {
                return CustomResults.Problem(Result.Failure<DeleteLgdFilesResponse>(validationResult));
            }

            // Extract UserId from token
            Guid? userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value switch
            {
                { } value when Guid.TryParse(value, out Guid guid) => guid,
                _ => (Guid?)null
            };

            if (userId is null)
            {
                var failure = Result.Failure<DeleteLgdFilesResponse>(new Error(
                    "Authentication.InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            // Create command
            var command = new DeleteLgdFilesCommand(
                FileIds: request.FileIds!,
                Year: request.Year!,
                FacilityStatus: request.FacilityStatus!,
                DeletedBy: userId.Value
            );

            Result<DeleteLgdFilesResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("LGD Files")
        .WithName("DeleteLgdFiles")
        .WithSummary("Delete one or multiple LGD files")
        .WithDescription("Deletes LGD files from file system, database JSON configuration, and validation records. Supports deleting multiple files in one request.");
    }
}

/// <summary>
/// Request model for deleting LGD files.
/// </summary>
/// <param name="FileIds">List of file IDs to delete.</param>
/// <param name="Year">The year the files belong to (e.g., "2023").</param>
/// <param name="FacilityStatus">The facility status (OpenFacility or ClosedFacility).</param>
public sealed record DeleteLgdFilesRequest(
    List<string>? FileIds,
    string? Year,
    string? FacilityStatus
);
