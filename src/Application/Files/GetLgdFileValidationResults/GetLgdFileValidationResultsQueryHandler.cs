using System.Globalization;
using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.DTOs.LGD;
using Application.Files.Common;
using Application.Files.UploadLgdFiles;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Files.GetLgdFileValidationResults;

internal sealed class GetLgdFileValidationResultsQueryHandler(
    IApplicationDbContext dbContext,
    IOptions<LgdFileStorageOptions> storageOptions,
    ILogger<GetLgdFileValidationResultsQueryHandler> logger)
    : IQueryHandler<GetLgdFileValidationResultsQuery, LgdFileValidationResultsResponse>
{
    public async Task<Result<LgdFileValidationResultsResponse>> Handle(
        GetLgdFileValidationResultsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Year))
        {
            logger.LogWarning("GetLgdFileValidationResults missing Year.");
            return Result.Success(new LgdFileValidationResultsResponse
            {
                Data = [],
                Total = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        if (string.IsNullOrWhiteSpace(request.FacilityStatus))
        {
            logger.LogWarning("GetLgdFileValidationResults missing FacilityStatus.");
            return Result.Success(new LgdFileValidationResultsResponse
            {
                Data = [],
                Total = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        // Validate facility status
        if (request.FacilityStatus != "OpenFacility" && request.FacilityStatus != "ClosedFacility")
        {
            logger.LogWarning("GetLgdFileValidationResults invalid FacilityStatus: {FacilityStatus}", request.FacilityStatus);
            return Result.Success(new LgdFileValidationResultsResponse
            {
                Data = [],
                Total = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        CollectiveImpairmentConfig? config = await dbContext.CollectiveImpairmentConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Parameter == ParameterType.LGD, cancellationToken);

        if (config is null)
        {
            logger.LogWarning("GetLgdFileValidationResults no LGD config found");
            return Result.Success(new LgdFileValidationResultsResponse
            {
                Data = [],
                Total = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        // Parse config JSON to get file list with validation results
        JsonDocument? configDoc;
        try
        {
            configDoc = JsonDocument.Parse(config.ConfigJson);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "GetLgdFileValidationResults failed to parse config JSON");
            return Result.Success(new LgdFileValidationResultsResponse
            {
                Data = [],
                Total = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        if (!configDoc.RootElement.TryGetProperty("lgdFileUpload", out JsonElement lgdFileUpload) ||
            !lgdFileUpload.TryGetProperty(request.Year, out JsonElement yearData))
        {
            logger.LogWarning("GetLgdFileValidationResults no data for year: {Year}", request.Year);
            return Result.Success(new LgdFileValidationResultsResponse
            {
                Data = [],
                Total = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        // Get facility data
        string facilityPropertyName = request.FacilityStatus == "OpenFacility" ? "openFacility" : "closedFacility";

        if (!yearData.TryGetProperty(facilityPropertyName, out JsonElement facilityData) ||
            !facilityData.TryGetProperty("files", out JsonElement filesArray))
        {
            logger.LogWarning("GetLgdFileValidationResults no files found for facility: {Facility}", request.FacilityStatus);
            return Result.Success(new LgdFileValidationResultsResponse
            {
                Data = [],
                Total = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        // Build directory path with new structure
        string configuredRoot = storageOptions.Value.RootPath ?? string.Empty;
        string expandedRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.GetTempPath()
            : Environment.ExpandEnvironmentVariables(configuredRoot);

        string rootPath = Path.IsPathRooted(expandedRoot)
            ? expandedRoot
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedRoot));

        // Check both pending and processed directories
        string pendingFolder = Path.Combine(rootPath, "LGD", "pending", request.FacilityStatus);
        string processedFolder = Path.Combine(rootPath, "LGD", "processed", request.FacilityStatus);

        // Extract validation results from JSON
        var validationResults = new List<LgdFileValidationResultDto>();
        int idCounter = 1;

        foreach (JsonElement fileElement in filesArray.EnumerateArray())
        {
            string? storedFileName = fileElement.TryGetProperty("storedFileName", out JsonElement sfn)
                ? sfn.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(storedFileName))
            {
                continue;
            }

            // Get validation result from JSON
            int totalRows = 0;
            int totalErrors = 0;
            string status = "Unknown";

            if (fileElement.TryGetProperty("validationResult", out JsonElement validationResultElement))
            {
                totalRows = validationResultElement.TryGetProperty("total_rows", out JsonElement trElement)
                    ? trElement.GetInt32()
                    : 0;
                totalErrors = validationResultElement.TryGetProperty("total_errors", out JsonElement teElement)
                    ? teElement.GetInt32()
                    : 0;
                status = validationResultElement.TryGetProperty("status", out JsonElement stElement)
                    ? stElement.GetString() ?? "Unknown"
                    : "Unknown";
            }

            // Determine physical path
            string? physicalPath = null;
            string pendingPath = Path.Combine(pendingFolder, storedFileName);
            string processedPath = Path.Combine(processedFolder, storedFileName);

            if (File.Exists(pendingPath))
            {
                physicalPath = pendingPath;
            }
            else if (File.Exists(processedPath))
            {
                physicalPath = processedPath;
            }

            // Get uploaded file ID
            string? uploadedFileId = fileElement.TryGetProperty("id", out JsonElement idElement)
                ? idElement.GetString()
                : null;

            // Map status string to enum
            LgdFileValidationStatus validationStatus = status == "Valid"
                ? LgdFileValidationStatus.Valid
                : LgdFileValidationStatus.Invalid;

            validationResults.Add(new LgdFileValidationResultDto
            {
                Id = idCounter++,
                Filename = storedFileName,
                TotalRows = totalRows,
                TotalErrors = totalErrors,
                Status = validationStatus,
                PhysicalPath = physicalPath,
                UploadedFileId = uploadedFileId
            });
        }

        // Apply status filtering
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "all")
        {
            validationResults = request.Status.ToUpperInvariant() switch
            {
                "VALID" => validationResults.Where(x => x.Status == LgdFileValidationStatus.Valid).ToList(),
                "INVALID" => validationResults.Where(x => x.Status == LgdFileValidationStatus.Invalid).ToList(),
                _ => validationResults
            };
        }

        int totalCount = validationResults.Count;

        // Apply pagination
        var paginatedResults = validationResults
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var response = new LgdFileValidationResultsResponse
        {
            Data = paginatedResults,
            Total = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return Result.Success(response);
    }
}
