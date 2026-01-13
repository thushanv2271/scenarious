using System.Globalization;
using System.Text.Json;
using Application.Abstractions.Calculations;
using Application.Abstractions.Data;
using Application.Abstractions.Pipeline;
using Application.DTOs.LGDCalculation;
using Application.LGD.Services;
using Application.Models;
using Domain.CollectiveImpairment;
using Domain.LGDCalculation;
using Domain.LGDProgressTrackings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Infrastructure.Services.Pipeline;

/// <summary>
/// Orchestration-level implementation for executing the complete LGD calculation pipeline
/// </summary>
internal sealed class LgdPipelineService : ILgdPipelineService
{
    // Add a static readonly JsonSerializerOptions field to cache the options
    private static readonly JsonSerializerOptions CachedJsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILgdCalculationService _lgdCalculationService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<LgdPipelineService> _logger;
    private readonly ILgdProgressPublisher _publisher;
    private Guid _sessionId = Guid.NewGuid();

    public LgdPipelineService(
        ILgdCalculationService lgdCalculationService,
        IApplicationDbContext dbContext,
        ILgdProgressPublisher publisher,
        ILogger<LgdPipelineService> logger)
    {
        _lgdCalculationService = lgdCalculationService;
        _dbContext = dbContext;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task<Result<Step5FinancialYearLgdResult>> RunPipelineFromDbAsync(
        string createdBy,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _sessionId = sessionId;
        if (string.IsNullOrWhiteSpace(createdBy))
        {
            return Result.Failure<Step5FinancialYearLgdResult>(Error.Validation(
                "LgdPipeline.InvalidInput",
                "Created by cannot be null or empty"));
        }

        var totalPipelineStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("=== LGD Calculation Pipeline Started === Initiated by: {CreatedBy}", createdBy);

        try
        {
            // ====================================================================
            // PHASE 1: Fetch Configuration from Database
            // ====================================================================
            _logger.LogInformation("PHASE 1: Fetching LGD configuration from database...");
            await _publisher.PublishProgress(_sessionId, 1, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);

            Result<LgdPipelineConfiguration> configResult = await FetchPipelineConfigurationAsync(cancellationToken);
            if (configResult.IsFailure)
            {
                await _publisher.PublishProgress(_sessionId, 1, 1, LgdProgressStatus.Failed, configResult.Error.Description, cancellationToken);
                return Result.Failure<Step5FinancialYearLgdResult>(configResult.Error);
            }

            LgdPipelineConfiguration config = configResult.Value;
            _logger.LogInformation("Configuration fetched successfully. VC Points provided: {VcPointsProvided}", config.VcPointsByClassification is not null);

            // Parse financial year ends from the configuration
            List<DateTime> financialYearEnds = ParseFinancialYearEnds(config.FinancialYearEnds);
            if (!financialYearEnds.Any())
            {
                await _publisher.PublishProgress(_sessionId, 1, 1, LgdProgressStatus.Failed, "No valid financial year ends found", cancellationToken);
                return Result.Failure<Step5FinancialYearLgdResult>(Error.Validation(
                    "LgdPipeline.InvalidConfiguration",
                    "No valid financial year ends found in configuration"));
            }

            await _publisher.PublishProgress(_sessionId, 1, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

            // ====================================================================
            // PHASE 2: Execute LGD Pipeline Steps
            // ====================================================================

            #region Step 1a - LGD Data Preparation

            _logger.LogInformation("Starting Step 1a - LGD Data Preparation");
            await _publisher.PublishProgress(_sessionId, 1, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
            Result lgdStep1Result = await _lgdCalculationService.ExecuteStep1Async(
                createdBy,
                LgdCalculationType.LGD,
                cancellationToken);

            if (!lgdStep1Result.IsSuccess)
            {
                _logger.LogWarning("Step 1a (LGD) of LGD Calculation Pipeline failed: {Error}", lgdStep1Result.Error.Description);
                await _publisher.PublishProgress(_sessionId, 1, 1, LgdProgressStatus.Failed, lgdStep1Result.Error.Description, cancellationToken);
                return Result.Failure<Step5FinancialYearLgdResult>(lgdStep1Result.Error);
            }
            _logger.LogInformation("Step 1a (LGD) of LGD Calculation Pipeline executed successfully.");
            await _publisher.PublishProgress(_sessionId, 1, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

            #endregion

            #region Step 1b - VC_LGD Data Preparation

            _logger.LogInformation("Starting Step 1b - VC_LGD Data Preparation");
            await _publisher.PublishProgress(_sessionId, 1, 2, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
            Result vcLgdStep1Result = await _lgdCalculationService.ExecuteStep1Async(
                createdBy,
                LgdCalculationType.VC_LGD,
                cancellationToken);

            if (!vcLgdStep1Result.IsSuccess)
            {
                _logger.LogWarning("Step 1b (VC_LGD) of LGD Calculation Pipeline failed: {Error}", vcLgdStep1Result.Error.Description);
                await _publisher.PublishProgress(_sessionId, 1, 2, LgdProgressStatus.Failed, vcLgdStep1Result.Error.Description, cancellationToken);
                return Result.Failure<Step5FinancialYearLgdResult>(vcLgdStep1Result.Error);
            }
            _logger.LogInformation("Step 1b (VC_LGD) of LGD Calculation Pipeline executed successfully.");
            await _publisher.PublishProgress(_sessionId, 1, 2, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

            #endregion

            #region Step 2 - Discounted Cashflow Summary

            _logger.LogInformation("Starting Step 2 - LGD Discounted Cashflow Summary");
            await _publisher.PublishProgress(_sessionId, 2, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
            Result<HierarchicalStep2LgdCalculationResult> step2Result = await _lgdCalculationService.ExecuteStep2Async(
                calculationType: LgdCalculationType.LGD,
                vcPoint: null,
                vcPointsByClassification: null,
                cancellationToken: cancellationToken);

            if (!step2Result.IsSuccess)
            {
                _logger.LogWarning("Step 2 of LGD Calculation Pipeline failed: {Error}", step2Result.Error.Description);
                await _publisher.PublishProgress(_sessionId, 2, 1, LgdProgressStatus.Failed, step2Result.Error.Description, cancellationToken);
                return Result.Failure<Step5FinancialYearLgdResult>(step2Result.Error);
            }

            _logger.LogInformation("Step 2 of LGD Calculation Pipeline executed successfully.");
            await _publisher.PublishProgress(_sessionId, 2, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

            HierarchicalStep2LgdCalculationResult hierarchicalResult = step2Result.Value;

            #endregion

            HierarchicalStep2LgdCalculationResult? step2VcLgdResult = null;

            // Check if VC points are provided in configuration
            if (config.VcPointsByClassification is not null && config.VcPointsByClassification.Any())
            {
                _logger.LogInformation("VC Points provided in configuration. Skipping Steps 3-4 and proceeding to Step 2 VC_LGD.");

                #region Step 2 VC_LGD - Discounted Cashflow Summary with VC points

                _logger.LogInformation("Starting Step 2 VC_LGD - Discounted Cashflow Summary with VC points");
                await _publisher.PublishProgress(_sessionId, 5, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
                Result<HierarchicalStep2LgdCalculationResult> vcLgdStep2Result = await _lgdCalculationService.ExecuteStep2Async(
                    calculationType: LgdCalculationType.VC_LGD,
                    vcPoint: null,
                    vcPointsByClassification: config.VcPointsByClassification,
                    cancellationToken: cancellationToken);

                if (!vcLgdStep2Result.IsSuccess)
                {
                    _logger.LogWarning("Step 2 VC_LGD of LGD Calculation Pipeline failed: {Error}", vcLgdStep2Result.Error.Description);
                    await _publisher.PublishProgress(_sessionId, 5, 1, LgdProgressStatus.Failed, vcLgdStep2Result.Error.Description, cancellationToken);
                    return Result.Failure<Step5FinancialYearLgdResult>(vcLgdStep2Result.Error);
                }

                _logger.LogInformation("Step 2 VC_LGD of LGD Calculation Pipeline executed successfully.");
                await _publisher.PublishProgress(_sessionId, 5, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

                step2VcLgdResult = vcLgdStep2Result.Value;

                #endregion
            }
            else
            {
                _logger.LogInformation("No VC Points provided. Executing Steps 3-4 for VC point determination.");

                #region Step 3 - Yearly LGD Average

                _logger.LogInformation("Starting Step 3 - Yearly LGD Average");
                await _publisher.PublishProgress(_sessionId, 3, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
                Result<Step3YearlyLgdAverageResult> step3Result = await _lgdCalculationService.ExecuteStep3Async(
                    hierarchicalResult,
                    cancellationToken);

                if (!step3Result.IsSuccess)
                {
                    _logger.LogWarning("Step 3 of LGD Calculation Pipeline failed: {Error}", step3Result.Error.Description);
                    await _publisher.PublishProgress(_sessionId, 3, 1, LgdProgressStatus.Failed, step3Result.Error.Description, cancellationToken);
                    return Result.Failure<Step5FinancialYearLgdResult>(step3Result.Error);
                }

                _logger.LogInformation("Step 3 of LGD Calculation Pipeline executed successfully.");
                await _publisher.PublishProgress(_sessionId, 3, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

                #endregion

                #region Step 4 - VC-Point Determination

                _logger.LogInformation("Starting Step 4 - VC-Point Determination");
                await _publisher.PublishProgress(_sessionId, 4, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
                Result<Step4VcPointDeterminationResult> step4ExecutionResult = await _lgdCalculationService.ExecuteStep4Async(
                    step3Result.Value,
                    VcPointDeterminationMethod.MaxDeltaLgdMinusOne,
                    cancellationToken);

                if (!step4ExecutionResult.IsSuccess)
                {
                    _logger.LogWarning("Step 4 of LGD Calculation Pipeline failed: {Error}", step4ExecutionResult.Error.Description);
                    await _publisher.PublishProgress(_sessionId, 4, 1, LgdProgressStatus.Failed, step4ExecutionResult.Error.Description, cancellationToken);
                    return Result.Failure<Step5FinancialYearLgdResult>(step4ExecutionResult.Error);
                }

                Step4VcPointDeterminationResult step4Result = step4ExecutionResult.Value;
                _logger.LogInformation("Step 4 of LGD Calculation Pipeline executed successfully.");
                await _publisher.PublishProgress(_sessionId, 4, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

                #endregion

                #region Step 2 VC_LGD - Discounted Cashflow Summary with determined VC points

                _logger.LogInformation("Starting Step 2 VC_LGD - Discounted Cashflow Summary with determined VC points");
                await _publisher.PublishProgress(_sessionId, 5, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);

                // Convert Step4VcPointDeterminationResult to Dictionary<string, decimal>
                var convertedVcPoints = step4Result.ClassificationResults
                    .ToDictionary(vc => vc.Classification, vc => (decimal)vc.VcPoint);

                Result<HierarchicalStep2LgdCalculationResult> vcLgdStep2Result = await _lgdCalculationService.ExecuteStep2Async(
                    calculationType: LgdCalculationType.VC_LGD,
                    vcPoint: null,
                    vcPointsByClassification: convertedVcPoints,
                    cancellationToken: cancellationToken);

                if (!vcLgdStep2Result.IsSuccess)
                {
                    _logger.LogWarning("Step 2 VC_LGD of LGD Calculation Pipeline failed: {Error}", vcLgdStep2Result.Error.Description);
                    await _publisher.PublishProgress(_sessionId, 5, 1, LgdProgressStatus.Failed, vcLgdStep2Result.Error.Description, cancellationToken);
                    return Result.Failure<Step5FinancialYearLgdResult>(vcLgdStep2Result.Error);
                }

                _logger.LogInformation("Step 2 VC_LGD of LGD Calculation Pipeline executed successfully.");
                await _publisher.PublishProgress(_sessionId, 5, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

                step2VcLgdResult = vcLgdStep2Result.Value;

                #endregion
            }

            #region Step 5 LGD - Financial Year Analysis

            _logger.LogInformation("Starting Step 5 LGD - Financial Year Analysis");
            await _publisher.PublishProgress(_sessionId, 6, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
            Result<Step5FinancialYearLgdResult> lgdStep5Result = await _lgdCalculationService.ExecuteStep5Async(
                hierarchicalResult,
                financialYearEnds,
                cancellationToken);

            if (!lgdStep5Result.IsSuccess)
            {
                _logger.LogWarning("Step 5 LGD of LGD Calculation Pipeline failed: {Error}", lgdStep5Result.Error.Description);
                await _publisher.PublishProgress(_sessionId, 6, 1, LgdProgressStatus.Failed, lgdStep5Result.Error.Description, cancellationToken);
                return Result.Failure<Step5FinancialYearLgdResult>(lgdStep5Result.Error);
            }

            _logger.LogInformation("Step 5 LGD of LGD Calculation Pipeline executed successfully.");
            await _publisher.PublishProgress(_sessionId, 6, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

            #endregion

            #region Step 5 VC_LGD - Financial Year Analysis

            _logger.LogInformation("Starting Step 5 VC_LGD - Financial Year Analysis");
            await _publisher.PublishProgress(_sessionId, 6, 2, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
            Result<Step5FinancialYearLgdResult> vcLgdStep5Result = await _lgdCalculationService.ExecuteStep5Async(
                step2VcLgdResult!,
                financialYearEnds,
                cancellationToken);

            if (!vcLgdStep5Result.IsSuccess)
            {
                _logger.LogWarning("Step 5 VC_LGD of LGD Calculation Pipeline failed: {Error}", vcLgdStep5Result.Error.Description);
                await _publisher.PublishProgress(_sessionId, 6, 2, LgdProgressStatus.Failed, vcLgdStep5Result.Error.Description, cancellationToken);
                return Result.Failure<Step5FinancialYearLgdResult>(vcLgdStep5Result.Error);
            }

            _logger.LogInformation("Step 5 VC_LGD of LGD Calculation Pipeline executed successfully.");
            await _publisher.PublishProgress(_sessionId, 6, 2, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

            #endregion

            #region Step 6 - Final Result Combination

            _logger.LogInformation("Starting Step 6 - Final Result Combination");
            await _publisher.PublishProgress(_sessionId, 7, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
            Result<Step5FinancialYearLgdResult> step6Result = await _lgdCalculationService.ExecuteStep6Async(
                lgdStep5Result.Value,
                vcLgdStep5Result.Value,
                cancellationToken);

            if (!step6Result.IsSuccess)
            {
                _logger.LogWarning("Step 6 of LGD Calculation Pipeline failed: {Error}", step6Result.Error.Description);
                await _publisher.PublishProgress(_sessionId, 7, 1, LgdProgressStatus.Failed, step6Result.Error.Description, cancellationToken);
                return Result.Failure<Step5FinancialYearLgdResult>(step6Result.Error);
            }

            _logger.LogInformation("Step 6 of LGD Calculation Pipeline executed successfully.");
            await _publisher.PublishProgress(_sessionId, 7, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

            #endregion

            // ====================================================================
            // PIPELINE COMPLETED SUCCESSFULLY
            // ====================================================================
            totalPipelineStopwatch.Stop();
            _logger.LogInformation("=== LGD Calculation Pipeline Completed Successfully === Total time: {ElapsedMs}ms ({ElapsedSec:F2}s)",
                totalPipelineStopwatch.ElapsedMilliseconds, totalPipelineStopwatch.Elapsed.TotalSeconds);

            // ====================================================================
            // PHASE 7: Save LGD Algorithm Result to database
            // ====================================================================
            _logger.LogInformation("PHASE 7: Saving LGD Algorithm Result to database...");
            var saveStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Parse createdBy to Guid (assuming it's a user ID string representation)
            if (!Guid.TryParse(createdBy, out Guid createdByUserId))
            {
                _logger.LogWarning("CreatedBy value '{CreatedBy}' is not a valid GUID, using default Guid.Empty", createdBy);
                createdByUserId = Guid.Empty;
            }

            // Check if there are existing records and delete them
            List<LgdAlgorithmResult> existingRecords = await _dbContext.LgdAlgorithmResults
                .Where(x => x.CreatedBy == createdByUserId)
                .ToListAsync(cancellationToken);

            if (existingRecords.Count > 0)
            {
                _logger.LogInformation("Found {Count} existing LGD Algorithm Results for user {UserId}. Deleting them.", existingRecords.Count, createdByUserId);
                _dbContext.LgdAlgorithmResults.RemoveRange(existingRecords);
            }
            else
            {
                _logger.LogInformation("No existing LGD Algorithm Results found");
            }

            // Serialize the LGD result to JSON
            string lgdAlgorithmJson = JsonSerializer.Serialize(step6Result.Value, CachedJsonOptions);

            // Create the LGD Algorithm Result entity
            var lgdAlgorithmResult = LgdAlgorithmResult.Create(
                lgdAlgorithmJson,
                createdByUserId
            );

            // Save to database
            _dbContext.LgdAlgorithmResults.Add(lgdAlgorithmResult);
            await _dbContext.SaveChangesAsync(cancellationToken);

            saveStopwatch.Stop();
            _logger.LogInformation(
                "LGD Algorithm Result saved successfully in {ElapsedMs}ms. ID: {Id}, CreatedBy: {UserId}",
                saveStopwatch.ElapsedMilliseconds,
                lgdAlgorithmResult.Id,
                createdByUserId);

            return step6Result;
        }
        catch (Exception ex)
        {
            await _publisher.PublishProgress(_sessionId, 2, 1, LgdProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 3, 1, LgdProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 4, 1, LgdProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 5, 1, LgdProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 6, 1, LgdProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 6, 2, LgdProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 7, 1, LgdProgressStatus.Failed, cancellationToken: cancellationToken);
            totalPipelineStopwatch.Stop();
            _logger.LogError(ex, "Unexpected error occurred during LGD pipeline execution after {ElapsedMs}ms: {ErrorMessage}",
                totalPipelineStopwatch.ElapsedMilliseconds, ex.Message);

            return Result.Failure<Step5FinancialYearLgdResult>(Error.Failure(
                "LgdPipeline.UnexpectedError",
                $"An unexpected error occurred during pipeline execution: {ex.Message}"));
        }
    }

    /// <summary>
    /// Fetches all required configuration from the database for pipeline execution
    /// </summary>
    private async Task<Result<LgdPipelineConfiguration>> FetchPipelineConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            // ====================================================================
            // Fetch LGD configuration from collective_impairment_configs table
            // ====================================================================
            CollectiveImpairmentConfig? lgdConfig = await _dbContext.CollectiveImpairmentConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.Parameter == ParameterType.LGD,
                    cancellationToken);

            if (lgdConfig is null)
            {
                _logger.LogError("LGD configuration not found in collective_impairment_configs table");
                return Result.Failure<LgdPipelineConfiguration>(Error.NotFound(
                    "LgdPipeline.ConfigurationNotFound",
                    "LGD configuration not found in database"));
            }

            _logger.LogInformation("Found LGD configuration with ID: {ConfigId}", lgdConfig.Id);

            // ====================================================================
            // Parse and validate configuration JSON
            // ====================================================================
            LgdConfigurationJson? configJson;
            try
            {
                configJson = JsonSerializer.Deserialize<LgdConfigurationJson>(lgdConfig.ConfigJson, CachedJsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize LGD configuration JSON");
                return Result.Failure<LgdPipelineConfiguration>(Error.Validation(
                    "LgdPipeline.InvalidConfigurationFormat",
                    "LGD configuration JSON format is invalid"));
            }

            if (configJson?.LgdFileUpload is null)
            {
                _logger.LogError("LGD file upload configuration is missing or null");
                return Result.Failure<LgdPipelineConfiguration>(Error.Validation(
                    "LgdPipeline.MissingFileUploadConfig",
                    "LGD file upload configuration is missing"));
            }

            // Extract financial year ends from the configuration
            List<string> financialYearEnds = new();
            if (configJson.LgdFileUpload?.Count > 0)
            {
                // Get all years from financial year data
                foreach (KeyValuePair<string, object> kvp in configJson.LgdFileUpload)
                {
                    if (kvp.Key != "timePeriod" && kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Object && element.TryGetProperty("financialYearEnd", out JsonElement fyeElement))
                    {
                        string? fye = fyeElement.GetString();
                        if (!string.IsNullOrWhiteSpace(fye))
                        {
                            financialYearEnds.Add(fye);
                        }
                    }
                }
            }

            // Extract VC points if provided
            Dictionary<string, decimal>? vcPointsByClassification = null;
            if (configJson.LgdVCPointData?.Assignments is not null && configJson.LgdVCPointData.Assignments.Any())
            {
                vcPointsByClassification = new Dictionary<string, decimal>();
                foreach (VcPointAssignment assignment in configJson.LgdVCPointData.Assignments.Values)
                {
                    if (!string.IsNullOrWhiteSpace(assignment.VcPoint))
                    {
                        // Convert VC point string to decimal (assuming format like "1year" -> 1, "2years" -> 2, etc.)
                        string vcPointStr = assignment.VcPoint.ToUpperInvariant().Replace("YEAR", "").Replace("S", "");
                        if (int.TryParse(vcPointStr, out int vcPointInt))
                        {
                            vcPointsByClassification[assignment.SegmentId] = (decimal)vcPointInt;
                        }
                    }
                }
            }

            LgdPipelineConfiguration pipelineConfig = new()
            {
                FinancialYearEnds = financialYearEnds,
                VcPointsByClassification = vcPointsByClassification
            };

            _logger.LogInformation("LGD pipeline configuration parsed successfully. Financial Years: {Count}, VC Points: {VcPointsCount}",
                financialYearEnds.Count, vcPointsByClassification?.Count ?? 0);

            return Result.Success(pipelineConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching LGD pipeline configuration");
            return Result.Failure<LgdPipelineConfiguration>(Error.Failure(
                "LgdPipeline.ConfigurationFetchError",
                $"Failed to fetch pipeline configuration: {ex.Message}"));
        }
    }

    /// <summary>
    /// Parses financial year end strings to DateTime objects
    /// </summary>
    private List<DateTime> ParseFinancialYearEnds(List<string> financialYearEndStrings)
    {
        List<DateTime> financialYearEnds = new();

        foreach (string fyeString in financialYearEndStrings)
        {
            if (DateTime.TryParseExact(fyeString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fye))
            {
                financialYearEnds.Add(fye);
            }
            else if (DateTime.TryParse(fyeString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fyeParsed))
            {
                financialYearEnds.Add(fyeParsed);
            }
            else
            {
                _logger.LogWarning("Could not parse financial year end: {FYE}", fyeString);
            }
        }

        return financialYearEnds.OrderBy(d => d).ToList();
    }
}

/// <summary>
/// Configuration extracted from database for LGD pipeline execution
/// </summary>
public sealed class LgdPipelineConfiguration
{
    public required List<string> FinancialYearEnds { get; init; } = new();
    public Dictionary<string, decimal>? VcPointsByClassification { get; init; }
}

/// <summary>
/// LGD configuration JSON structure for deserialization
/// </summary>
public sealed class LgdConfigurationJson
{
    public Dictionary<string, object>? LgdFileUpload { get; init; }
    public LgdVCPointData? LgdVCPointData { get; init; }
}

/// <summary>
/// VC Point data structure from configuration
/// </summary>
public sealed class LgdVCPointData
{
    public Dictionary<string, VcPointAssignment>? Assignments { get; init; }
}

/// <summary>
/// Individual VC point assignment
/// </summary>
public sealed class VcPointAssignment
{
    public required string ProductCategoryId { get; init; }
    public required string SegmentId { get; init; }
    public required string VcPoint { get; init; }
    public string? Reason { get; init; }
}