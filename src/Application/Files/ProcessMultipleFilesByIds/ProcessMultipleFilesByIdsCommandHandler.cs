using System.IO;
using System.Linq;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.Common;
using Application.Files.ProcessMultipleFiles;
using Application.Files.Services;
using Application.Files.UploadFile;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saral.FileProcessor.Core.Services;
using Saral.FileProcessor.Data.Extensions;
using Saral.FileProcessor.Data.Services;
using Saral.FileProcessor.IO;
using Saral.FileProcessor.Reports.Services;
using SharedKernel;

namespace Application.Files.ProcessMultipleFilesByIds;

internal sealed class ProcessMultipleFilesByIdsCommandHandler(
    IApplicationDbContext dbContext,
    IFileProcessingService fileProcessingService,
    IFileProcessingResultService fileProcessingResultService,
    IOptions<ProcessedFilePathsOptions> processedFilePathsOptions,
    IFileMovementService fileMovementService,
    ILogger<ProcessMultipleFilesByIdsCommandHandler> logger) : ICommandHandler<ProcessMultipleFilesByIdsCommand, ProcessMultipleFilesResponse>
{
    public async Task<Result<ProcessMultipleFilesResponse>> Handle(
        ProcessMultipleFilesByIdsCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("ProcessMultipleFilesByIds starting processing for {FileCount} file IDs", command.FileIds.Length);
        
        try
        {
            logger.LogInformation("Starting file processing for {FileCount} files by IDs: {FileIds}", command.FileIds.Length, command.FileIds);
            
            // Validate CollectiveImpairmentType and fetch configuration
            logger.LogInformation("ProcessMultipleFilesByIds validating CollectiveImpairmentType: {CollectiveImpairmentType}", command.CollectiveImpairmentType);
            
            if (string.IsNullOrWhiteSpace(command.CollectiveImpairmentType))
            {
                return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                    "CollectiveImpairmentType.Required",
                    "Collective impairment type is required."));
            }

            if (!Enum.TryParse<ParameterType>(command.CollectiveImpairmentType, true, out ParameterType parameterType))
            {
                logger.LogWarning("ProcessMultipleFilesByIds invalid CollectiveImpairmentType: {CollectiveImpairmentType}", command.CollectiveImpairmentType);
                return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                    "CollectiveImpairmentType.Invalid",
                    $"Invalid collective impairment type. Valid values are: {string.Join(", ", Enum.GetNames<ParameterType>())}."));
            }

            logger.LogInformation("ProcessMultipleFilesByIds fetching config for ParameterType: {ParameterType}", parameterType);
            Domain.CollectiveImpairment.CollectiveImpairmentConfig? config = await dbContext.CollectiveImpairmentConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Parameter == parameterType, cancellationToken);

            if (config is null)
            {
                logger.LogWarning("ProcessMultipleFilesByIds no config found for ParameterType: {ParameterType}", parameterType);
                return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                    "CollectiveImpairmentConfig.NotFound",
                    $"No configuration found for collective impairment type '{parameterType}'."));
            }

            // Validate time period against configuration
            logger.LogInformation("ProcessMultipleFilesByIds config found, validating time period: {TimePeriod}", command.TimePeriod);
            Result<string> timePeriodValidation = FileProcessingUtilities.ValidateTimePeriod(command.TimePeriod, config.ConfigJson);
            if (!timePeriodValidation.IsSuccess)
            {
                return Result.Failure<ProcessMultipleFilesResponse>(timePeriodValidation.Error);
            }

            logger.LogInformation("ProcessMultipleFilesByIds querying database for file information");
            
            // 1. Get file information from database
            List<Guid> fileIdsList = command.FileIds.ToList();
            var uploadedFiles = await dbContext.UploadedFiles
                .Where(f => fileIdsList.Contains(f.Id))
                .Select(f => new { f.Id, f.PhysicalPath, f.OriginalFileName })
                .ToListAsync(cancellationToken);

            logger.LogInformation("ProcessMultipleFilesByIds found {FoundCount} files in database out of {RequestedCount} requested", uploadedFiles.Count, command.FileIds.Length);

            if (uploadedFiles.Count != command.FileIds.Length)
            {
                logger.LogWarning("Some files not found in database. Requested: {RequestedCount}, Found: {FoundCount}", 
                    command.FileIds.Length, uploadedFiles.Count);
                return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                    "Files.NotFound",
                    "One or more files not found in the database"));
            }

            // 2. Extract file paths
            string[] files = uploadedFiles.Select(f => f.PhysicalPath).ToArray();
            logger.LogInformation("ProcessMultipleFilesByIds extracted file paths: {FilePaths}", string.Join(", ", files));

            // 3. Verify files exist on disk
            logger.LogInformation("ProcessMultipleFilesByIds verifying files exist on disk");
            string[] missingFiles = files.Where(path => !File.Exists(path)).ToArray();
            if (missingFiles.Length > 0)
            {
                logger.LogWarning("Some files not found on disk: {MissingFiles}", string.Join(", ", missingFiles));
                return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                    "Files.PhysicalNotFound",
                    $"Files not found on disk: {string.Join(", ", missingFiles)}"));
            }

            var sessionId = Guid.CreateVersion7();
            logger.LogInformation("Processing files with session ID: {SessionId}", sessionId);
            logger.LogInformation("ProcessMultipleFilesByIds starting FileProcessor with session ID: {SessionId}", sessionId);

            // Call Saral.FileProcessor service with file paths and database integration
            logger.LogInformation("ProcessMultipleFilesByIds calling FileProcessor service for {FileCount} files", files.Length);
            (Saral.FileProcessor.Core.Models.MultiFileAnalysisResult analysisResult, List<int> _) = await fileProcessingService.ProcessMultipleFilesWithDatabaseAsync(
                fileProcessingResultService,
                files,
                sessionId.ToString(),
                command.TimePeriod,                      // Pass time period
                parameterType.ToString(),                // Pass collective impairment type
                null,  // options
                cancellationToken);
            logger.LogInformation("ProcessMultipleFilesByIds FileProcessor completed, individual results count: {ResultCount}", analysisResult.IndividualResults.Count);

            // Process individual results
            Application.Files.ProcessMultipleFiles.IndividualFileResult[] individualResults = analysisResult.IndividualResults.Select(r =>
            {
                Saral.FileProcessor.Core.Models.AnalysisResult analysis = r.Analysis;
                
                int totalRows = analysis.TotalRows;
                int totalColumns = analysis.TotalColumns;
                int validRows = analysis.ValidationSummary?.ValidRows ?? totalRows;
                int totalErrors = analysis.ValidationSummary?.InvalidRows ?? 0;
                
                double validationSuccessRate = totalRows == 0 ? 100.0 : (double)validRows / totalRows * 100.0;
                
                // Use validation success rate as data quality score
                double dataQualityScore = validationSuccessRate;
                
                // Extract validation error details from ValidationSummary
                List<ValidationErrorDetail> validationErrors = [];
                if (analysis.ValidationSummary?.RowValidations != null)
                {
                    validationErrors = analysis.ValidationSummary.RowValidations
                        .Where(rv => !rv.IsValid)
                        .Select(rv => 
                        {
                            var columnErrors = rv.ColumnValidations
                                .Where(cv => !cv.Value.IsValid)
                                .ToDictionary(cv => cv.Key, cv => cv.Value.ErrorMessage ?? "Unknown error");
                            
                            return new ValidationErrorDetail(
                                RowIndex: rv.RowIndex,
                                ErrorMessage: rv.ValidationMessage,
                                ColumnErrors: new Dictionary<string, string>(columnErrors)
                            );
                        })
                        .ToList();
                }
                
                return new Application.Files.ProcessMultipleFiles.IndividualFileResult(
                    FileName: r.FileName,
                    FileIndex: r.FileIndex,
                    TotalRows: totalRows,
                    TotalColumns: totalColumns,
                    ValidRows: validRows,
                    InvalidRows: totalErrors,
                    DataQualityScore: dataQualityScore,
                    ValidationSuccessRate: validationSuccessRate,
                    ValidationErrors: validationErrors
                );
            }).ToArray();

            int totalRowsSum = analysisResult.IndividualResults.Sum(r => r.Analysis.TotalRows);
            int crossFileValidationErrors = analysisResult.CrossFileValidation.CrossFileValidationErrors.Count;
            double averageDataQualityScore = individualResults.Length > 0 
                ? individualResults.Average(r => r.DataQualityScore) 
                : 100.0;

            // Determine if processing was successful (no critical errors)
            bool processingSuccessful = crossFileValidationErrors == 0 && 
                          individualResults.All(r => r.ValidationSuccessRate >= 100.0); // 100% threshold for success

            // Handle files based on processing success
            string? fileMovementErrors = null;
            if (processingSuccessful)
            {
                logger.LogInformation("ProcessMultipleFilesByIds files are clean, attempting file movement");
                // Files are clean - move to processed directory without adding error columns
                if (processedFilePathsOptions.Value != null)
                {
                    string? destinationPath = parameterType switch
                    {
                        ParameterType.PD => processedFilePathsOptions.Value.PD,
                        ParameterType.LGD => processedFilePathsOptions.Value.LGD,
                        _ => null
                    };

                    if (!string.IsNullOrWhiteSpace(destinationPath))
                    {
                        try
                        {
                            logger.LogInformation("ProcessMultipleFilesByIds moving files using FileMovementService");
                            Result<FileMovementResult> movementResult = await fileMovementService.MoveFilesAsync(
                                files, parameterType, command.TimePeriod, config.ConfigJson, cancellationToken);
                            
                            if (!movementResult.IsSuccess)
                            {
                                fileMovementErrors = movementResult.Error.Description;
                                logger.LogWarning("ProcessMultipleFilesByIds file movement failed: {Error}", fileMovementErrors);
                            }
                            else if (movementResult.Value.FailedMoves > 0)
                            {
                                fileMovementErrors = $"Failed to move {movementResult.Value.FailedMoves} out of {movementResult.Value.TotalFiles} files. Errors: {string.Join("; ", movementResult.Value.Errors)}";
                                logger.LogWarning("ProcessMultipleFilesByIds partial file movement failure: {Error}", fileMovementErrors);
                            }
                            else
                            {
                                logger.LogInformation("ProcessMultipleFilesByIds successfully moved {TotalFiles} files", movementResult.Value.TotalFiles);
                                
                                // Update UploadedFile entities with new PhysicalPath after successful move
                                foreach (KeyValuePair<string, string> movedFile in movementResult.Value.MovedFiles)
                                {
                                    string oldPath = movedFile.Key;
                                    string newPath = movedFile.Value;
                                    string fileName = Path.GetFileName(oldPath);
                                    
                                    Domain.Files.UploadedFile? uploadedFile = await dbContext.UploadedFiles
                                        .FirstOrDefaultAsync(uf => uf.StoredFileName == fileName, cancellationToken);

                                    FileValidationResult? fileValidationResult = await dbContext.FileValidationResults
                                        .FirstOrDefaultAsync(fvr => fvr.Filename == fileName && fvr.SessionId == sessionId, cancellationToken);

                                    if (uploadedFile is not null && fileValidationResult is not null)
                                    {
                                        // Update the physical path
                                        uploadedFile.PhysicalPath = newPath;
                                        uploadedFile.StoredFileName = Path.GetFileName(newPath);
                                        logger.LogInformation("Updated PhysicalPath for file {FileName} from {OldPath} to {NewPath}", 
                                            fileName, oldPath, newPath);

                                        fileValidationResult.Filename = Path.GetFileName(newPath);
                                        logger.LogInformation("Updated PhysicalPath for fileValidationResult {FileName} from {OldPath} to {NewPath}",
                                            fileName, oldPath, newPath);
                                    }
                                }
                                
                                // Save changes to database
                                await dbContext.SaveChangesAsync(cancellationToken);
                                logger.LogInformation("Successfully updated {Count} UploadedFile records with new paths", 
                                    movementResult.Value.MovedFiles.Count);
                            }
                        }
                        catch (Exception ex)
                        {
                            fileMovementErrors = $"File movement failed: {ex.Message}";
                            logger.LogError(ex, "ProcessMultipleFilesByIds file movement exception: {Message}", ex.Message);
                        }
                    }
                    else
                    {
                        logger.LogInformation("ProcessMultipleFilesByIds no destination path configured for ParameterType: {ParameterType}", parameterType);
                    }
                }
                else
                {
                    logger.LogInformation("ProcessMultipleFilesByIds processedFilePathsOptions not configured, skipping file movement");
                }
            }
            else
            {
                logger.LogInformation("ProcessMultipleFilesByIds files have errors, updating original files with validation columns");
                // Files have errors - update original files with validation error columns
                foreach (Saral.FileProcessor.Core.Models.IndividualFileResult result in analysisResult.IndividualResults)
                {
                    if (result.Analysis.ValidationSummary?.ModifiedData != null)
                    {
                        string originalFilePath = result.Analysis.FileContext.FilePath;
                        logger.LogInformation("ProcessMultipleFilesByIds updating file with validation errors: {FilePath}", originalFilePath);
                        
                        // Update original file with validation errors included
                        CsvWriter.UpdateOriginalFile(result.Analysis.ValidationSummary.ModifiedData, originalFilePath);
                    }
                }
            }

            var response = new ProcessMultipleFilesResponse(
                TotalFiles: analysisResult.IndividualResults.Count,
                TotalRows: totalRowsSum,
                CrossFileDuplicates: analysisResult.CrossFileValidation.TotalDuplicateRows,
                CrossFileValidationErrors: crossFileValidationErrors,
                ProcessingTimeMs: analysisResult.SummaryStatistics.ProcessingTime.TotalMilliseconds,
                DataQualityScore: averageDataQualityScore,
                IndividualResults: individualResults,
                JsonReportPath: null, // TODO: Enable when report generation is compatible
                ExcelReportPath: null, // TODO: Enable when report generation is compatible
                FileMovementErrors: fileMovementErrors
            );

            return Result.Success(response);
        }
        catch (ArgumentException ex) when (ex.ParamName is "filePaths" or "fileStreams")
        {
            return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                "Files.Empty",
                "No files provided for processing."));
        }
        catch (FileNotFoundException ex)
        {
            return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                "File.NotFound",
                $"File not found: {ex.FileName}"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                "File.AccessDenied",
                $"Access denied to file: {ex.Message}"));
        }
        catch (DirectoryNotFoundException ex)
        {
            return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                "Directory.NotFound",
                $"Directory not found: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ProcessMultipleFilesByIds exception occurred: {Message}", ex.Message);
            return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                "Processing.Failed",
                $"An unexpected error occurred during file processing: {ex.Message}"));
        }
    }
}
