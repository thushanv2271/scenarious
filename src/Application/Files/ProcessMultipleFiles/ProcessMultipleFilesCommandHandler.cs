using System.IO;
using System.Linq;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.Common;
using Application.Files.Services;
using Application.Files.UploadFile;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Saral.FileProcessor.Core.Services;
using Saral.FileProcessor.Data.Extensions;
using Saral.FileProcessor.Data.Services;
using Saral.FileProcessor.IO;
using Saral.FileProcessor.Reports.Services;
using SharedKernel;

namespace Application.Files.ProcessMultipleFiles;

internal sealed class ProcessMultipleFilesCommandHandler(
    IApplicationDbContext dbContext,
    IFileProcessingService fileProcessingService,
    IFileProcessingResultService fileProcessingResultService,
    IOptions<FileStorageOptions> storageOptions,
    IOptions<ProcessedFilePathsOptions> processedFilePathsOptions,
    IFileMovementService fileMovementService) : ICommandHandler<ProcessMultipleFilesCommand, ProcessMultipleFilesResponse>
{
    public async Task<Result<ProcessMultipleFilesResponse>> Handle(

        ProcessMultipleFilesCommand command,
        CancellationToken cancellationToken)
    {
        try
        {

            // Validate CollectiveImpairmentType and fetch configuration
            if (string.IsNullOrWhiteSpace(command.CollectiveImpairmentType))
            {
                return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                    "CollectiveImpairmentType.Required",
                    "Collective impairment type is required."));
            }

            if (!Enum.TryParse<ParameterType>(command.CollectiveImpairmentType, true, out ParameterType parameterType))
            {
                return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                    "CollectiveImpairmentType.Invalid",
                    $"Invalid collective impairment type. Valid values are: {string.Join(", ", Enum.GetNames<ParameterType>())}."));
            }

            CollectiveImpairmentConfig? config = await dbContext.CollectiveImpairmentConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Parameter == parameterType, cancellationToken);

            if (config is null)
            {
                return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                    "CollectiveImpairmentConfig.NotFound",
                    $"No configuration found for collective impairment type '{parameterType}'."));
            }

            // Validate time period against configuration
            Result<string> timePeriodValidation = FileProcessingUtilities.ValidateTimePeriod(command.TimePeriod, config.ConfigJson);
            if (!timePeriodValidation.IsSuccess)
            {
                return Result.Failure<ProcessMultipleFilesResponse>(timePeriodValidation.Error);
            }

            string configuredRoot = storageOptions.Value.RootPath ?? string.Empty;

            string expandedRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.GetTempPath()
            : Environment.ExpandEnvironmentVariables(configuredRoot);

            string rootPath = Path.IsPathRooted(expandedRoot)
                ? expandedRoot
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedRoot));

            string parameterFolder = Path.Combine(rootPath, parameterType.ToString());

            // Create hierarchical folder structure based on frequency
            string timePeriodFolder = FileProcessingUtilities.CreateTimePeriodFolderPath(parameterFolder, command.TimePeriod, config.ConfigJson);

            if (!Directory.Exists(timePeriodFolder))
            {
                return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                    "Directory.NotFound",
                    $"No directory found '{timePeriodFolder}'."));
            }

            string[] files = Directory.GetFiles(timePeriodFolder);

            if (files.Length == 0)
            {
                return Result.Failure<ProcessMultipleFilesResponse>(Error.Problem(
                    "Files.NotFound",
                    $"No files found in directory '{timePeriodFolder}'."));
            }

            var sessionId = Guid.CreateVersion7();

            // Call Saral.FileProcessor service with file paths and database integration
            (Saral.FileProcessor.Core.Models.MultiFileAnalysisResult analysisResult, List<int> _) = await fileProcessingService.ProcessMultipleFilesWithDatabaseAsync(
                fileProcessingResultService,
                files,
                sessionId.ToString(),
                null, 
                cancellationToken);

            // Fetch results from database for accurate data
            List<Domain.Files.FileValidationResult> fileValidationResults = await dbContext.FileValidationResults
                .Where(fvr => fvr.SessionId == sessionId)
                .ToListAsync(cancellationToken);

            // Create response with actual data from analysis and database
            IndividualFileResult[] individualResults = [.. analysisResult.IndividualResults.Select((r, index) => {
                Domain.Files.FileValidationResult? dbResult = fileValidationResults.FirstOrDefault(db => db.Filename == r.FileName);
                Saral.FileProcessor.Core.Models.AnalysisResult analysis = r.Analysis;
                
                int totalRows = analysis.TotalRows;
                int totalColumns = analysis.FileContext?.Data?.ColumnKeys.Count() ?? 0;
                int totalErrors = dbResult?.TotalErrors ?? 0;
                int validRows = totalRows - totalErrors;
                double validationSuccessRate = totalRows > 0 ? (double)validRows / totalRows * 100 : 100.0;
                
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
                
                return new IndividualFileResult(
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
            })];

            int totalRowsSum = analysisResult.IndividualResults.Sum(r => r.Analysis.TotalRows);
            int crossFileValidationErrors = analysisResult.CrossFileValidation.CrossFileValidationErrors.Count;
            double averageDataQualityScore = individualResults.Length > 0 
                ? individualResults.Average(r => r.DataQualityScore) 
                : 100.0;

            // Determine if processing was successful (no critical errors)
            bool processingSuccessful = crossFileValidationErrors == 0 && 
                                      individualResults.All(r => r.ValidationSuccessRate >= 95.0); // 95% threshold for success

            // Handle files based on processing success
            string? fileMovementErrors = null;
            if (processingSuccessful)
            {
                // Files are clean - move to processed directory without adding error columns
                // Move files to appropriate destination if file movement is configured
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
                            Result<FileMovementResult> movementResult = await fileMovementService.MoveFilesAsync(
                                files, parameterType, command.TimePeriod, config.ConfigJson, cancellationToken);
                            
                            if (!movementResult.IsSuccess)
                            {
                                fileMovementErrors = movementResult.Error.Description;
                            }
                            else if (movementResult.Value.FailedMoves > 0)
                            {
                                fileMovementErrors = $"Failed to move {movementResult.Value.FailedMoves} out of {movementResult.Value.TotalFiles} files. Errors: {string.Join("; ", movementResult.Value.Errors)}";
                            }
                        }
                        catch (Exception ex)
                        {
                            fileMovementErrors = $"File movement failed: {ex.Message}";
                        }
                    }
                }
            }
            else
            {
                // Files have errors - update original files with validation error columns
                foreach (Saral.FileProcessor.Core.Models.IndividualFileResult result in analysisResult.IndividualResults)
                {
                    if (result.Analysis.ValidationSummary?.ModifiedData != null)
                    {
                        string originalFilePath = result.Analysis.FileContext.FilePath;
                        
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
        catch (Exception ex)
        {
            return Result.Failure<ProcessMultipleFilesResponse>(new Error(
                "ProcessMultipleFiles.Failed",
                $"Failed to process multiple files: {ex.Message}",
                ErrorType.Failure
            ));
        }
    }
}
