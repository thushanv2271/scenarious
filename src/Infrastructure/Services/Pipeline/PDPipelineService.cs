using System.Globalization;
using System.Text.Json;
using Application.Abstractions.Calculations;
using Application.Abstractions.Data;
using Application.Abstractions.Pipeline;
using Application.DTOs.PD;
using Application.Models;
using Application.Models.PDSummary;
using Application.PD;
using Application.PD.Services;
using Azure.Core;
using DocumentFormat.OpenXml.Bibliography;
using Domain.CollectiveImpairment;
using Domain.Configurations;
using Domain.PDAlgorithmResults;
using Domain.PDProgressTrackings;
using Infrastructure.PD;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;
using SummaryModels = Application.Models.PDSummary;

namespace Infrastructure.Services.Pipeline;

/// <summary>
/// Orchestration-level implementation for executing the complete PD calculation pipeline
/// </summary>
internal sealed class PDPipelineService : IPDPipelineService
{
    // Add a static readonly JsonSerializerOptions field to cache the options
    private static readonly JsonSerializerOptions CachedJsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions JsonSerializeOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IPDCalculationService _pdCalculationService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<PDPipelineService> _logger;
    private readonly IPDProgressPublisher _publisher;
    private Guid _sessionId = Guid.NewGuid();

