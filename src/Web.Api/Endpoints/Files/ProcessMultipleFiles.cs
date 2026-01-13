using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Files.ProcessMultipleFiles;
using Microsoft.Extensions.Logging;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for processing multiple files and generating analysis reports with cross-file validation.
/// </summary>
/// <remarks>
/// This endpoint processes multiple uploaded files simultaneously, performing data analysis and validation 
/// across all files to detect duplicates and generate comprehensive reports. It leverages the Saral.FileProcessor 
/// libraries to provide detailed analytics on file content, data quality metrics, and cross-file relationships.
/// </remarks>
internal sealed class ProcessMultipleFiles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/files/process-multiple", async (
            HttpContext httpContext,
            ICommandHandler<ProcessMultipleFilesCommand, ProcessMultipleFilesResponse> handler,
            ILogger<ProcessMultipleFiles> logger,
            string collectiveImpairmentType,
            string timePeriod,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("ProcessMultipleFiles request received - CollectiveImpairmentType: {CollectiveImpairmentType}, TimePeriod: {TimePeriod}", collectiveImpairmentType, timePeriod);
            
            // Validate parameters
            if (string.IsNullOrWhiteSpace(collectiveImpairmentType))
            {
                var failure = Result.Failure<ProcessMultipleFilesResponse>(new Error(
                    "CollectiveImpairmentType.Empty",
                    "CollectiveImpairmentType is required.",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            if (string.IsNullOrWhiteSpace(timePeriod))
            {
                var failure = Result.Failure<ProcessMultipleFilesResponse>(new Error(
                    "TimePeriod.Empty",
                    "TimePeriod is required.",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            // Dispatch command via the handler
            var command = new ProcessMultipleFilesCommand(
                CollectiveImpairmentType: collectiveImpairmentType,
                TimePeriod: timePeriod
            );

            logger.LogInformation("ProcessMultipleFiles dispatching command to handler");
            Result<ProcessMultipleFilesResponse> result = await handler.Handle(command, cancellationToken);
            logger.LogInformation("ProcessMultipleFiles handler completed - Success: {IsSuccess}", result.IsSuccess);

            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Files")
        .DisableAntiforgery();
    }
}