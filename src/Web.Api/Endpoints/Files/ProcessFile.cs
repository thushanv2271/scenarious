using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Files.ProcessFile;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Represents an endpoint for processing files and generating reports.
/// </summary>
/// <remarks>
/// This endpoint processes uploaded files by validating the file content, 
/// extracting user information from the HTTP context, and dispatching a command 
/// to handle file processing and report generation. The endpoint requires 
/// authorization and specific permissions to access.
/// </remarks>
internal sealed class ProcessFile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/files/process", async (
            HttpContext httpContext,
            ICommandHandler<ProcessFileCommand, ProcessFileResponse> handler,
            IFormFile file,
            CancellationToken cancellationToken) =>
        {
            // Validate file presence
            if (file is null || file.Length == 0)
            {
                var failure = Result.Failure<ProcessFileResponse>(new Error(
                    "File.Empty",
                    "No file was provided or the file is empty.",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            // Extract UserId from claims
            string? userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                var failure = Result.Failure<ProcessFileResponse>(new Error(
                    "InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            // Read file content
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms, cancellationToken);
                bytes = ms.ToArray();
            }

            // Dispatch command via the handler
            var command = new ProcessFileCommand(
                UploadedBy: userId,
                FileName: file.FileName,
                Content: bytes
            );

            Result<ProcessFileResponse> result = await handler.Handle(command, cancellationToken);

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