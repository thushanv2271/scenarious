using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.MasterData.SegmentMasterData.UploadSegmentMasterData;
using SharedKernel;
using Web.Api.Endpoints;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.MasterData;

internal sealed class UploadSegmentMasterData : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/master-data/segments/upload", async (
            HttpContext httpContext,
            ICommandHandler<UploadSegmentMasterDataCommand, UploadSegmentMasterDataResponse> handler,
            IFormFile file,
            CancellationToken cancellationToken) =>
        {
            // Validate file presence
            if (file is null || file.Length == 0)
            {
                var failure = Result.Failure<UploadSegmentMasterDataResponse>(new Error(
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
                var failure = Result.Failure<UploadSegmentMasterDataResponse>(new Error(
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
            var command = new UploadSegmentMasterDataCommand(
                UploadedBy: userId,
                FileName: file.FileName,
                Content: bytes
            );

            Result<UploadSegmentMasterDataResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Master Data")
        .WithName("UploadSegmentMasterData")
        // Disable antiforgery for API endpoint used by Swagger/clients
        .DisableAntiforgery();
    }
}