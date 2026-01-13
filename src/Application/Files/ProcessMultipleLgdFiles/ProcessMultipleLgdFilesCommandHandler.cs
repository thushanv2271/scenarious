using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.Common;
using Application.Files.UploadLgdFiles;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saral.FileProcessor.Core.Services;
using Saral.FileProcessor.Data.Extensions;
using Saral.FileProcessor.Data.Services;
using Saral.FileProcessor.IO;
using SharedKernel;

namespace Application.Files.ProcessMultipleLgdFiles;

internal sealed class ProcessMultipleLgdFilesCommandHandler(
    IApplicationDbContext dbContext,
    IFileProcessingService fileProcessingService,
    IOptions<LgdFileStorageOptions> storageOptions,
    ILogger<ProcessMultipleLgdFilesCommandHandler> logger)
    : ICommandHandler<ProcessMultipleLgdFilesCommand, ProcessMultipleLgdFilesResponse>
{
    public async Task<Result<ProcessMultipleLgdFilesResponse>> Handle(
        ProcessMultipleLgdFilesCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("ProcessMultipleLgdFiles starting for Year: {Year}, FacilityStatus: {FacilityStatus}",
                command.Year, command.FacilityStatus);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(command.Year))
            {
                return Result.Failure<ProcessMultipleLgdFilesResponse>(Error.Problem(
                    "Year.Required",
                    "Year is required."));
            }

            if (string.IsNullOrWhiteSpace(command.FacilityStatus) ||
                command.FacilityStatus != "OpenFacility" && command.FacilityStatus != "ClosedFacility")
            {
                return Result.Failure<ProcessMultipleLgdFilesResponse>(Error.Problem(
                    "FacilityStatus.Invalid",
                    "Facility status must be either 'OpenFacility' or 'ClosedFacility'."));
            }

            // Fetch LGD configuration
            CollectiveImpairmentConfig? config = await dbContext.CollectiveImpairmentConfigs
                .FirstOrDefaultAsync(c => c.Parameter == ParameterType.LGD, cancellationToken);

            if (config is null)
            {
                return Result.Failure<ProcessMultipleLgdFilesResponse>(Error.Problem(
                    "LgdConfig.NotFound",
                    "No LGD configuration found."));
            }

            // Build directory path with new structure
            string configuredRoot = storageOptions.Value.RootPath ?? string.Empty;
            string expandedRoot = string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.GetTempPath()
                : Environment.ExpandEnvironmentVariables(configuredRoot);

            string rootPath = Path.IsPathRooted(expandedRoot)
                ? expandedRoot
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedRoot));

            // New path structure: /LGD/pending/{FacilityStatus}
            string pendingFolder = Path.Combine(rootPath, "LGD", "pending", command.FacilityStatus);

            if (!Directory.Exists(pendingFolder))
            {
                logger.LogWarning("ProcessMultipleLgdFiles directory does not exist: {Folder}", pendingFolder);
                return Result.Failure<ProcessMultipleLgdFilesResponse>(Error.Problem(
                    "Directory.NotFound",
                    $"Directory not found: {pendingFolder}"));
            }

            string[] files = Directory.GetFiles(pendingFolder);
            logger.LogInformation("ProcessMultipleLgdFiles found {FileCount} files in {Folder}", files.Length, pendingFolder);

            if (files.Length == 0)
            {
                logger.LogWarning("ProcessMultipleLgdFiles no files found in directory: {Folder}", pendingFolder);
                return Result.Failure<ProcessMultipleLgdFilesResponse>(Error.Problem(
                    "Files.NotFound",
                    $"No files found in directory: {pendingFolder}"));
            }

            var sessionId = Guid.CreateVersion7();
            logger.LogInformation("ProcessMultipleLgdFiles starting file processing with session ID: {SessionId}", sessionId);

            // Process files directly without database integration (validation results go to JSON instead)
            logger.LogInformation("ProcessMultipleLgdFiles calling FileProcessor service for {FileCount} files", files.Length);
            Saral.FileProcessor.Core.Models.MultiFileAnalysisResult analysisResult =
                await fileProcessingService.ProcessMultipleFilesAsync(
                    files,
                    null,
                    cancellationToken);
            logger.LogInformation("ProcessMultipleLgdFiles FileProcessor completed, individual results count: {ResultCount}",
                analysisResult.IndividualResults.Count);

            // Build validation results directly from analysis result (no database query needed)
            var fileValidationResults = analysisResult.IndividualResults.Select(r =>
            {
                Saral.FileProcessor.Core.Models.AnalysisResult analysis = r.Analysis;
                int totalErrors = analysis.ValidationSummary?.RowValidations.Count(rv => !rv.IsValid) ?? 0;
                string status = totalErrors > 0 ? "HasValidationErrors" : "Valid";

                return new Domain.Files.FileValidationResult
                {
                    Filename = r.FileName,
                    TotalRows = analysis.TotalRows,
                    TotalErrors = totalErrors,
                    Status = status,
                    SessionId = sessionId,
                    TimePeriod = command.Year,               // ADD THIS (LGD uses Year as TimePeriod)
                    CollectiveImpairmentType = "LGD",        // ADD THIS
                    CreatedOnUtc = DateTime.UtcNow
                };
            }).ToList();

            logger.LogInformation("ProcessMultipleLgdFiles built {ValidationResultCount} validation results from analysis",
                fileValidationResults.Count);

            // Save validation results to database
            dbContext.FileValidationResults.AddRange(fileValidationResults);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("ProcessMultipleLgdFiles saved {ValidationResultCount} validation results to database", 
                fileValidationResults.Count);

            // Build individual results with proper validation logic
            LgdIndividualFileResult[] individualResults = analysisResult.IndividualResults.Select(r =>
            {
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
                List<LgdValidationErrorDetail> validationErrors = [];
                if (analysis.ValidationSummary?.RowValidations != null)
                {
                    validationErrors = analysis.ValidationSummary.RowValidations
                        .Where(rv => !rv.IsValid)
                        .Select(rv =>
                        {
                            var columnErrors = rv.ColumnValidations
                                .Where(cv => !cv.Value.IsValid)
                                .ToDictionary(cv => cv.Key, cv => cv.Value.ErrorMessage ?? "Unknown error");

                            return new LgdValidationErrorDetail(
                                RowIndex: rv.RowIndex,
                                ErrorMessage: rv.ValidationMessage ?? "Validation failed",
                                ColumnErrors: new Dictionary<string, string>(columnErrors)
                            );
                        })
                        .ToList();
                }

                return new LgdIndividualFileResult(
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

            // Determine if processing was successful (same threshold as PD: 95%)
            bool processingSuccessful = crossFileValidationErrors == 0 &&
                                      individualResults.All(r => r.ValidationSuccessRate >= 95.0);

            string? fileMovementErrors = null;
            if (processingSuccessful)
            {
                logger.LogInformation("ProcessMultipleLgdFiles files are valid, moving to processed directory");

                // Move files to processed directory
                string processedFolder = Path.Combine(rootPath, "LGD", "processed", command.FacilityStatus);

                if (!Directory.Exists(processedFolder))
                {
                    Directory.CreateDirectory(processedFolder);
                    logger.LogInformation("Created processed directory: {Path}", processedFolder);
                }

                try
                {
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        string destinationPath = Path.Combine(processedFolder, fileName);
                        File.Move(file, destinationPath, overwrite: true);
                        logger.LogInformation("Moved file from {Source} to {Destination}", file, destinationPath);
                    }

                    // Update config JSON to mark files as completed and validated with validation results
                    await UpdateFileStatusInConfig(config, command.Year, command.FacilityStatus,
                        files.Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToList(),
                        "completed", true, fileValidationResults, cancellationToken);

                    logger.LogInformation("Successfully moved {FileCount} files to processed directory", files.Length);
                }
                catch (Exception ex)
                {
                    fileMovementErrors = $"File movement failed: {ex.Message}";
                    logger.LogError(ex, "ProcessMultipleLgdFiles file movement error");
                }
            }
            else
            {
                logger.LogInformation("ProcessMultipleLgdFiles files have errors, updating original files with error columns");

                // Update original files with validation errors (same as PD)
                foreach (Saral.FileProcessor.Core.Models.IndividualFileResult result in analysisResult.IndividualResults)
                {
                    if (result.Analysis.ValidationSummary?.ModifiedData != null)
                    {
                        string originalFilePath = result.Analysis.FileContext.FilePath;
                        CsvWriter.UpdateOriginalFile(result.Analysis.ValidationSummary.ModifiedData, originalFilePath);
                        logger.LogInformation("Updated file with validation errors: {FilePath}", originalFilePath);
                    }
                }

                // Update config JSON to mark files as validated even though they have errors, with validation results
                await UpdateFileStatusInConfig(config, command.Year, command.FacilityStatus,
                    files.Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToList(),
                    "pending", true, fileValidationResults, cancellationToken);

                logger.LogInformation("Updated file status to validated despite validation errors");
            }

            var response = new ProcessMultipleLgdFilesResponse(
                TotalFiles: analysisResult.IndividualResults.Count,
                TotalRows: totalRowsSum,
                CrossFileDuplicates: analysisResult.CrossFileValidation.TotalDuplicateRows,
                CrossFileValidationErrors: crossFileValidationErrors,
                ProcessingTimeMs: analysisResult.SummaryStatistics.ProcessingTime.TotalMilliseconds,
                DataQualityScore: averageDataQualityScore,
                IndividualResults: individualResults,
                JsonReportPath: null,
                ExcelReportPath: null,
                FileMovementErrors: fileMovementErrors
            );

            logger.LogInformation(
                "ProcessMultipleLgdFiles completed: {TotalFiles} files, {TotalRows} rows, Quality Score: {QualityScore}",
                response.TotalFiles, response.TotalRows, response.DataQualityScore);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ProcessMultipleLgdFiles exception: {Message}", ex.Message);
            return Result.Failure<ProcessMultipleLgdFilesResponse>(Error.Problem(
                "Processing.Failed",
                $"Failed to process LGD files: {ex.Message}"));
        }
    }

    private async Task UpdateFileStatusInConfig(
        CollectiveImpairmentConfig config,
        string year,
        string facilityStatus,
        List<string> storedFileNames,
        string status,
        bool isValidated,
        List<Domain.Files.FileValidationResult> validationResults,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingDoc = JsonDocument.Parse(config.ConfigJson);

            if (!existingDoc.RootElement.TryGetProperty("lgdFileUpload", out JsonElement lgdFileUpload) ||
                !lgdFileUpload.TryGetProperty(year, out JsonElement yearData))
            {
                return;
            }

            string facilityPropertyName = facilityStatus == "OpenFacility" ? "openFacility" : "closedFacility";

            if (!yearData.TryGetProperty(facilityPropertyName, out JsonElement facilityData) ||
                !facilityData.TryGetProperty("files", out _))
            {
                return;
            }

            // Rebuild JSON with updated file statuses and validation results
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("lgdFileUpload");
                writer.WriteStartObject();

                foreach (JsonProperty property in lgdFileUpload.EnumerateObject())
                {
                    if (property.Name == year)
                    {
                        WriteYearDataWithUpdatedStatus(writer, year, yearData, facilityStatus,
                            storedFileNames, status, isValidated, validationResults);
                    }
                    else
                    {
                        writer.WritePropertyName(property.Name);
                        property.Value.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            config.ConfigJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            dbContext.CollectiveImpairmentConfigs.Update(config);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update file status in config");
        }
    }

    private static void WriteYearDataWithUpdatedStatus(
        Utf8JsonWriter writer,
        string year,
        JsonElement yearData,
        string targetFacilityStatus,
        List<string> storedFileNames,
        string status,
        bool isValidated,
        List<Domain.Files.FileValidationResult> validationResults)
    {
        writer.WritePropertyName(year);
        writer.WriteStartObject();

        foreach (JsonProperty property in yearData.EnumerateObject())
        {
            if (property.Name == "openFacility" && targetFacilityStatus == "OpenFacility")
            {
                WriteUpdatedFacilityData(writer, "openFacility", property.Value, storedFileNames,
                    status, isValidated, validationResults);
            }
            else if (property.Name == "closedFacility" && targetFacilityStatus == "ClosedFacility")
            {
                WriteUpdatedFacilityData(writer, "closedFacility", property.Value, storedFileNames,
                    status, isValidated, validationResults);
            }
            else
            {
                writer.WritePropertyName(property.Name);
                property.Value.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
    }

    private static void WriteUpdatedFacilityData(
        Utf8JsonWriter writer,
        string facilityName,
        JsonElement facilityData,
        List<string> storedFileNames,
        string status,
        bool isValidated,
        List<Domain.Files.FileValidationResult> validationResults)
    {
        writer.WritePropertyName(facilityName);
        writer.WriteStartObject();
        writer.WritePropertyName("files");
        writer.WriteStartArray();

        if (facilityData.TryGetProperty("files", out JsonElement filesArray))
        {
            foreach (JsonElement file in filesArray.EnumerateArray())
            {
                string? storedFileName = file.TryGetProperty("storedFileName", out JsonElement sfn)
                    ? sfn.GetString()
                    : null;

                if (storedFileName != null && storedFileNames.Contains(storedFileName))
                {
                    // Find validation result for this file
                    Domain.Files.FileValidationResult? validationResult = validationResults
                        .FirstOrDefault(vr => vr.Filename == storedFileName);

                    // Update status for this file
                    writer.WriteStartObject();
                    foreach (JsonProperty fileProp in file.EnumerateObject())
                    {
                        if (fileProp.Name == "status")
                        {
                            writer.WriteString("status", status);
                        }
                        else if (fileProp.Name == "isValidated")
                        {
                            writer.WriteBoolean("isValidated", isValidated);
                        }
                        else if (fileProp.Name == "validationResult")
                        {
                            // Skip existing validationResult, we'll write new one below
                            continue;
                        }
                        else
                        {
                            writer.WritePropertyName(fileProp.Name);
                            fileProp.Value.WriteTo(writer);
                        }
                    }

                    // Write validation result if available
                    if (validationResult is not null)
                    {
                        writer.WritePropertyName("validationResult");
                        writer.WriteStartObject();
                        writer.WriteNumber("total_rows", validationResult.TotalRows);
                        writer.WriteNumber("total_errors", validationResult.TotalErrors);
                        writer.WriteString("status", validationResult.Status);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                }
                else
                {
                    file.WriteTo(writer);
                }
            }
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
