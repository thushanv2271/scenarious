using Application.Abstractions.Messaging;
using Application.Files.ProcessMultipleFiles;
using Application.Files.ProcessMultipleFilesByIds;
using Microsoft.Extensions.Logging;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for processing multiple files by their database IDs.
/// </summary>
/// <remarks>
/// This endpoint processes files that have already been uploaded to the system by specifying their database IDs.
/// It performs the same analysis and validation as the process-multiple endpoint but works with files already in the system.
/// </remarks>
internal sealed class ProcessMultipleFilesByIds : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/files/process-by-ids", async (
            ProcessMultipleFilesByIdsRequest request,
            ICommandHandler<ProcessMultipleFilesByIdsCommand, ProcessMultipleFilesResponse> handler,
            ILogger<ProcessMultipleFilesByIds> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("ProcessMultipleFilesByIds request received - FileIds count: {FileIdsCount}, CollectiveImpairmentType: {CollectiveImpairmentType}, TimePeriod: {TimePeriod}", 
                request?.FileIds?.Length ?? 0, request?.CollectiveImpairmentType, request?.TimePeriod);
            
            // Validate parameters
            if (request?.FileIds == null || request.FileIds.Length == 0)
            {
                var failure = Result.Failure<ProcessMultipleFilesResponse>(new Error(
                    "FileIds.Empty",
                    "At least one file ID is required.",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            if (string.IsNullOrWhiteSpace(request.CollectiveImpairmentType))
            {
                var failure = Result.Failure<ProcessMultipleFilesResponse>(new Error(
                    "CollectiveImpairmentType.Empty",
                    "CollectiveImpairmentType is required.",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            if (string.IsNullOrWhiteSpace(request.TimePeriod))
            {
                var failure = Result.Failure<ProcessMultipleFilesResponse>(new Error(
                    "TimePeriod.Empty",
                    "TimePeriod is required.",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            var command = new ProcessMultipleFilesByIdsCommand(
                FileIds: request.FileIds,
                CollectiveImpairmentType: request.CollectiveImpairmentType,
                TimePeriod: request.TimePeriod
            );

            logger.LogInformation("ProcessMultipleFilesByIds dispatching command to handler with {FileIdsCount} file IDs", request.FileIds.Length);
            Result<ProcessMultipleFilesResponse> result = await handler.Handle(command, cancellationToken);
            logger.LogInformation("ProcessMultipleFilesByIds handler completed - Success: {IsSuccess}", result.IsSuccess);

            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Files")
        .WithName("ProcessMultipleFilesByIds")
        .WithSummary("Process multiple files by their database IDs")
        .DisableAntiforgery();
    }
}

/// <summary>
/// Request model for processing files by IDs
/// </summary>
public sealed record ProcessMultipleFilesByIdsRequest(
    Guid[] FileIds,
    string CollectiveImpairmentType,
    string TimePeriod
);