using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Files.DeleteFile;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for deleting uploaded files.
/// Removes both the physical file from storage and metadata from the database.
/// Supports single or batch deletion.
/// </summary>
internal sealed class DeleteFile : IEndpoint
{
    public sealed record DeleteFileRequest(List<Guid> Ids);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/files", async (
            [FromBody] DeleteFileRequest request,  //Add [FromBody] attribute
            HttpContext httpContext,
            ICommandHandler<DeleteFileCommand, List<DeleteFileResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            // Extract and validate UserId from JWT token
            string? userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                var failure = Result.Failure<List<DeleteFileResponse>>(new Error(
                    "InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            // Create delete command
            var command = new DeleteFileCommand(request.Ids, userId);

            // Execute command via handler
            Result<List<DeleteFileResponse>> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                data => Results.Ok(new
                {
                    Message = data.Count == 1
                        ? "File deleted successfully"
                        : $"{data.Count} files deleted successfully",
                    Data = data
                }),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Files")
        .WithName("DeleteFiles");
    }
}
