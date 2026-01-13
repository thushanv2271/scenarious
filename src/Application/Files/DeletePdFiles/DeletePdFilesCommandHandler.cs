using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.DTOs.PD;
using Application.Files.Common;
using Application.Files.UploadLgdFiles;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

// Type aliases for clarity
using PDConfigurationJson = Application.DTOs.PD.PDConfigJson;
using PdTimePeriodData = Application.DTOs.PD.PeriodFileUploadData;
using PdFileMetadata = Application.DTOs.PD.UploadedFileInfo;

namespace Application.Files.DeletePdFiles;

/// <summary>
/// Handler for deleting PD files from file system and JSON configuration
/// </summary>
internal sealed class DeletePdFilesCommandHandler(
    IApplicationDbContext dbContext,
    IOptions<LgdFileStorageOptions> storageOptions,
    ILogger<DeletePdFilesCommandHandler> logger
) : ICommandHandler<DeletePdFilesCommand, DeletePdFilesResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<Result<DeletePdFilesResponse>> Handle(
        DeletePdFilesCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null || command.UploadIds is null || command.UploadIds.Count == 0)
        {
            return Result.Failure<DeletePdFilesResponse>(Error.Validation(
                "Delete.NoFiles",
                "No file IDs provided for deletion"));
        }

        if (string.IsNullOrWhiteSpace(command.TimePeriod))
        {
            return Result.Failure<DeletePdFilesResponse>(Error.Validation(
                "Delete.InvalidTimePeriod",
                "Time period is required"));
        }

        logger.LogInformation("Starting PD file deletion for {FileCount} files, TimePeriod: {TimePeriod}",
            command.UploadIds.Count, command.TimePeriod);

        try
        {
            // Fetch PD configuration
            CollectiveImpairmentConfig? pdConfig = await dbContext.CollectiveImpairmentConfigs
                .FirstOrDefaultAsync(c => c.Parameter == ParameterType.PD, cancellationToken);

            if (pdConfig is null)
            {
                return Result.Failure<DeletePdFilesResponse>(Error.NotFound(
                    "Config.NotFound",
                    "PD configuration not found"));
            }

            // Parse existing configuration
            PDConfigurationJson? configJson = JsonSerializer.Deserialize<PDConfigurationJson>(
                pdConfig.ConfigJson, JsonOptions);

            if (configJson?.PdFileUpload is null)
            {
                return Result.Failure<DeletePdFilesResponse>(Error.Validation(
                    "Config.InvalidJson",
                    "Failed to parse PD configuration JSON"));
            }

            // Get time period data
            if (!configJson.PdFileUpload.TryGetValue(command.TimePeriod, out PdTimePeriodData? timePeriodData))
            {
                return Result.Failure<DeletePdFilesResponse>(Error.NotFound(
                    "TimePeriod.NotFound",
                    $"Time period '{command.TimePeriod}' not found in configuration"));
            }

            // Build hierarchical storage paths based on frequency
            string rootPath = GetRootPath();
            string pdPendingBasePath = Path.Combine(rootPath, "PD", "pending");
            string pdProcessedBasePath = Path.Combine(rootPath, "PD", "processed");

            // Create hierarchical folder structure for the time period
            string pdPendingTimePeriodPath = FileProcessingUtilities.CreateTimePeriodFolderPath(
                pdPendingBasePath, 
                command.TimePeriod, 
                pdConfig.ConfigJson);
            
            string pdProcessedTimePeriodPath = FileProcessingUtilities.CreateTimePeriodFolderPath(
                pdProcessedBasePath, 
                command.TimePeriod, 
                pdConfig.ConfigJson);

            logger.LogInformation("PD file paths - Pending: {PendingPath}, Processed: {ProcessedPath}",
                pdPendingTimePeriodPath, pdProcessedTimePeriodPath);

            var deletedFiles = new List<string>();
            var failedFiles = new Dictionary<string, string>();
            var deletedFileNames = new List<string>();

            foreach (string uploadId in command.UploadIds)
            {
                PdFileMetadata? fileMetadata = timePeriodData.Files
                    .FirstOrDefault(f => f.UploadId == uploadId);

                if (fileMetadata is null)
                {
                    failedFiles[uploadId] = "File not found in configuration";
                    logger.LogWarning("File with UploadId {UploadId} not found in configuration", uploadId);
                    continue;
                }

                // Try to delete physical file from both directories using hierarchical paths
                bool physicalFileDeleted = false;
                var deletionErrors = new List<string>();

                // Check pending directory with hierarchical path
                string pendingFilePath = Path.Combine(pdPendingTimePeriodPath, fileMetadata.StoredFileName);
                logger.LogInformation("Checking pending file: {FilePath}", pendingFilePath);
                
                if (File.Exists(pendingFilePath))
                {
                    try
                    {
                        File.Delete(pendingFilePath);
                        logger.LogInformation("? Deleted file from pending: {FilePath}", pendingFilePath);
                        physicalFileDeleted = true;
                        deletedFileNames.Add(fileMetadata.StoredFileName);
                    }
                    catch (Exception ex)
                    {
                        deletionErrors.Add($"Pending: {ex.Message}");
                        logger.LogError(ex, "? Failed to delete file from pending: {FilePath}", pendingFilePath);
                    }
                }
                else
                {
                    logger.LogInformation("File not found in pending directory: {FilePath}", pendingFilePath);
                }

                // Check processed directory with hierarchical path
                string processedFilePath = Path.Combine(pdProcessedTimePeriodPath, fileMetadata.StoredFileName);
                logger.LogInformation("Checking processed file: {FilePath}", processedFilePath);
                
                if (File.Exists(processedFilePath))
                {
                    try
                    {
                        File.Delete(processedFilePath);
                        logger.LogInformation("? Deleted file from processed: {FilePath}", processedFilePath);
                        physicalFileDeleted = true;
                        deletedFileNames.Add(fileMetadata.StoredFileName);
                    }
                    catch (Exception ex)
                    {
                        deletionErrors.Add($"Processed: {ex.Message}");
                        logger.LogError(ex, "? Failed to delete file from processed: {FilePath}", processedFilePath);
                    }
                }
                else
                {
                    logger.LogInformation("File not found in processed directory: {FilePath}", processedFilePath);
                }

                if (!physicalFileDeleted && deletionErrors.Count == 0)
                {
                    // File doesn't exist in either location - still remove from JSON
                    logger.LogWarning("?? Physical file not found: {FileName}, removing from JSON anyway",
                        fileMetadata.StoredFileName);
                }

                if (deletionErrors.Count > 0 && !physicalFileDeleted)
                {
                    failedFiles[uploadId] = string.Join("; ", deletionErrors);
                    continue;
                }

                // Remove from JSON configuration
                timePeriodData.Files.RemoveAll(f => f.UploadId == uploadId);
                deletedFiles.Add(uploadId);

                logger.LogInformation("Successfully deleted file: {FileName} (UploadId: {UploadId})",
                    fileMetadata.Name, uploadId);
            }

            // Delete from file_validation_results table if exists
            if (deletedFileNames.Count > 0)
            {
                try
                {
                    logger.LogInformation("Cleaning up validation records for {Count} deleted files", deletedFileNames.Count);
                    
                    List<Domain.Files.FileValidationResult> validationResults = await dbContext.FileValidationResults
                        .Where(fvr => deletedFileNames.Contains(fvr.Filename))
                        .ToListAsync(cancellationToken);

                    if (validationResults.Count > 0)
                    {
                        dbContext.FileValidationResults.RemoveRange(validationResults);
                        logger.LogInformation("? Deleted {Count} validation records for deleted files", validationResults.Count);
                    }
                    else
                    {
                        logger.LogInformation("No validation records found for deleted files");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete validation records. Continuing with deletion.");
                    // Continue anyway - don't fail the entire operation
                }
            }

            // Update configuration JSON
            string updatedConfigJson = JsonSerializer.Serialize(configJson, JsonOptions);
            pdConfig.ConfigJson = updatedConfigJson;
            pdConfig.UpdatedBy = command.DeletedBy;
            pdConfig.UpdatedDate = DateTime.UtcNow;

            dbContext.CollectiveImpairmentConfigs.Update(pdConfig);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("? PD file deletion completed: {DeletedCount} deleted, {FailedCount} failed",
                deletedFiles.Count, failedFiles.Count);

            return Result.Success(new DeletePdFilesResponse(
                DeletedFiles: deletedFiles,
                FailedFiles: failedFiles,
                TotalDeleted: deletedFiles.Count,
                TotalFailed: failedFiles.Count
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "? Error deleting PD files for TimePeriod: {TimePeriod}", command.TimePeriod);
            return Result.Failure<DeletePdFilesResponse>(Error.Failure(
                "Delete.Failed",
                $"Failed to delete files: {ex.Message}"));
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
}
