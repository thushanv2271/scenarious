using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Files.UploadLgdFiles;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for uploading multiple LGD files with facility status classification.
/// </summary>
/// <remarks>
/// This endpoint accepts multiple LGD file uploads via HTTP POST requests, validates each file,
/// stores the files in organized folder structures based on facility status,
/// and updates the collective_impairment_configs table with file metadata.
/// Files are organized as: /pending/LGD/{FacilityStatus}/
/// Maximum of 10 files can be uploaded per request.
/// </remarks>
internal sealed class UploadLgdFiles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lgd-files", async (
            IFormFileCollection files,
            ICommandHandler<UploadLgdFilesCommand, UploadLgdFilesResponse> handler,
            HttpContext context,
            [FromForm] string year,
            [FromForm] string facilityStatus,
            [FromForm] string? timePeriodFrom,
            [FromForm] string? timePeriodTo,
            [FromForm] string? financialYearEnd,
            CancellationToken cancellationToken) =>
        {
            // Enhanced validation
            Error? validationResult = (files?.Count ?? 0, year, facilityStatus) switch
            {
                (0, _, _) => new Error("Files.Empty", "No files provided", ErrorType.Validation),
                ( > 10, _, _) => new Error("Files.TooMany", "Maximum 10 files allowed per upload", ErrorType.Validation),
                (_, null or "", _) => new Error("Year.Required", "Year is required", ErrorType.Validation),
                (_, _, null or "") => new Error("FacilityStatus.Required", "Facility status is required", ErrorType.Validation),
                _ => null
            };

            if (validationResult is not null)
            {
                return CustomResults.Problem(Result.Failure<UploadLgdFilesResponse>(validationResult));
            }

            // Validate financialYearEnd format if provided (YYYY-MM-DD)
            if (!string.IsNullOrWhiteSpace(financialYearEnd))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(financialYearEnd, @"^\d{4}-\d{2}-\d{2}$"))
                {
                    var failure = Result.Failure<UploadLgdFilesResponse>(new Error(
                        "FinancialYearEnd.InvalidFormat",
                        "Financial year end must be in format YYYY-MM-DD (e.g., '2025-12-31')",
                        ErrorType.Validation
                    ));
                    return CustomResults.Problem(failure);
                }

                // Validate that it's a valid date
                if (!DateTime.TryParse(financialYearEnd, System.Globalization.CultureInfo.InvariantCulture, out _))
                {
                    var failure = Result.Failure<UploadLgdFilesResponse>(new Error(
                        "FinancialYearEnd.InvalidDate",
                        "Financial year end is not a valid date",
                        ErrorType.Validation
                    ));
                    return CustomResults.Problem(failure);
                }
            }

            // Validate timePeriod if provided
            TimePeriodRequest? timePeriod = null;
            if (!string.IsNullOrWhiteSpace(timePeriodFrom) && !string.IsNullOrWhiteSpace(timePeriodTo))
            {
                timePeriod = new TimePeriodRequest(timePeriodFrom, timePeriodTo);
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
                    return CustomResults.Problem(Result.Failure<UploadLgdFilesResponse>(fileValidation));
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
                var failure = Result.Failure<UploadLgdFilesResponse>(new Error(
                    "Authentication.InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failure);
            }

            // Read file contents
            var fileDataList = new List<LgdFileUploadData>();

            foreach (IFormFile file in files!)
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream, cancellationToken);
                byte[] bytes = memoryStream.ToArray();

                fileDataList.Add(new LgdFileUploadData(
                    FileName: file.FileName,
                    ContentType: file.ContentType ?? string.Empty,
                    Content: bytes
                ));
            }

            // Dispatch command
            var command = new UploadLgdFilesCommand(
                UploadedBy: userId.Value,
                Files: fileDataList,
                Year: year!,
                FacilityStatus: facilityStatus!,
                TimePeriod: timePeriod,
                FinancialYearEnd: financialYearEnd
            );

            Result<UploadLgdFilesResponse> result = await handler.Handle(command, cancellationToken);

            return result switch
            {
                { IsSuccess: true } => Results.Created($"/lgd-files", result.Value),
                var failure => CustomResults.Problem(failure)
            };
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("LGD Files")
        .WithName("UploadLgdFiles")
        .WithSummary("Upload multiple LGD files with facility status classification")
        .WithDescription("Upload LGD files organized by facility status (OpenFacility/ClosedFacility) with optional time period and financial year end. Files are stored in facility-specific directories, with year information maintained in JSON metadata. Financial year end can be manually specified in YYYY-MM-DD format (e.g., '2025-12-31'), otherwise defaults to December 31st of the year.")
        .DisableAntiforgery();
    }
}
