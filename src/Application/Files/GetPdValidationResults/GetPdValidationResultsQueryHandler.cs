using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.UploadPdFiles;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.Files.GetPdValidationResults;

/// <summary>
/// Handler for retrieving PD file validation results from JSON configuration
/// FIXED VERSION: Correctly reads totalRows and totalErrors from validationResult
/// </summary>
internal sealed class GetPdValidationResultsQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetPdValidationResultsQueryHandler> logger
) : IQueryHandler<GetPdValidationResultsQuery, PaginatedValidationResultsResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Result<PaginatedValidationResultsResponse>> Handle(
        GetPdValidationResultsQuery query,
        CancellationToken cancellationToken)
    {
        if (query is null)
        {
            return Result.Failure<PaginatedValidationResultsResponse>(Error.NullValue);
        }

        try
        {
            logger.LogInformation("Retrieving PD validation results - Page: {PageNumber}, PageSize: {PageSize}, TimePeriod: {TimePeriod}, Status: {Status}",
                query.PageNumber, query.PageSize, query.TimePeriod ?? "All", query.Status ?? "All");

            // Fetch PD configuration
            CollectiveImpairmentConfig? pdConfig = await dbContext.CollectiveImpairmentConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Parameter == ParameterType.PD, cancellationToken);

            if (pdConfig is null)
            {
                return Result.Failure<PaginatedValidationResultsResponse>(Error.NotFound(
                    "Config.NotFound",
                    "PD configuration not found"));
            }

            // Check if ConfigJson is null or empty
            if (string.IsNullOrWhiteSpace(pdConfig.ConfigJson))
            {
                logger.LogWarning("ConfigJson is null or empty");
                return Result.Failure<PaginatedValidationResultsResponse>(Error.Validation(
                    "Config.InvalidJson",
                    "Configuration JSON is empty or null"));
            }

            // Parse configuration JSON - Log for debugging
            logger.LogDebug("Parsing PD configuration JSON, length: {Length}", pdConfig.ConfigJson.Length);
            
            PDConfigurationJson? configJson = JsonSerializer.Deserialize<PDConfigurationJson>(
                pdConfig.ConfigJson, JsonOptions);

            if (configJson?.PdFileUpload is null)
            {
                logger.LogWarning("PdFileUpload is null in configuration");
                return Result.Success(new PaginatedValidationResultsResponse(
                    Items: new List<PdFileValidationInfo>(),
                    TotalCount: 0,
                    PageNumber: query.PageNumber,
                    PageSize: query.PageSize,
                    TotalPages: 0
                ));
            }

            // Collect all files from all time periods
            var allFiles = new List<PdFileValidationInfo>();

            foreach ((string timePeriod, PdTimePeriodData timePeriodData) in configJson.PdFileUpload)
            {
                // Filter by time period if specified
                if (!string.IsNullOrWhiteSpace(query.TimePeriod) && 
                    !timePeriod.Equals(query.TimePeriod, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                logger.LogDebug("Processing time period: {TimePeriod} with {FileCount} files", 
                    timePeriod, timePeriodData.Files.Count);

                foreach (PdFileMetadata file in timePeriodData.Files)
                {
                    // CRITICAL FIX: Read actual values from ValidationResult
                    int totalRows = file.ValidationResult?.TotalRows ?? 0;
                    int totalErrors = file.ValidationResult?.TotalErrors ?? 0;
                    
                    logger.LogDebug("File {FileName}: IsValidated={IsValidated}, TotalRows={TotalRows}, TotalErrors={TotalErrors}", 
                        file.Name, file.IsValidated, totalRows, totalErrors);

                    // Normalize status based on ACTUAL validation data
                    string normalizedStatus;
                    if (file.IsValidated)
                    {
                        // If validated, status is based on validation errors only
                        normalizedStatus = totalErrors == 0 ? "valid" : "invalid";
                    }
                    else
                    {
                        // If not validated, status is pending
                        normalizedStatus = "pending";
                    }

                    // Filter by status if specified (AFTER calculating normalized status)
                    if (!string.IsNullOrWhiteSpace(query.Status))
                    {
                        bool matchesStatus = query.Status.ToUpperInvariant() switch
                        {
                            "VALID" => normalizedStatus == "valid",
                            "INVALID" => normalizedStatus == "invalid",
                            "PENDING" => normalizedStatus == "pending",
                            _ => true // "all" or unrecognized status
                        };

                        if (!matchesStatus)
                        {
                            logger.LogDebug("File {FileName} filtered out: status={Status}, query={QueryStatus}", 
                                file.Name, normalizedStatus, query.Status);
                            continue;
                        }
                    }

                    // Build error list from ValidationResult
                    List<ValidationErrorInfo>? errorsList = null;
                    if (file.ValidationResult?.Errors != null && file.ValidationResult.Errors.Count > 0)
                    {
                        errorsList = file.ValidationResult.Errors
                            .Select(e => new ValidationErrorInfo(e.Row, e.Column, e.Error))
                            .ToList();
                        
                        logger.LogDebug("File {FileName} has {ErrorCount} validation errors", 
                            file.Name, errorsList.Count);
                    }

                    var validationInfo = new PdFileValidationInfo(
                        UploadId: file.UploadId,
                        FileName: file.Name,
                        TimePeriod: timePeriod,
                        Status: normalizedStatus,
                        TotalRows: totalRows,  // Use actual values from ValidationResult
                        TotalErrors: totalErrors,  // Use actual values from ValidationResult
                        IsValidated: file.IsValidated,
                        UploadedAt: file.UploadedAt,
                        Errors: errorsList
                    );

                    allFiles.Add(validationInfo);
                    
                    logger.LogDebug("Added file to results: {FileName}, Status={Status}, TotalRows={TotalRows}, TotalErrors={TotalErrors}", 
                        file.Name, normalizedStatus, totalRows, totalErrors);
                }
            }

            // Sort by upload date descending
            allFiles = allFiles.OrderByDescending(f => f.UploadedAt).ToList();

            // Apply pagination
            int totalCount = allFiles.Count;
            int totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

            var pagedFiles = allFiles
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            logger.LogInformation("Retrieved {Count} PD validation results (Page {PageNumber} of {TotalPages}), Total in system: {TotalCount}",
                pagedFiles.Count, query.PageNumber, totalPages, totalCount);

            return Result.Success(new PaginatedValidationResultsResponse(
                Items: pagedFiles,
                TotalCount: totalCount,
                PageNumber: query.PageNumber,
                PageSize: query.PageSize,
                TotalPages: totalPages
            ));
        }
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "JSON parsing error retrieving PD validation results");
            return Result.Failure<PaginatedValidationResultsResponse>(Error.Failure(
                "Query.JsonParseError",
                $"Failed to parse JSON configuration: {jsonEx.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving PD validation results");
            return Result.Failure<PaginatedValidationResultsResponse>(Error.Failure(
                "Query.Failed",
                $"Failed to retrieve validation results: {ex.Message}"));
        }
    }
}