    public PDPipelineService(
        IPDCalculationService pdCalculationService,
        IApplicationDbContext dbContext,
        IPDProgressPublisher publisher,
        ILogger<PDPipelineService> logger)
    {
        _pdCalculationService = pdCalculationService;
        _dbContext = dbContext;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task<Result<PdExtrapolationResultDto>> RunPipelineFromDbAsync(
        string createdBy,
        Guid SessionId,
        CancellationToken cancellationToken = default)
    {
        _sessionId = SessionId;
        if (string.IsNullOrWhiteSpace(createdBy))
        {
            return Result.Failure<PdExtrapolationResultDto>(Error.Validation(
             "PDPipeline.InvalidInput",
                "Created by cannot be null or empty"));
        }

        var totalPipelineStopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("=== PD Calculation Pipeline Started === Initiated by: {CreatedBy}", createdBy);

        try
        {
            // ====================================================================
            // PHASE 1: Fetch Configuration from Database
            // ====================================================================
            _logger.LogInformation("PHASE 1: Fetching PD configuration from database...");
            Result<PDPipelineConfiguration> configResult = await FetchPipelineConfigurationAsync(cancellationToken);

            if (!configResult.IsSuccess)
            {
                _logger.LogError("Failed to fetch pipeline configuration: {Error}", configResult.Error.Description);
                return Result.Failure<PdExtrapolationResultDto>(configResult.Error);
            }

            PDPipelineConfiguration config = configResult.Value;
            _logger.LogInformation("Configuration fetched successfully: Frequency={Frequency}, Buckets={BucketCount}, Configurations={ConfigCount}",
            config.TimeConfig.Frequency, config.DatePassedDueBuckets.Count, config.PDConfigurations.Count);

            // ====================================================================
            // PHASE 2: Execute Step 1 - File Extraction and Calculation
            // ====================================================================
            await _publisher.PublishProgress(_sessionId, 2, 1, PDProgressStatus.InProgress, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 2, 1, PDProgressStatus.Completed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 2, 2, PDProgressStatus.InProgress, cancellationToken: cancellationToken);
            _logger.LogInformation("PHASE 2: Executing Step 1 - File extraction, calculation, and DB insertion...");
            var step1Stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Result step1Result = await _pdCalculationService.ExecuteStep1Async(
            config.QuarterEndedDates,
            config.DatePassedDueBuckets,
            config.FinalBucketPayload,
            createdBy,
            null,
            cancellationToken);

            step1Stopwatch.Stop();

            if (!step1Result.IsSuccess)
            {
                _logger.LogError("Step 1 failed after {ElapsedMs}ms: {Error}",
                step1Stopwatch.ElapsedMilliseconds, step1Result.Error.Description);
                return Result.Failure<PdExtrapolationResultDto>(step1Result.Error);
            }
            
            await _publisher.PublishProgress(_sessionId, 2, 2, PDProgressStatus.Completed, cancellationToken: cancellationToken);
            _logger.LogInformation("Step 1 completed successfully in {ElapsedMs}ms ({ElapsedSec:F2}s)",
            step1Stopwatch.ElapsedMilliseconds, step1Stopwatch.Elapsed.TotalSeconds);

            await _publisher.PublishProgress(_sessionId, 2, 3, PDProgressStatus.InProgress, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 2, 3, PDProgressStatus.Completed, cancellationToken: cancellationToken);
            // ====================================================================
            // PHASE 3: Execute Step 2 - Migration Matrix Generation
            // ====================================================================
            await _publisher.PublishProgress(_sessionId, 3, 1, PDProgressStatus.InProgress, cancellationToken: cancellationToken);
            _logger.LogInformation("PHASE 3: Executing Step 2 - Migration matrix generation...");
            var step2Stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Result<IReadOnlyList<PeriodMigrationMatrix>> step2Result = await _pdCalculationService.ExecuteStep2Async(
                config.TimeConfig,
                config.DatePassedDueBuckets,
                config.PDConfigurations,
                createdBy,
                cancellationToken);

            step2Stopwatch.Stop();

            if (!step2Result.IsSuccess)
            {
                _logger.LogError("Step 2 failed after {ElapsedMs}ms: {Error}",
                step2Stopwatch.ElapsedMilliseconds, step2Result.Error.Description);
                return Result.Failure<PdExtrapolationResultDto>(step2Result.Error);
            }

            await _publisher.PublishProgress(_sessionId, 3, 1, PDProgressStatus.Completed, cancellationToken: cancellationToken);
            _logger.LogInformation("Step 2 completed successfully in {ElapsedMs}ms ({ElapsedSec:F2}s) - Generated {MatrixCount} period migration matrices",
            step2Stopwatch.ElapsedMilliseconds, step2Stopwatch.Elapsed.TotalSeconds, step2Result.Value.Count);

            // ====================================================================
            // PHASE 4: Execute Step 3 - PD Summary Table Generation
            // ====================================================================
            await _publisher.PublishProgress(_sessionId, 4, 1, PDProgressStatus.InProgress, cancellationToken: cancellationToken);
            _logger.LogInformation("PHASE 4: Executing Step 3 - PD Summary table generation...");
            var step3Stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Convert PeriodMigrationMatrix to MigrationMatricesAndSummaryResponse format for Step 3
            SummaryModels.MigrationMatricesAndSummaryResponse migrationData = ConvertToMigrationMatricesResponse(step2Result.Value);

            Result<string> step3Result = await _pdCalculationService.ExecuteStep3Async(
                migrationData,
                cancellationToken);

            step3Stopwatch.Stop();

            if (!step3Result.IsSuccess)
            {
                _logger.LogError("Step 3 failed after {ElapsedMs}ms: {Error}",
                step3Stopwatch.ElapsedMilliseconds, step3Result.Error.Description);
                return Result.Failure<PdExtrapolationResultDto>(step3Result.Error);
            }
            await _publisher.PublishProgress(_sessionId, 4, 1, PDProgressStatus.Completed, cancellationToken: cancellationToken);
            _logger.LogInformation("Step 3 completed successfully in {ElapsedMs}ms ({ElapsedSec:F2}s)",
            step3Stopwatch.ElapsedMilliseconds, step3Stopwatch.Elapsed.TotalSeconds);

            // TODO: Parse step3Result.Value (JSON string) to AveragePDTablesResponse
            // For now, passing null to Step 4 - implementation needed
            AveragePDTablesResponse? step3Data = JsonSerializer.Deserialize<AveragePDTablesResponse>(step3Result.Value, CachedJsonOptions);

            // ====================================================================
            // PHASE 5: Execute Step 4 - PD Extrapolation
            // ====================================================================
            await _publisher.PublishProgress(_sessionId, 5, 1, PDProgressStatus.InProgress, cancellationToken: cancellationToken);
            _logger.LogInformation("PHASE 5: Executing Step 4 - PD extrapolation tables generation...");
            var step4Stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Result<PdExtrapolationResultDto> step4Result = await _pdCalculationService.ExecuteStep4Async(
                step3Data,
                createdBy,
                cancellationToken);

            step4Stopwatch.Stop();

            if (!step4Result.IsSuccess)
            {
                _logger.LogError("Step 4 failed after {ElapsedMs}ms: {Error}",
                step4Stopwatch.ElapsedMilliseconds, step4Result.Error.Description);
                return Result.Failure<PdExtrapolationResultDto>(step4Result.Error);
            }

            _logger.LogInformation("Step 4 completed successfully in {ElapsedMs}ms ({ElapsedSec:F2}s)",
            step4Stopwatch.ElapsedMilliseconds, step4Stopwatch.Elapsed.TotalSeconds);
            await _publisher.PublishProgress(_sessionId, 5, 1, PDProgressStatus.Completed, cancellationToken: cancellationToken);
            // ====================================================================
            // PHASE 6: Save PD Algorithm Result to Database
            // ====================================================================
            await SavePDAlgorithmResultAsync(step4Result.Value, createdBy, cancellationToken);

            // ====================================================================
            // PIPELINE COMPLETED SUCCESSFULLY
            // ====================================================================
            totalPipelineStopwatch.Stop();
            _logger.LogInformation("=== PD Calculation Pipeline Completed Successfully === Total time: {ElapsedMs}ms ({ElapsedSec:F2}s)",
            totalPipelineStopwatch.ElapsedMilliseconds, totalPipelineStopwatch.Elapsed.TotalSeconds);

            return step4Result;
        }
        catch (Exception ex)
        {
            await _publisher.PublishProgress(_sessionId, 2, 1, PDProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 2, 2, PDProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 2, 3, PDProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 3, 1, PDProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 4, 1, PDProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 5, 1, PDProgressStatus.Failed, cancellationToken: cancellationToken);
            totalPipelineStopwatch.Stop();
            _logger.LogError(ex, "Unexpected error occurred during PD pipeline execution after {ElapsedMs}ms: {ErrorMessage}",
            totalPipelineStopwatch.ElapsedMilliseconds, ex.Message);

            return Result.Failure<PdExtrapolationResultDto>(Error.Failure(
                "PDPipeline.UnexpectedError",
                $"An unexpected error occurred during pipeline execution: {ex.Message}"));
        }
    }

    /// <summary>
    /// Fetches all required configuration from the database for pipeline execution
    /// </summary>
    private async Task<Result<PDPipelineConfiguration>> FetchPipelineConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            // ====================================================================
            // Fetch PD configuration from collective_impairment_configs table
            // ====================================================================
            CollectiveImpairmentConfig? pdConfig = await _dbContext.CollectiveImpairmentConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.Parameter == ParameterType.PD, 
                    cancellationToken);

