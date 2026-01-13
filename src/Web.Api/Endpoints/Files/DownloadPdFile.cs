using Application.Abstractions.Messaging;
using Application.Files.DownloadPdFile;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for downloading PD files with error indicators
/// </summary>
internal sealed class DownloadPdFile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/pd-files/{uploadId}/download", async (
            string uploadId,
            string? timePeriod,  // Make it nullable to provide better error message
            IQueryHandler<DownloadPdFileQuery, DownloadPdFileResponse> handler,
            ILogger<DownloadPdFile> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("DownloadPdFile request received - UploadId: {UploadId}, TimePeriod: {TimePeriod}", 
                uploadId, timePeriod ?? "null");

            if (string.IsNullOrWhiteSpace(uploadId))
            {
                logger.LogWarning("DownloadPdFile - Upload ID is empty or null");
                var failure = Result.Failure<DownloadPdFileResponse>(new Error(
                    "UploadId.Empty",
                    "Upload ID is required.",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            if (string.IsNullOrWhiteSpace(timePeriod))
            {
                logger.LogWarning("DownloadPdFile - Time period is empty or null for UploadId: {UploadId}", uploadId);
                var failure = Result.Failure<DownloadPdFileResponse>(new Error(
                    "TimePeriod.Empty",
                    "Time period query parameter is required. Example: ?timePeriod=2024",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            var query = new DownloadPdFileQuery(
                UploadId: uploadId,
                TimePeriod: timePeriod
            );

            logger.LogInformation("DownloadPdFile dispatching query to handler");
            Result<DownloadPdFileResponse> result = await handler.Handle(query, cancellationToken);
            
            if (result.IsSuccess)
            {
                logger.LogInformation("DownloadPdFile completed successfully for UploadId: {UploadId}", uploadId);
            }
            else
            {
                logger.LogWarning("DownloadPdFile failed for UploadId: {UploadId} - Error: {Error}", 
                    uploadId, result.Error.Description);
            }

            return result.Match(
                data => Results.File(
                    data.Content,
                    data.ContentType,
                    data.FileName
                ),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("PD Files")
        .WithName("DownloadPdFile")
        .WithSummary("Download PD file with error indicators")
        .WithDescription("Downloads a PD file by upload ID. If validation errors exist, a 'ValidationErrors' column is added to the CSV with error details for each row.");
    }
}
