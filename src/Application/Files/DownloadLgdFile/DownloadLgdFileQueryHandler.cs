using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.UploadLgdFiles;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Files.DownloadLgdFile;

/// <summary>
/// Handler for downloading LGD files.
/// </summary>
internal sealed class DownloadLgdFileQueryHandler(
    IApplicationDbContext dbContext,
    IOptions<LgdFileStorageOptions> storageOptions,
    ILogger<DownloadLgdFileQueryHandler> logger
) : IQueryHandler<DownloadLgdFileQuery, DownloadLgdFileResult>
{
    public async Task<Result<DownloadLgdFileResult>> Handle(
        DownloadLgdFileQuery query,
        CancellationToken cancellationToken)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(query.FileId))
        {
            return Result.Failure<DownloadLgdFileResult>(Error.Problem(
                "FileId.Required",
                "File ID is required."));
        }

        if (string.IsNullOrWhiteSpace(query.Year))
        {
            return Result.Failure<DownloadLgdFileResult>(Error.Problem(
                "Year.Required",
                "Year is required."));
        }

        if (string.IsNullOrWhiteSpace(query.FacilityStatus) ||
            query.FacilityStatus != "OpenFacility" && query.FacilityStatus != "ClosedFacility")
        {
            return Result.Failure<DownloadLgdFileResult>(Error.Problem(
                "FacilityStatus.Invalid",
                "Facility status must be either 'OpenFacility' or 'ClosedFacility'."));
        }

        // Fetch LGD configuration
        CollectiveImpairmentConfig? config = await dbContext.CollectiveImpairmentConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Parameter == ParameterType.LGD, cancellationToken);

        if (config is null)
        {
            return Result.Failure<DownloadLgdFileResult>(Error.NotFound(
                "LgdConfig.NotFound",
                "No LGD configuration found."));
        }

        // Parse JSON to find file metadata
        JsonDocument configDoc;
        try
        {
            configDoc = JsonDocument.Parse(config.ConfigJson);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse LGD configuration JSON");
            return Result.Failure<DownloadLgdFileResult>(Error.Problem(
                "LgdConfig.InvalidJson",
                "LGD configuration contains invalid JSON."));
        }

        // Navigate JSON to find the file
        if (!configDoc.RootElement.TryGetProperty("lgdFileUpload", out JsonElement lgdFileUpload))
        {
            return Result.Failure<DownloadLgdFileResult>(Error.NotFound(
                "File.NotFound",
                "LGD file upload configuration not found."));
        }

        if (!lgdFileUpload.TryGetProperty(query.Year, out JsonElement yearData))
        {
            return Result.Failure<DownloadLgdFileResult>(Error.NotFound(
                "File.NotFound",
                $"No data found for year {query.Year}."));
        }

        string facilityPropertyName = query.FacilityStatus == "OpenFacility" ? "openFacility" : "closedFacility";

        if (!yearData.TryGetProperty(facilityPropertyName, out JsonElement facilityData))
        {
            return Result.Failure<DownloadLgdFileResult>(Error.NotFound(
                "File.NotFound",
                $"No {query.FacilityStatus} data found for year {query.Year}."));
        }

        if (!facilityData.TryGetProperty("files", out JsonElement filesArray))
        {
            return Result.Failure<DownloadLgdFileResult>(Error.NotFound(
                "File.NotFound",
                "No files found in configuration."));
        }

        // Find the specific file
        JsonElement? targetFile = null;
        foreach (JsonElement file in filesArray.EnumerateArray())
        {
            if (file.TryGetProperty("id", out JsonElement idElement) &&
                idElement.GetString() == query.FileId)
            {
                targetFile = file;
                break;
            }
        }

        if (targetFile is null)
        {
            return Result.Failure<DownloadLgdFileResult>(Error.NotFound(
                "File.NotFound",
                $"File with ID '{query.FileId}' not found in configuration."));
        }

        // Extract file metadata
        string? storedFileName = targetFile.Value.TryGetProperty("storedFileName", out JsonElement sfn)
            ? sfn.GetString()
            : null;

        string? originalFileName = targetFile.Value.TryGetProperty("name", out JsonElement name)
            ? name.GetString()
            : null;

        string? fileType = targetFile.Value.TryGetProperty("type", out JsonElement type)
            ? type.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            return Result.Failure<DownloadLgdFileResult>(Error.Problem(
                "File.MetadataIncomplete",
                "Stored file name not found in configuration."));
        }

        // Build physical file paths
        string configuredRoot = storageOptions.Value.RootPath ?? string.Empty;
        string expandedRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.GetTempPath()
            : Environment.ExpandEnvironmentVariables(configuredRoot);

        string rootPath = Path.IsPathRooted(expandedRoot)
            ? expandedRoot
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedRoot));

        // Check pending directory first
        string pendingPath = Path.Combine(rootPath, "LGD", "pending", query.FacilityStatus, storedFileName);

        if (File.Exists(pendingPath))
        {
            FileInfo fileInfo = new(pendingPath);
            string contentType = GetContentType(fileType);

            return Result.Success(new DownloadLgdFileResult(
                PhysicalPath: pendingPath,
                OriginalFileName: originalFileName ?? storedFileName,
                ContentType: contentType,
                Size: fileInfo.Length
            ));
        }

        // Check processed directory
        string processedPath = Path.Combine(rootPath, "LGD", "processed", query.FacilityStatus, storedFileName);

        if (File.Exists(processedPath))
        {
            FileInfo fileInfo = new(processedPath);
            string contentType = GetContentType(fileType);

            return Result.Success(new DownloadLgdFileResult(
                PhysicalPath: processedPath,
                OriginalFileName: originalFileName ?? storedFileName,
                ContentType: contentType,
                Size: fileInfo.Length
            ));
        }

        // File not found in either location
        logger.LogWarning("File not found in pending or processed directories: {FileName}", storedFileName);
        return Result.Failure<DownloadLgdFileResult>(Error.NotFound(
            "File.PhysicalFileNotFound",
            $"Physical file '{storedFileName}' not found in pending or processed directories."));
    }

    private static string GetContentType(string? fileType)
    {
        return fileType?.ToUpperInvariant() switch
        {
            "CSV" => "text/csv",
            "XLSX" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "XLS" => "application/vnd.ms-excel",
            _ => "application/octet-stream"
        };
    }
}
