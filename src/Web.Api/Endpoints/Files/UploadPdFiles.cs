using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Files.UploadPdFiles;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for uploading PD files - stores metadata in collective_impairment_configs JSON
/// </summary>
internal sealed class UploadPdFiles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/pd-files", async (
            IFormFileCollection files,
            ICommandHandler<UploadPdFilesCommand, UploadPdFilesResponse> handler,
            HttpContext context,
            [FromForm] string timePeriod,
            CancellationToken cancellationToken) =>
        {
            // Validation
            Error? validationResult = (files?.Count ?? 0, timePeriod) switch
            {
                (0, _) => new Error("Files.Empty", "No files provided", ErrorType.Validation),
                ( > 10, _) => new Error("Files.TooMany", "Maximum 10 files allowed per upload", ErrorType.Validation),
                (_, null or "") => new Error("TimePeriod.Required", "Time period is required", ErrorType.Validation),
                _ => null
            };

            if (validationResult is not null)
            {
                return CustomResults.Problem(Result.Failure<UploadPdFilesResponse>(validationResult));
            }

            // Validate individual files
            foreach (IFormFile file in files!)
            {
                Error? fileValidation = (file.FileName, file.Length) switch
                {
                    (null or "", _) => new Error("File.NoName", "File name is required", ErrorType.Validation),
                    (_, 0) => new Error("File.Empty", "File cannot be empty", ErrorType.Validation),
                    (_, > 50_000_000) => new Error("File.TooLarge", "File size exceeds 50MB limit", ErrorType.Validation),
                    _ => null
                };

                if (fileValidation is not null)
                {
                    return CustomResults.Problem(Result.Failure<UploadPdFilesResponse>(fileValidation));
                }
            }

            // Extract UserId from token
            Guid? userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value switch
            {
                { } value when Guid.TryParse(value, out Guid guid) => guid,
                _ => (Guid?)null
            };

            if (userId is null)
            {
                var failure = Result.Failure<UploadPdFilesResponse>(new Error(
                    "Authentication.InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            // Read file contents
            var fileDataList = new List<PdFileUploadData>();

            foreach (IFormFile file in files!)
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream, cancellationToken);
                byte[] bytes = memoryStream.ToArray();

                fileDataList.Add(new PdFileUploadData(
                    FileName: file.FileName,
                    ContentType: file.ContentType ?? string.Empty,
                    Content: bytes
                ));
            }

            // Dispatch command
            var command = new UploadPdFilesCommand(
                UploadedBy: userId.Value,
                Files: fileDataList,
                TimePeriod: timePeriod!
            );

            Result<UploadPdFilesResponse> result = await handler.Handle(command, cancellationToken);

            return result switch
            {
                { IsSuccess: true } => Results.Created($"/pd-files", result.Value),
                var failure => CustomResults.Problem(failure)
            };
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("PD Files")
        .WithName("UploadPdFiles")
        .WithSummary("Upload PD files and store metadata in JSON")
        .WithDescription("Upload PD files for a specific time period. Files are stored physically and metadata is maintained in collective_impairment_configs JSON.")
        .DisableAntiforgery();
    }
}