            if (pdConfig is null)
            {
                _logger.LogError("PD configuration not found in collective_impairment_configs table");
                return Result.Failure<PDPipelineConfiguration>(Error.NotFound(
                    "PDPipeline.ConfigurationNotFound",
                    "PD configuration not found in database"));
            }

            if (string.IsNullOrWhiteSpace(pdConfig.ConfigJson))
            {
                _logger.LogError("PD configuration JSON is empty or null");
                return Result.Failure<PDPipelineConfiguration>(Error.Validation(
                    "PDPipeline.InvalidConfiguration",
                    "PD configuration JSON is empty or null"));
            }

            _logger.LogInformation("PD configuration fetched from database successfully");

            // ====================================================================
            // Parse the JSON configuration
            // ====================================================================
            PDConfigJson? configJson;
            try
            {
                // Use the cached JsonSerializerOptions instance
                configJson = JsonSerializer.Deserialize<PDConfigJson>(pdConfig.ConfigJson, CachedJsonOptions);

                if (configJson is null)
                {
                    _logger.LogError("Failed to deserialize PD configuration JSON");
                    return Result.Failure<PDPipelineConfiguration>(Error.Validation(
                        "PDPipeline.InvalidConfiguration",
                        "Failed to deserialize PD configuration JSON"));
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing PD configuration JSON: {ErrorMessage}", ex.Message);
                return Result.Failure<PDPipelineConfiguration>(Error.Validation(
                    "PDPipeline.InvalidConfiguration",
                    $"Invalid JSON format in PD configuration: {ex.Message}"));
            }

            _logger.LogInformation("PD configuration JSON parsed successfully");

            // ====================================================================
            // Extract TimeConfig
            // ====================================================================
            await _publisher.PublishProgress(_sessionId, 1, 1, PDProgressStatus.InProgress, cancellationToken: cancellationToken);
            TimeConfig timeConfig = new() 
            { 
                Frequency = configJson.PdSetup.Frequency 
            };

            if (!timeConfig.IsValid())
            {
                return Result.Failure<PDPipelineConfiguration>(Error.Validation(
                    "PDPipeline.InvalidConfiguration",
                    $"Invalid frequency value: {configJson.PdSetup.Frequency}"));
            }
            await _publisher.PublishProgress(_sessionId, 1, 1, PDProgressStatus.Completed, cancellationToken: cancellationToken);
            // ====================================================================
            // Extract QuarterEndedDates from pdFileUpload
            // ====================================================================
            Dictionary<string, DateTime> quarterEndedDates = new();
            foreach ((string period, PeriodFileUploadData fileData) in configJson.PdFileUpload)
            {
                if (DateTime.TryParse(fileData.FinancialYear, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime quarterEndedDate))
                {
                    quarterEndedDates["PD_"+ period] = quarterEndedDate;
                }
                else
                {
                    _logger.LogWarning("Failed to parse financial year for period {Period}: {FinancialYear}", 
                        period, fileData.FinancialYear);
                }
            }

            if (quarterEndedDates.Count == 0)
            {
                return Result.Failure<PDPipelineConfiguration>(Error.Validation(
                    "PDPipeline.InvalidConfiguration",
                    "No valid period dates found in pdFileUpload configuration"));
            }

            _logger.LogInformation("Extracted {Count} quarter ended dates from configuration", quarterEndedDates.Count);

            // ====================================================================
            // Fetch DatePassedDueBuckets from age_bucket_configurations table
            // ====================================================================
            await _publisher.PublishProgress(_sessionId, 1, 2, PDProgressStatus.InProgress, cancellationToken: cancellationToken);
            AgeBucketConfiguration? ageBucketConfig = await _dbContext.AgeBucketConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (ageBucketConfig is null)
            {
                _logger.LogError("Age bucket configuration not found in database");
                return Result.Failure<PDPipelineConfiguration>(Error.NotFound(
                    "PDPipeline.AgeBucketConfigurationNotFound",
                    "Age bucket configuration not found in database"));
            }

            if (ageBucketConfig.ConfigurationData?.DatePassedDueBasedConfigurations is null 
                || !ageBucketConfig.ConfigurationData.DatePassedDueBasedConfigurations.Any())
            {
                _logger.LogError("No date passed due based configurations found in age bucket configuration");
                return Result.Failure<PDPipelineConfiguration>(Error.Validation(
                    "PDPipeline.InvalidConfiguration",
                    "No date passed due based configurations found in age bucket configuration"));
            }

            List<DatePassedDueBucket> datePassedDueBuckets = ConvertToDatePassedDueBuckets(
                ageBucketConfig.ConfigurationData.DatePassedDueBasedConfigurations);

            _logger.LogInformation("Extracted {Count} date passed due buckets from configuration", datePassedDueBuckets.Count);
            await _publisher.PublishProgress(_sessionId, 1, 2, PDProgressStatus.Completed, cancellationToken: cancellationToken);

            await _publisher.PublishProgress(_sessionId, 1, 3, PDProgressStatus.InProgress, cancellationToken: cancellationToken);
            // ====================================================================
            // Convert PDConfigurationItem to PDConfiguration
            // ====================================================================
            var pdConfigurations = configJson.PdConfigurations.PdConfiguration
                .Select(item => new PDConfiguration
                {
                    ProductCategoryId = item.ProductCategoryId,
                    ProductCategory = item.ProductCategory,
                    Segment = item.Segment,
                    PDEstimationApproach = item.PdEstimationApproach,
                    ComparisonPeriod = item.ComparisonPeriod,
                    ConsiderNormalMaturities = item.ConsiderNormalMaturities,
                    ADS1 = item.AdvancedDefaultSearch,
                    ADS2 = item.AdvancedDefaultSearch,
                    Comments = item.Comments
                })
                .ToList();

            _logger.LogInformation("Converted {Count} PD configurations", pdConfigurations.Count);

            // ====================================================================
            // Extract FinalBucketPayload
            // ====================================================================
            FinalBucketPayload finalBucketPayload = new()
            {
                Type = configJson.PdConfigurations.MultiFacilityAdjustmentRule?.ToUpperInvariant() switch
                {
                    "ADJUST WITH % RULE" => FinalBucketTypes.Percentage,
                    "ADJUST WITH WORST" => FinalBucketTypes.Worst,
                    _ => FinalBucketTypes.None
                },
                Percentage = configJson.PdConfigurations.PercentRuleThreshold
            };

            if (!finalBucketPayload.IsValid())
            {
                _logger.LogWarning("Invalid final bucket payload configuration, using default 'None'");
                finalBucketPayload = new FinalBucketPayload { Type = FinalBucketTypes.None };
            }

            // ====================================================================
            // Determine file type suffix based on frequency
            // ====================================================================
            string? type = configJson.PdSetup.Frequency?.ToUpperInvariant() switch
            {
                "YEARLY" => "Yearly",
                "QUARTERLY" => "Quarterly",
                "MONTHLY" => "Monthly",
                _ => null
            };

            _logger.LogInformation("File type suffix determined: {Type}", type ?? "null (will use full path)");

            // ====================================================================
            // Build final configuration object
            // ====================================================================
            PDPipelineConfiguration config = new()
            {
                TimeConfig = timeConfig,
                DatePassedDueBuckets = datePassedDueBuckets,
                PDConfigurations = pdConfigurations,
                QuarterEndedDates = quarterEndedDates,
                FinalBucketPayload = finalBucketPayload,
                Type = type
            };

            await _publisher.PublishProgress(_sessionId, 1, 3, PDProgressStatus.Completed, cancellationToken: cancellationToken);
            _logger.LogInformation("Pipeline configuration built successfully");
            return Result.Success(config);
        }
        catch (Exception ex)
        {
            await _publisher.PublishProgress(_sessionId, 1, 1, PDProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 1, 2, PDProgressStatus.Failed, cancellationToken: cancellationToken);
            await _publisher.PublishProgress(_sessionId, 1, 3, PDProgressStatus.Failed, cancellationToken: cancellationToken);
            _logger.LogError(ex, "Error fetching pipeline configuration from database: {ErrorMessage}", ex.Message);
                return Result.Failure<PDPipelineConfiguration>(Error.Failure(
                    "PDPipeline.ConfigurationFetchFailed",
                    $"Failed to fetch pipeline configuration: {ex.Message}"));
            }
        }

    /// <summary>
    /// Saves the PD algorithm result to the database as JSONB
    /// </summary>
    /// <param name="pdExtrapolationResult">The PD extrapolation result to save</param>
    /// <param name="createdBy">User who initiated the calculation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task SavePDAlgorithmResultAsync(
        PdExtrapolationResultDto pdExtrapolationResult,
        string createdBy,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("PHASE 6: Saving PD Algorithm Result to database...");
            var saveStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Parse createdBy to Guid (assuming it's a user ID string representation)
            if (!Guid.TryParse(createdBy, out Guid createdByUserId))
            {
                _logger.LogWarning("CreatedBy value '{CreatedBy}' is not a valid GUID, using default Guid.Empty", createdBy);
                createdByUserId = Guid.Empty;
            }

            // Check if there are existing records and delete them
            bool hasExistingRecords = await _dbContext.PDAlgorithmResults.AnyAsync(cancellationToken);
            
            if (hasExistingRecords)
            {
                _logger.LogInformation("Existing PD Algorithm Results found. Deleting all existing records...");
     
                // Get all existing records
                List<PDAlgorithmResult> existingResults = await _dbContext.PDAlgorithmResults
                    .ToListAsync(cancellationToken);
     
                int existingCount = existingResults.Count;
                
                // Remove all existing records
                _dbContext.PDAlgorithmResults.RemoveRange(existingResults);
                await _dbContext.SaveChangesAsync(cancellationToken);
              
                _logger.LogInformation("Deleted {Count} existing PD Algorithm Result records", existingCount);
            }
            else
            {
                _logger.LogInformation("No existing PD Algorithm Results found");
            }

            // Serialize the PD extrapolation result to JSON
            string pdAlgorithmJson = JsonSerializer.Serialize(pdExtrapolationResult, JsonSerializeOptions);

            // Create the PD Algorithm Result entity
            var pdAlgorithmResult = PDAlgorithmResult.Create(
                pdAlgorithmJson,
                createdByUserId
            );

            // Save to database
            _dbContext.PDAlgorithmResults.Add(pdAlgorithmResult);
            await _dbContext.SaveChangesAsync(cancellationToken);

            saveStopwatch.Stop();
            _logger.LogInformation(
                "PD Algorithm Result saved successfully in {ElapsedMs}ms. ID: {Id}, ProductCategories: {Count}, CreatedBy: {UserId}",
                saveStopwatch.ElapsedMilliseconds,
                pdAlgorithmResult.Id,
                pdExtrapolationResult.ProductCategories.Count,
            createdByUserId);
    }
    catch (Exception ex)
    {
            // Log error but don't fail the pipeline - result is still returned to caller
            _logger.LogError(ex, "Failed to save PD Algorithm Result to database: {ErrorMessage}. Pipeline will continue.", ex.Message);
    }
}

    /// <summary>
    /// Converts PeriodMigrationMatrix collection to MigrationMatricesAndSummaryResponse
    /// </summary>
    private static SummaryModels.MigrationMatricesAndSummaryResponse ConvertToMigrationMatricesResponse(
            IReadOnlyList<PeriodMigrationMatrix> periodMigrationMatrices)
        {
            MigrationMatricesAndSummaryResponse response = new()
            {
                Success = true,
                Message = "Migration matrices generated successfully",
                Timestamp = DateTime.UtcNow,
                Matrices = periodMigrationMatrices.Select(pm => new Application.Models.PDSummary.MigrationMatrix
                {
                    PeriodNMinus1 = pm.PeriodNMinus1,
                    PeriodN = pm.PeriodN,
                    ProductCategories = pm.ProductCategories.Select(pc => new ProductCategory
                    {
                        ProductCategoryName = pc.ProductCategoryName,
                        Segments = pc.Segments.Select(seg => new Application.Models.PDSummary.Segment
                        {
                            SegmentName = seg.SegmentName,
                            ComparisonPeriod = seg.ComparisonPeriod,
                            Buckets = seg.Buckets,
                            BucketCount = seg.BucketCount,
                            HasPercentageData = seg.HasPercentageData,
                            Counts = new MatrixCounts
                            {
                                Matrix = seg.Counts.Matrix.Select(row => row.ToList()).ToList(),
                                ExitCounts = seg.Counts.ExitCounts.ToList(),
                                RowTotals = seg.Counts.RowTotals.ToList()
                            },
                            Percentages = seg.Percentages is not null ? new MatrixPercentages
                            {
                                Matrix = seg.Percentages.Matrix.Select(row => row.ToList()).ToList(),
                                ExitPercentages = seg.Percentages.ExitPercentages.ToList(),
                                GrandTotal = seg.Percentages.GrandTotal.Select(d => (double?)d).ToList()
                            } : null
                        }).ToList()
                    }).ToList()
                }).ToList()
            };

            return response;
        }

    /// <summary>
    /// Converts DatePassedDueBasedConfiguration collection to DatePassedDueBucket list
    /// </summary>
    /// <param name="configurations">Collection of date passed due based configurations from database</param>
    /// <returns>List of DatePassedDueBucket objects</returns>
    private static List<DatePassedDueBucket> ConvertToDatePassedDueBuckets(
        ICollection<DatePassedDueBasedConfiguration> configurations)
    {
        return configurations
            .Select(config => new DatePassedDueBucket(
                Id: config.Id ?? string.Empty,
                RangeStart: config.RangeStart,
                RangeEnd: config.RangeEnd,
                BucketLabel: config.BucketLabel,
                StageMapping: config.StageMapping,
                RangeEndError: config.RangeEndError ?? string.Empty,
                HasBeenTouched: config.HasBeenTouched))
            .ToList();
  }

        /// <summary>
        /// Internal configuration model for pipeline execution
        /// </summary>
        private sealed class PDPipelineConfiguration
        {
            public TimeConfig TimeConfig { get; init; } = null!;
            public List<DatePassedDueBucket> DatePassedDueBuckets { get; init; } = null!;
            public List<PDConfiguration> PDConfigurations { get; init; } = null!;
            public Dictionary<string, DateTime> QuarterEndedDates { get; init; } = null!;
            public FinalBucketPayload FinalBucketPayload { get; init; } = null!;
            public string? Type { get; init; }
        }
    }
