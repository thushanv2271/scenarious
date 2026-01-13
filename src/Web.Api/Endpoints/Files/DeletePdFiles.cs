using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Files.DeletePdFiles;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for deleting PD files by upload IDs
/// </summary>
internal sealed class DeletePdFiles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/pd-files", async (
            [FromBody] DeletePdFilesRequest request,
            ICommandHandler<DeletePdFilesCommand, DeletePdFilesResponse> handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Validate request
            Error? validationResult = (request.Ids?.Count ?? 0, request.TimePeriod) switch
            {
                (0, _) => new Error("Ids.Empty", "No file IDs provided", ErrorType.Validation),
                (_, null or "") => new Error("TimePeriod.Required", "Time period is required", ErrorType.Validation),
                _ => null
            };

            if (validationResult is not null)
            {
                return CustomResults.Problem(Result.Failure<DeletePdFilesResponse>(validationResult));
            }

            // Extract UserId from token
            Guid? userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value switch
            {
                { } value when Guid.TryParse(value, out Guid guid) => guid,
                _ => (Guid?)null
            };

            if (userId is null)
            {
                var failure = Result.Failure<DeletePdFilesResponse>(new Error(
                    "Authentication.InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            // Create command
            var command = new DeletePdFilesCommand(
                UploadIds: request.Ids!,
                TimePeriod: request.TimePeriod!,
                DeletedBy: userId.Value
            );

            Result<DeletePdFilesResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("PD Files")
        .WithName("DeletePdFiles")
        .WithSummary("Delete PD files by upload IDs")
        .WithDescription("Deletes PD files from file system and JSON configuration. Removes files from both pending and processed directories.");
    }
}

/// <summary>
/// Request model for deleting PD files
/// </summary>
public sealed record DeletePdFilesRequest(
    List<string>? Ids,
    string? TimePeriod
);
