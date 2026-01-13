using Application.Abstractions.Messaging;
using Application.Files.DownloadFile;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for downloading an uploaded file by its ID.
/// </summary>
/// <remarks>
/// This endpoint retrieves a file from the server's storage based on the provided file ID.
/// The file is streamed to the client with appropriate headers for download.
/// Authentication is required, and the user must have the PDSetupAccess permission.
/// </remarks>
internal sealed class DownloadFile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/files/{id:guid}/download", async (
            Guid id,
            IQueryHandler<DownloadFileQuery, DownloadFileResult> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new DownloadFileQuery(id);
            Result<DownloadFileResult> result = await handler.Handle(query, cancellationToken);

            if (result.IsFailure)
            {
                return CustomResults.Problem(result);
            }

            DownloadFileResult fileData = result.Value!;

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
        .WithTags("Files")
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
