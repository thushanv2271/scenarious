using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Files.UploadFile;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Represents an endpoint for handling file uploads via HTTP POST requests.
/// </summary>
/// <remarks>This endpoint processes file uploads by validating the file, extracting user information from the
/// HTTP context, and dispatching a command to handle the uploaded file. The endpoint requires authorization and
/// specific permissions to access.  The uploaded file is expected to be provided as an <see cref="IFormFile"/> in the
/// request body. The endpoint validates the presence and content of the file, as well as the validity of the user's
/// authentication token. If validation fails, an appropriate error response is returned.  Upon successful validation,
/// the file content is read into memory and passed to a command handler for further processing. The handler is
/// responsible for executing the business logic associated with the file upload.  The endpoint returns a 201 Created
/// response with the file's URL if the operation succeeds, or an appropriate error response if it fails.</remarks>
internal sealed class UploadFile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/files/upload", async (
            HttpContext httpContext,
            ICommandHandler<UploadFileCommand, UploadFileResponse> handler,
            IFormFile file,
            CancellationToken cancellationToken) =>
        {
            // Validate file presence
            if (file is null || file.Length == 0)
            {
                var failure = Result.Failure<UploadFileResponse>(new Error(
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
                var failure = Result.Failure<UploadFileResponse>(new Error(
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

            // Dispatch command via the handler (no MediatR)
            var command = new UploadFileCommand(
                UploadedBy: userId,
                FileName: file.FileName,
                ContentType: file.ContentType ?? string.Empty,
                Content: bytes
            );

            Result<UploadFileResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                data => Results.Created(data.Url, data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Files")
        // Disable antiforgery for API endpoint used by Swagger/clients
        .DisableAntiforgery();
    }
}
