using System.Globalization;
using System.Text;
using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.Common;
using Application.Files.UploadLgdFiles;
using Application.Files.UploadPdFiles;
using CsvHelper;
using CsvHelper.Configuration;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Files.DownloadPdFile;

/// <summary>
/// Handler for downloading PD files with error indicators in CSV
/// Searches in hierarchical pending folders and flat processed folder
/// </summary>
internal sealed class DownloadPdFileQueryHandler(
    IApplicationDbContext dbContext,
    IOptions<LgdFileStorageOptions> storageOptions,
    ILogger<DownloadPdFileQueryHandler> logger
) : IQueryHandler<DownloadPdFileQuery, DownloadPdFileResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Result<DownloadPdFileResponse>> Handle(
        DownloadPdFileQuery query,
        CancellationToken cancellationToken)
    {
        if (query is null || string.IsNullOrWhiteSpace(query.UploadId))
        {
            return Result.Failure<DownloadPdFileResponse>(Error.Validation(
                "Download.InvalidUploadId",
                "Upload ID is required"));
        }

        if (string.IsNullOrWhiteSpace(query.TimePeriod))
        {
            return Result.Failure<DownloadPdFileResponse>(Error.Validation(
                "Download.InvalidTimePeriod",
                "Time period is required"));
        }

        logger.LogInformation("Downloading PD file - UploadId: {UploadId}, TimePeriod: {TimePeriod}",
            query.UploadId, query.TimePeriod);

        try
        {
            // Fetch PD configuration
            CollectiveImpairmentConfig? pdConfig = await dbContext.CollectiveImpairmentConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Parameter == ParameterType.PD, cancellationToken);

            if (pdConfig is null)
            {
                return Result.Failure<DownloadPdFileResponse>(Error.NotFound(
                    "Config.NotFound",
                    "PD configuration not found"));
            }

            // Parse configuration JSON
            PDConfigurationJson? configJson = JsonSerializer.Deserialize<PDConfigurationJson>(
                pdConfig.ConfigJson, JsonOptions);

            if (configJson?.PdFileUpload is null)
            {
                return Result.Failure<DownloadPdFileResponse>(Error.Validation(
                    "Config.InvalidJson",
                    "Failed to parse PD configuration JSON"));
            }

            // Get time period data
            if (!configJson.PdFileUpload.TryGetValue(query.TimePeriod, out PdTimePeriodData? timePeriodData))
            {
                return Result.Failure<DownloadPdFileResponse>(Error.NotFound(
                    "TimePeriod.NotFound",
                    $"Time period '{query.TimePeriod}' not found"));
            }

            // Find file metadata
            PdFileMetadata? fileMetadata = timePeriodData.Files
                .FirstOrDefault(f => f.UploadId == query.UploadId);

            if (fileMetadata is null)
            {
                return Result.Failure<DownloadPdFileResponse>(Error.NotFound(
                    "File.NotFound",
                    $"File with upload ID '{query.UploadId}' not found"));
            }

            // Build paths
            string rootPath = GetRootPath();
            
            // Hierarchical pending path
            string pdPendingBase = Path.Combine(rootPath, "PD", "pending");
            string pdPendingPath = FileProcessingUtilities.CreateTimePeriodFolderPath(
                pdPendingBase, 
                query.TimePeriod, 
                pdConfig.ConfigJson);

            // FLAT processed path
            string pdProcessedPath = Path.Combine(rootPath, "PD", "processed");

            // Try to find file - check processed (flat) first, then pending (hierarchical)
            string? filePath = null;
            string processedFilePath = Path.Combine(pdProcessedPath, fileMetadata.StoredFileName);
            string pendingFilePath = Path.Combine(pdPendingPath, fileMetadata.StoredFileName);

            if (File.Exists(processedFilePath))
            {
                filePath = processedFilePath;
                logger.LogInformation("Found file in flat processed directory: {FilePath}", filePath);
            }
            else if (File.Exists(pendingFilePath))
            {
                filePath = pendingFilePath;
                logger.LogInformation("Found file in hierarchical pending directory: {FilePath}", filePath);
            }

            if (filePath is null)
            {
                logger.LogWarning("Physical file not found in either directory: {FileName}", fileMetadata.StoredFileName);
                return Result.Failure<DownloadPdFileResponse>(Error.NotFound(
                    "File.PhysicalNotFound",
                    $"Physical file '{fileMetadata.StoredFileName}' not found"));
            }

            // Check if file already has validation error columns
            bool fileAlreadyHasErrorColumn = await FileHasValidationErrorColumn(filePath);

            // Read file and add error indicators if validation errors exist
            byte[] content;
            bool hasErrors = fileMetadata.ValidationResult?.TotalErrors > 0;
            bool hasErrorDetails = hasErrors && 
                                  fileMetadata.ValidationResult?.Errors is not null && 
                                  fileMetadata.ValidationResult.Errors.Count > 0;

            if (fileAlreadyHasErrorColumn)
            {
                // File already has validation error column from processing - return as-is
                logger.LogInformation("File already contains validation error column, returning as-is: {FileName}", 
                    fileMetadata.Name);
                content = await File.ReadAllBytesAsync(filePath, cancellationToken);
            }
            else if (hasErrorDetails)
            {
                // Add error column to CSV with detailed error messages
                logger.LogInformation("Adding error column to CSV with {ErrorCount} validation errors", 
                    fileMetadata.ValidationResult!.Errors!.Count);
                content = await AddErrorColumnToCsv(filePath, fileMetadata.ValidationResult.Errors);
            }
            else if (hasErrors)
            {
                // File has errors but no detailed error list (validation not yet implemented)
                logger.LogWarning("File has {ErrorCount} errors but no detailed error messages. " +
                    "Returning original file without error column.", 
                    fileMetadata.ValidationResult?.TotalErrors);
                content = await File.ReadAllBytesAsync(filePath, cancellationToken);
            }
            else
            {
                // No errors - return original file
                content = await File.ReadAllBytesAsync(filePath, cancellationToken);
            }

            logger.LogInformation("Successfully prepared file for download: {FileName}, HasErrors: {HasErrors}, HasErrorDetails: {HasErrorDetails}, AlreadyHasErrorColumn: {AlreadyHasErrorColumn}",
                fileMetadata.Name, hasErrors, hasErrorDetails, fileAlreadyHasErrorColumn);

            return Result.Success(new DownloadPdFileResponse(
                FileName: fileMetadata.Name,
                ContentType: "text/csv",
                Content: content,
                HasErrors: hasErrors,
                TotalErrors: fileMetadata.ValidationResult?.TotalErrors ?? 0
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error downloading PD file - UploadId: {UploadId}", query.UploadId);
            return Result.Failure<DownloadPdFileResponse>(Error.Failure(
                "Download.Failed",
                $"Failed to download file: {ex.Message}"));
        }
    }

    private string GetRootPath()
    {
        string configuredRoot = storageOptions.Value.RootPath ?? string.Empty;
        string expandedRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.GetTempPath()
            : Environment.ExpandEnvironmentVariables(configuredRoot);

        return Path.IsPathRooted(expandedRoot)
            ? expandedRoot
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedRoot));
    }

    /// <summary>
    /// Checks if the CSV file already has a validation error column
    /// </summary>
    private static async Task<bool> FileHasValidationErrorColumn(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            using var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            });

            await csvReader.ReadAsync();
            csvReader.ReadHeader();
            string[]? headers = csvReader.HeaderRecord;

            if (headers is null)
            {
                return false;
            }

            // Check for common validation error column names
            return headers.Any(h => 
                h.Equals("Validation_Messages", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("ValidationErrors", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Validation Messages", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Errors", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            // If we can't read the file, assume it doesn't have error column
            return false;
        }
    }

    private static async Task<byte[]> AddErrorColumnToCsv(
        string filePath, 
        List<UploadPdFiles.ValidationError> errors)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        // Read original CSV
        using var reader = new StreamReader(filePath);
        using var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        // Read headers
        await csvReader.ReadAsync();
        csvReader.ReadHeader();
        string[]? headers = csvReader.HeaderRecord ?? throw new InvalidOperationException("CSV file has no headers");

        // Write headers with error column
        foreach (string header in headers)
        {
            csv.WriteField(header);
        }
        csv.WriteField("ValidationErrors");
        await csv.NextRecordAsync();

        // Group errors by row for efficient lookup
        // Note: Row indices from Saral.FileProcessor are 0-based
        var errorsByRow = errors
            .GroupBy(e => e.Row)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Write data rows - use 0-based indexing to match validation error row indices
        int currentRow = 0;
        while (await csvReader.ReadAsync())
        {
            // Write original fields
            for (int i = 0; i < headers.Length; i++)
            {
                csv.WriteField(csvReader.GetField(i));
            }

            // Write error column - use 0-based row index
            if (errorsByRow.TryGetValue(currentRow, out List<UploadPdFiles.ValidationError>? rowErrors))
            {
                string errorMessage = string.Join("; ", rowErrors.Select(e => $"{e.Column}: {e.Error}"));
                csv.WriteField(errorMessage);
            }
            else
            {
                csv.WriteField(string.Empty);
            }

            await csv.NextRecordAsync();
            currentRow++;
        }

        await writer.FlushAsync();
        return memoryStream.ToArray();
    }
}
