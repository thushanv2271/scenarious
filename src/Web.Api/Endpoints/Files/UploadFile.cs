using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Files.UploadFile;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for uploading multiple files to the server with validation and metadata storage.
/// </summary>
/// <remarks>
/// This endpoint accepts multiple file uploads via HTTP POST requests, validates each file type and content,
/// stores the files physically on the server in organized folder structures, and persists metadata to the database. 
/// It supports Excel (.xlsx, .xls) and CSV file formats. The uploaded files are assigned unique identifiers and 
/// stored with timestamp-based filenames to prevent conflicts. Authentication is required, and the user must have 
/// the PDSetupAccess permission. Files are organized in folders based on collective impairment type and time period.
/// 
/// Maximum of 10 files can be uploaded per request.
/// </remarks>
internal sealed class UploadFile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/files", async (
            IFormFileCollection files,
            ICommandHandler<UploadFileCommand, UploadFileResponse> handler,
            HttpContext context,
            [FromForm] string collectiveImpairmentType,
            [FromForm] string timePeriod,
            CancellationToken cancellationToken) =>
        {
            // Enhanced validation with pattern matching
            Error? validationResult = (files?.Count ?? 0, collectiveImpairmentType, timePeriod) switch
            {
                (0, _, _) => new Error("Files.Empty", "No files provided", ErrorType.Validation),
                (> 10, _, _) => new Error("Files.TooMany", "Maximum 10 files allowed per upload", ErrorType.Validation),
                (_, null or "", _) => new Error("CollectiveImpairmentType.Required", "Collective impairment type is required", ErrorType.Validation),
                (_, _, null or "") => new Error("TimePeriod.Required", "Time period is required", ErrorType.Validation),
                _ => null
            };

            if (validationResult is not null)
            {
                return CustomResults.Problem(Result.Failure<UploadFileResponse>(validationResult));
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
                    return CustomResults.Problem(Result.Failure<UploadFileResponse>(fileValidation));
                }
            }

            // Extract UserId from token with modern pattern matching
            Guid? userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value switch
            {
                { } value when Guid.TryParse(value, out Guid guid) => guid,
                _ => (Guid?)null
            };

            if (userId is null)
            {
                var failure = Result.Failure<UploadFileResponse>(new Error(
                    "Authentication.InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            // Read file contents with modern using declarations
            var fileDataList = new List<FileUploadData>();
            
            foreach (IFormFile file in files!)
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream, cancellationToken);
                byte[] bytes = memoryStream.ToArray();

                fileDataList.Add(new FileUploadData(
                    FileName: file.FileName,
                    ContentType: file.ContentType ?? string.Empty,
                    Content: bytes
                ));
            }

            // Dispatch command via the handler (no MediatR)
            var command = new UploadFileCommand(
                UploadedBy: userId.Value,
                Files: fileDataList,
                CollectiveImpairmentType: collectiveImpairmentType!,
                TimePeriod: timePeriod!
            );

            Result<UploadFileResponse> result = await handler.Handle(command, cancellationToken);

            return result switch
            {
                { IsSuccess: true } => Results.Created($"/files", result.Value),
                var failure => CustomResults.Problem(failure)
            };
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Files")
        // Disable antiforgery for API endpoint used by Swagger/clients
        .DisableAntiforgery();
    }
}
