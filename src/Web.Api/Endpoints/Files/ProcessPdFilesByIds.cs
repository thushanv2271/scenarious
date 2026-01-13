using Application.Abstractions.Messaging;
using Application.Files.ProcessPdFilesByIds;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for processing PD files by upload IDs - stores validation results in both JSON and database
/// </summary>
internal sealed class ProcessPdFilesByIds : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/pd-files/process-by-ids", async (
            [FromBody] ProcessPdFilesByIdsRequest request,
            ICommandHandler<ProcessPdFilesByIdsCommand, ProcessPdFilesByIdsResponse> handler,
            ILogger<ProcessPdFilesByIds> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("ProcessPdFilesByIds request received - FileIds count: {Count}, TimePeriod: {TimePeriod}",
                request?.FileIds?.Count ?? 0, request?.TimePeriod);

            // Validate parameters - accept fileIds to match frontend expectation
            if (request?.FileIds == null || request.FileIds.Count == 0)
            {
                var failure = Result.Failure<ProcessPdFilesByIdsResponse>(new Error(
                    "FileIds.Empty",
                    "At least one file ID is required.",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            if (string.IsNullOrWhiteSpace(request.TimePeriod))
            {
                var failure = Result.Failure<ProcessPdFilesByIdsResponse>(new Error(
                    "TimePeriod.Empty",
                    "Time period is required.",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            var command = new ProcessPdFilesByIdsCommand(
                UploadIds: request.FileIds,  // Map fileIds from request to uploadIds in command
                TimePeriod: request.TimePeriod
            );

            logger.LogInformation("ProcessPdFilesByIds dispatching command to handler");
            Result<ProcessPdFilesByIdsResponse> result = await handler.Handle(command, cancellationToken);
            logger.LogInformation("ProcessPdFilesByIds handler completed - Success: {IsSuccess}", result.IsSuccess);

            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("PD Files")
        .WithName("ProcessPdFilesByIds")
        .WithSummary("Process PD files by upload IDs and store validation results")
        .WithDescription("Validates PD files and stores validation results in both collective_impairment_configs JSON and file_validation_results table. Files are moved from pending to processed directory.")
        .DisableAntiforgery();
    }
}

/// <summary>
/// Request model for processing PD files by IDs - matches POST /files/process-by-ids format
/// </summary>
public sealed record ProcessPdFilesByIdsRequest(
    List<string>? FileIds,  // Changed from UploadIds to FileIds to match frontend
    string? TimePeriod
);
