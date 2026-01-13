using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.Common;
using Application.Files.UploadLgdFiles;
using Application.Files.UploadPdFiles;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saral.FileProcessor.Core.Services;
using Saral.FileProcessor.IO;
using SharedKernel;

namespace Application.Files.ProcessPdFilesByIds;

/// <summary>
/// Handler for processing PD files by IDs and storing validation results in both JSON and database
/// Uses Saral.FileProcessor for comprehensive validation and adds error columns to CSV files
/// Reads from hierarchical pending folders, moves to FLAT processed folder for valid files
/// </summary>
internal sealed class ProcessPdFilesByIdsCommandHandler(
    IApplicationDbContext dbContext,
    IFileProcessingService fileProcessingService,
    IOptions<LgdFileStorageOptions> storageOptions,
    ILogger<ProcessPdFilesByIdsCommandHandler> logger
) : ICommandHandler<ProcessPdFilesByIdsCommand, ProcessPdFilesByIdsResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never, // Include null values
        // CRITICAL FIX: Ensure nested objects with [JsonPropertyName] are serialized correctly
        ReferenceHandler = null  // Ensure no reference loops, serialize fully
    };

    public async Task<Result<ProcessPdFilesByIdsResponse>> Handle(
        ProcessPdFilesByIdsCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null || command.UploadIds is null || command.UploadIds.Count == 0)
        {
            return Result.Failure<ProcessPdFilesByIdsResponse>(Error.Validation(
                "Process.NoFiles",
                "No file IDs provided for processing"));
        }

        if (string.IsNullOrWhiteSpace(command.TimePeriod))
        {
            return Result.Failure<ProcessPdFilesByIdsResponse>(Error.Validation(
                "Process.InvalidTimePeriod",
                "Time period is required"));
        }

        logger.LogInformation("Starting PD file processing for {FileCount} files, TimePeriod: {TimePeriod}",
            command.UploadIds.Count, command.TimePeriod);

        try
        {
            // Generate session ID for grouping validation results
            var sessionId = Guid.CreateVersion7();
            logger.LogInformation("Created session ID: {SessionId} for file validation tracking", sessionId);

            // Fetch PD configuration
            CollectiveImpairmentConfig? pdConfig = await dbContext.CollectiveImpairmentConfigs
                .FirstOrDefaultAsync(c => c.Parameter == ParameterType.PD, cancellationToken);

            if (pdConfig is null)
            {
                return Result.Failure<ProcessPdFilesByIdsResponse>(Error.NotFound(
                    "Config.NotFound",
                    "PD configuration not found"));
            }

            // Parse existing configuration
            PDConfigurationJson? configJson = JsonSerializer.Deserialize<PDConfigurationJson>(
                pdConfig.ConfigJson, JsonOptions);

            if (configJson is null || configJson.PdFileUpload is null)
            {
                return Result.Failure<ProcessPdFilesByIdsResponse>(Error.Validation(
                    "Config.InvalidJson",
                    "Failed to parse PD configuration JSON"));
            }

            // Get time period data
            if (!configJson.PdFileUpload.TryGetValue(command.TimePeriod, out PdTimePeriodData? timePeriodData))
            {
                return Result.Failure<ProcessPdFilesByIdsResponse>(Error.NotFound(
                    "TimePeriod.NotFound",
                    $"Time period '{command.TimePeriod}' not found in configuration"));
            }

            // Build storage paths
            string rootPath = GetRootPath();
            
            // Hierarchical pending path (where files are uploaded)
            string pdPendingBase = Path.Combine(rootPath, "PD", "pending");
            string pdPendingPath = FileProcessingUtilities.CreateTimePeriodFolderPath(
                pdPendingBase, 
                command.TimePeriod, 
                pdConfig.ConfigJson);

            // FLAT processed path (for easier next-step processing)
            string pdProcessedPath = Path.Combine(rootPath, "PD", "processed");
            Directory.CreateDirectory(pdProcessedPath);

            // Collect file metadata for processing
            var fileMetadataToProcess = new List<(PdFileMetadata metadata, string filePath)>();

            foreach (string uploadId in command.UploadIds)
            {
                PdFileMetadata? fileMetadata = timePeriodData.Files
                    .FirstOrDefault(f => f.UploadId == uploadId);

                if (fileMetadata is null)
                {
                    logger.LogWarning("File with UploadId {UploadId} not found in configuration", uploadId);
                    continue;
                }

                // Get file path from hierarchical pending structure
                string filePath = Path.Combine(pdPendingPath, fileMetadata.StoredFileName);
                if (!File.Exists(filePath))
                {
                    logger.LogWarning("Physical file not found: {FilePath}", filePath);
                    continue;
                }

                fileMetadataToProcess.Add((fileMetadata, filePath));
            }

            if (fileMetadataToProcess.Count == 0)
            {
                return Result.Failure<ProcessPdFilesByIdsResponse>(Error.NotFound(
                    "Files.NotFound",
                    "No valid files found for processing"));
            }

            logger.LogInformation("Processing {FileCount} PD files individually with error isolation", 
                fileMetadataToProcess.Count);

            // Process each file individually with error handling
            var processedFiles = new List<ProcessedFileInfo>();
            var validationResults = new List<Domain.Files.FileValidationResult>();
            var processingErrors = new List<string>();
            int totalWithErrors = 0;
            int totalSuccessful = 0;

            foreach ((PdFileMetadata fileMetadata, string filePath) in fileMetadataToProcess)
            {
                try
                {
                   logger.LogInformation("Processing file: {FileName} (UploadId: {UploadId})", 
                        fileMetadata.Name, fileMetadata.UploadId);

                    // Process single file
                    Saral.FileProcessor.Core.Models.AnalysisResult analysisResult = 
                        await fileProcessingService.ProcessFileAsync(filePath, null, cancellationToken);

                    int totalRows = analysisResult.TotalRows;
                    int totalErrors = analysisResult.ValidationSummary?.RowValidations.Count(rv => !rv.IsValid) ?? 0;
                    double validationSuccessRate = totalRows > 0 ? (double)(totalRows - totalErrors) / totalRows * 100 : 100.0;
                    bool hasErrors = totalErrors > 0;

                    // Build detailed error list
                    List<UploadPdFiles.ValidationError>? detailedErrors = null;
                    if (hasErrors && analysisResult.ValidationSummary?.RowValidations != null)
                    {
                        detailedErrors = analysisResult.ValidationSummary.RowValidations
                            .Where(rv => !rv.IsValid)
                            .SelectMany(rv => rv.ColumnValidations
                                .Where(cv => !cv.Value.IsValid)
                                .Select(cv => new UploadPdFiles.ValidationError
                                {
                                    Row = rv.RowIndex,
                                    Column = cv.Key,
                                    Error = cv.Value.ErrorMessage ?? "Validation failed"
                                }))
                            .ToList();
                    }

                    // Update file metadata with validation results
                    fileMetadata.Status = hasErrors ? "invalid" : "valid";
                    fileMetadata.Progress = 100;
                    fileMetadata.IsValidated = true;
                    fileMetadata.ValidationResult = new PdValidationResult
                    {
                        TotalRows = totalRows,
                        TotalErrors = totalErrors,
                        Status = hasErrors ? "invalid" : "valid",
                        Errors = detailedErrors
                    };

                    // Create file validation result for database storage
                    var fileValidationResult = new Domain.Files.FileValidationResult
                    {
                        Filename = fileMetadata.StoredFileName,
                        TotalRows = totalRows,
                        TotalErrors = totalErrors,
                        Status = hasErrors ? "HasValidationErrors" : "Valid",
                        SessionId = sessionId,
                        TimePeriod = command.TimePeriod,
                        CollectiveImpairmentType = "PD",
                        CreatedOnUtc = DateTime.UtcNow
                    };

                    validationResults.Add(fileValidationResult);

                    // Handle file based on validation result
                    if (!hasErrors && validationSuccessRate >= 95.0)
                    {
                        try
                        {
                            // Move file to processed directory
                            string processedFilePath = Path.Combine(pdProcessedPath, fileMetadata.StoredFileName);
                            File.Move(filePath, processedFilePath, overwrite: true);
                            logger.LogInformation("Moved valid file to processed directory: {FileName}", fileMetadata.StoredFileName);
                            totalSuccessful++;
                        }
                        catch (Exception moveEx)
                        {
                            logger.LogError(moveEx, "Failed to move file {FileName} to processed directory", fileMetadata.StoredFileName);
                            processingErrors.Add($"File '{fileMetadata.Name}': Failed to move to processed directory - {moveEx.Message}");
                        }
                    }
                    else
                    {
                        try
                        {
                            // Update original file with validation errors
                            if (analysisResult.ValidationSummary?.ModifiedData != null)
                            {
                                CsvWriter.UpdateOriginalFile(analysisResult.ValidationSummary.ModifiedData, filePath);
                                logger.LogInformation("Updated file with ValidationErrors column: {FileName}", fileMetadata.StoredFileName);
                            }
                            logger.LogInformation("File has errors, keeping in pending directory: {FileName}", fileMetadata.StoredFileName);
                            totalWithErrors++;
                        }
                        catch (Exception updateEx)
                        {
                            logger.LogError(updateEx, "Failed to update file {FileName} with validation errors", fileMetadata.StoredFileName);
                            processingErrors.Add($"File '{fileMetadata.Name}': Failed to update with validation errors - {updateEx.Message}");
                        }
                    }

                    processedFiles.Add(new ProcessedFileInfo(
                        UploadId: fileMetadata.UploadId,
                        FileName: fileMetadata.Name,
                        Status: fileMetadata.Status,
                        TotalRows: totalRows,
                        TotalErrors: totalErrors,
                        HasErrors: hasErrors
                    ));

                    logger.LogInformation("Processed file {FileName}: {TotalRows} rows, {TotalErrors} errors, Success Rate: {SuccessRate}%",
                        fileMetadata.Name, totalRows, totalErrors, validationSuccessRate);
                }
                catch (Exception fileEx)
                {
                    logger.LogError(fileEx, "Error processing file {FileName} (UploadId: {UploadId})", 
                        fileMetadata.Name, fileMetadata.UploadId);
                    
                    processingErrors.Add($"File '{fileMetadata.Name}': {fileEx.Message}");
                    
                    // Add failed file to results with error status
                    processedFiles.Add(new ProcessedFileInfo(
                        UploadId: fileMetadata.UploadId,
                        FileName: fileMetadata.Name,
                        Status: "error",
                        TotalRows: 0,
                        TotalErrors: 0,
                        HasErrors: true
                    ));
                }
            }

            // Save validation results to database (only successful validations)
            if (validationResults.Count > 0)
            {
                try
                {
                    dbContext.FileValidationResults.AddRange(validationResults);
                    logger.LogInformation("Saving {Count} validation results to file_validation_results table with session ID: {SessionId}", 
                        validationResults.Count, sessionId);
                }
                catch (Exception dbEx)
                {
                    logger.LogError(dbEx, "Failed to save validation results to database");
                    processingErrors.Add($"Database save failed: {dbEx.Message}");
                }
            }

            // Update configuration JSON
            try
            {
                // CRITICAL FIX: Ensure ALL file references are updated in the configJson object
                // Before serialization, verify the ValidationResult is set on the actual objects in the list
                foreach (string uploadId in command.UploadIds)
                {
                    PdFileMetadata? fileInList = timePeriodData.Files.FirstOrDefault(f => f.UploadId == uploadId);
                    if (fileInList != null)
                    {
                        // Find the processed file info
                        ProcessedFileInfo? processedInfo = processedFiles.FirstOrDefault(p => p.UploadId == uploadId);
                        if (processedInfo != null && processedInfo.Status != "error")
                        {
                            // Explicitly set the ValidationResult on the file in the list
                            // This ensures the reference in configJson is updated
                            if (fileInList.ValidationResult == null)
                            {
                                logger.LogWarning("FIXING: File {FileName} ValidationResult was NULL, creating new one", fileInList.Name);
                                fileInList.ValidationResult = new PdValidationResult
                                {
                                    TotalRows = processedInfo.TotalRows,
                                    TotalErrors = processedInfo.TotalErrors,
                                    Status = processedInfo.Status,
                                    Errors = null  // Errors list handled separately
                                };
                            }
                            
                            logger.LogInformation("? File {FileName} ValidationResult: TotalRows={TotalRows}, TotalErrors={TotalErrors}, Status={Status}",
                                fileInList.Name, fileInList.ValidationResult.TotalRows, fileInList.ValidationResult.TotalErrors, fileInList.ValidationResult.Status);
                        }
                    }
                }

                string updatedConfigJson = JsonSerializer.Serialize(configJson, JsonOptions);
                
                logger.LogDebug("Serialized config JSON length: {Length} characters", updatedConfigJson.Length);
                
                if (updatedConfigJson.Contains("validationResult"))
                {
                    logger.LogInformation("? ValidationResult field IS present in serialized JSON");
                }
                else
                {
                    logger.LogWarning("? ValidationResult field NOT present in serialized JSON");
                    
                    // Debug: Log a sample of the JSON to see what's actually being serialized
                    PdFileMetadata? sampleFile = timePeriodData.Files.FirstOrDefault(f => command.UploadIds.Contains(f.UploadId));
                    if (sampleFile != null)
                    {
                        string sampleJson = JsonSerializer.Serialize(sampleFile, JsonOptions);
                        logger.LogDebug("Sample file JSON: {SampleJson}", sampleJson);
                    }
                }
                
                pdConfig.ConfigJson = updatedConfigJson;
                pdConfig.UpdatedDate = DateTime.UtcNow;

                dbContext.CollectiveImpairmentConfigs.Update(pdConfig);
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Successfully updated PD configuration and saved to database");
            }
            catch (Exception configEx)
            {
                logger.LogError(configEx, "Failed to update configuration JSON");
                processingErrors.Add($"Configuration update failed: {configEx.Message}");
            }

            // Log summary
            if (processingErrors.Count > 0)
            {
                logger.LogWarning("Processed {FileCount} PD files with {ErrorCount} errors: {Successful} successful, {Errors} with validation errors",
                    processedFiles.Count, processingErrors.Count, totalSuccessful, totalWithErrors);
                
                foreach (string error in processingErrors)
                {
                    logger.LogWarning("Processing error: {Error}", error);
                }
            }
            else
            {
                logger.LogInformation("Successfully processed {FileCount} PD files: {Successful} successful, {Errors} with validation errors",
                    processedFiles.Count, totalSuccessful, totalWithErrors);
            }

            return Result.Success(new ProcessPdFilesByIdsResponse(
                ProcessedFiles: processedFiles,
                TotalProcessed: processedFiles.Count,
                TotalWithErrors: totalWithErrors,
                TotalSuccessful: totalSuccessful
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error processing PD files for TimePeriod: {TimePeriod}", command.TimePeriod);
            return Result.Failure<ProcessPdFilesByIdsResponse>(Error.Failure(
                "Process.Failed",
                $"Failed to process files: {ex.Message}"));
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
