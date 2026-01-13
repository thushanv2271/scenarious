using Application.Abstractions.Messaging;
using Application.Files.DownloadLgdFile;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for downloading an LGD file by its ID, year, and facility status.
/// </summary>
/// <remarks>
/// This endpoint retrieves an LGD file from the server's storage based on the provided file ID, year, and facility status.
/// The file is streamed to the client with appropriate headers for download.
/// Authentication is required, and the user must have the PDSetupAccess permission.
/// 
/// ## Example Usage:
/// ```
/// GET /lgd-files/{fileId}/download?year=2025&facilityStatus=OpenFacility
/// ```
/// 
/// ## Parameters:
/// - **fileId** (path): The unique file ID from the JSON configuration
/// - **year** (query): The year (e.g., "2025")
/// - **facilityStatus** (query): Either "OpenFacility" or "ClosedFacility"
/// 
/// ## Response:
/// - 200 OK: File download stream
/// - 404 Not Found: File not found in configuration or physical location
/// - 400 Bad Request: Invalid parameters
/// - 401 Unauthorized: Authentication required
/// </remarks>
internal sealed class DownloadLgdFile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lgd-files/{fileId}/download", async (
            string fileId,
            [FromQuery] string year,
            [FromQuery] string facilityStatus,
            IQueryHandler<DownloadLgdFileQuery, DownloadLgdFileResult> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new DownloadLgdFileQuery(fileId, year, facilityStatus);
            Result<DownloadLgdFileResult> result = await handler.Handle(query, cancellationToken);

            if (result.IsFailure)
            {
                return CustomResults.Problem(result);
            }

            DownloadLgdFileResult fileData = result.Value!;

            // Return the file as a downloadable stream
            return Results.File(
                fileData.PhysicalPath,
                contentType: fileData.ContentType,
                fileDownloadName: fileData.OriginalFileName,
                enableRangeProcessing: true
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("LGD Files")
        .WithName("DownloadLgdFile")
        .WithSummary("Download an LGD file by ID")
        .WithDescription("Downloads an LGD file from pending or processed directories based on file ID, year, and facility status")
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
