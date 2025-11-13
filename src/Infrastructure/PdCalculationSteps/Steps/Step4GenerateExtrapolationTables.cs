using Application.Abstractions.Data;
using Application.DTOs.PD;
using Application.Services;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Infrastructure.PDCalculationSteps.Steps;

/// <summary>
/// Step 4 of PD Calculation: Generate PD extrapolation tables for all three methods
/// </summary>
internal sealed class Step4GenerateExtrapolationTables
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPDSetupConfigurationService _pdSetupConfigurationService;
    private readonly ILogger<Step4GenerateExtrapolationTables> _logger;

    public Step4GenerateExtrapolationTables(
    IApplicationDbContext dbContext,
        IPDSetupConfigurationService pdSetupConfigurationService,
        ILogger<Step4GenerateExtrapolationTables> logger)
    {
        _dbContext = dbContext;
        _pdSetupConfigurationService = pdSetupConfigurationService;
        _logger = logger;
    }

    /// <summary>
    /// Executes Step 4: generates PD extrapolation tables for all product categories and segments
    /// </summary>
    /// <param name="createdBy">User who initiated the process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the PD extrapolation results with all three methods</returns>
    public Task<Result<PdExtrapolationResultDto>> ExecuteAsync(
        AveragePDTablesResponse? step3Data,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing Step 4: PD extrapolation table generation");

            MacroEconomicFactorAdjustmentDto MacroEconomicFactorAdjustmentDto = GetMacroEconomicFactorAdjustment();
            _ = MacroEconomicFactorAdjustmentDto;
            _ = _dbContext;
            _ = _pdSetupConfigurationService;

            PdExtrapolationResultDto PdExtrapolationResultDto = new();
            _ = PdExtrapolationResultDto;

            foreach (KeyValuePair<string, Dictionary<string, SegmentAveragePDTable>> avgPdTable in step3Data?.AveragePDTables ?? [])
            {
                string productCategory = avgPdTable.Key;

                PdExtrapolationCategoryDto ProductCategoryDto = new()
                {
                    ProductCategory = productCategory,
                    Segments = new List<PdExtrapolationSegmentDto>()
                };

                foreach (KeyValuePair<string, SegmentAveragePDTable> segmentTable in avgPdTable.Value)
                {
                    string segment = segmentTable.Key;
                    SegmentAveragePDTable segmentAveragePDTable = segmentTable.Value;
                    _logger.LogInformation("Processing Product Category: {ProductCategory}, Segment: {Segment}",
                                           productCategory,
                                           segment);

                    PdExtrapolationMethod1Dto method1Result = GenerateGeometricApproach(MacroEconomicFactorAdjustmentDto, segmentAveragePDTable);
                    PdExtrapolationMethod2Dto method2Result = GenerateGeometricAndLognormalApproach(MacroEconomicFactorAdjustmentDto, segmentAveragePDTable);
                    PdExtrapolationMethod3Dto method3Result = GenerateSurvivalRateApproach(MacroEconomicFactorAdjustmentDto, segmentAveragePDTable);

                    PdExtrapolationSegmentDto segmentDto = new()
                    {
                        Segment = segment,
                        Summary = new PdExtrapolationSummaryDto
                        {
                            Method1 = method1Result,
                            Method2 = method2Result,
                            Method3 = method3Result
                        }
                    };
                    ProductCategoryDto.Segments.Add(segmentDto);
                }
                PdExtrapolationResultDto.ProductCategories.Add(ProductCategoryDto);
            }

            _logger.LogInformation("Step 4 completed successfully");

            return Task.FromResult(Result.Success(PdExtrapolationResultDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Step 4 execution: {ErrorMessage}", ex.Message);
            return Task.FromResult(Result.Failure<PdExtrapolationResultDto>(Error.Failure(
               "PDCalculation.Step4.UnexpectedError",
                  $"An unexpected error occurred during Step 4 execution: {ex.Message}")));
        }
    }

    /// <summary>
    /// Generates PD extrapolation using the Geometric Approach (Method 1)
    /// </summary>
    /// <param name="segmentAveragePDTable">The segment average PD table containing maturity data</param>
    /// <returns>PD extrapolation result using geometric approach</returns>
    private PdExtrapolationMethod1Dto GenerateGeometricApproach(MacroEconomicFactorAdjustmentDto MacroEconomicFactorAdjustmentDto, SegmentAveragePDTable segmentAveragePDTable)
    {
        PdExtrapolationTableDto extrapolatedCumulativePdsBeforeEfa = GenerateExtrapolatedCumulativePDsBeforeAdjustingEFA(segmentAveragePDTable);
        PdExtrapolationTableDto extrapolatedCumulativePdsAfterEfa = GenerateExtrapolatedCumulativePDsAfterEfaAdjustment(MacroEconomicFactorAdjustmentDto, extrapolatedCumulativePdsBeforeEfa);
        PdExtrapolationTableDto marginalPdsAfterEfa = GenerateMarginalPdsAfterEfaAdjustment(extrapolatedCumulativePdsAfterEfa);

        PdExtrapolationMethod1Dto result = new()
        {
            ExtrapolatedCumulativePdsBeforeEfa = extrapolatedCumulativePdsBeforeEfa,
            ExtrapolatedCumulativePdsAfterEfa = extrapolatedCumulativePdsAfterEfa,
            MarginalPdsAfterEfa = marginalPdsAfterEfa
        };

        return result;
    }

    /// <summary>
    /// Generates PD extrapolation using the Geometric and Lognormal Approach (Method 2)
    /// </summary>
    /// <param name="segmentAveragePDTable">The segment average PD table containing maturity data</param>
    /// <returns>PD extrapolation result using geometric and lognormal approach</returns>
    private PdExtrapolationMethod2Dto GenerateGeometricAndLognormalApproach(MacroEconomicFactorAdjustmentDto MacroEconomicFactorAdjustmentDto, SegmentAveragePDTable segmentAveragePDTable)
    {
        PdExtrapolationTableDto extrapolatedCumulativePdsBeforeEfa = GenerateExtrapolatedCumulativePDsBeforeAdjustingEFA_Method2(segmentAveragePDTable);
        PdExtrapolationTableDto extrapolatedCumulativePdsAfterEfa = GenerateExtrapolatedCumulativePDsAfterEfaAdjustment_Method2(MacroEconomicFactorAdjustmentDto, extrapolatedCumulativePdsBeforeEfa);
        PdExtrapolationTableDto marginalPdsAfterEfa = GenerateMarginalPdsAfterEfaAdjustment_Method2(extrapolatedCumulativePdsAfterEfa);

        PdExtrapolationMethod2Dto result = new()
        {
            ExtrapolatedCumulativePdsBeforeEfa = extrapolatedCumulativePdsBeforeEfa,
            ExtrapolatedCumulativePdsAfterEfa = extrapolatedCumulativePdsAfterEfa,
            MarginalPdsAfterEfa = marginalPdsAfterEfa,
        };

        return result;
    }

    /// <summary>
    /// Generates PD extrapolation using the Survival Rate Approach (Method 3)
    /// </summary>
    /// <param name="segmentAveragePDTable">The segment average PD table containing maturity data</param>
    /// <returns>PD extrapolation result using survival rate approach</returns>
    private PdExtrapolationMethod3Dto GenerateSurvivalRateApproach(MacroEconomicFactorAdjustmentDto MacroEconomicFactorAdjustmentDto, SegmentAveragePDTable segmentAveragePDTable)
    {
        PdExtrapolationTableDto efaAdjustedPds = GenerateEfaAdjustedPds_Method3(segmentAveragePDTable, MacroEconomicFactorAdjustmentDto);
        PdExtrapolationTableDto survivalRates = GenerateSurvivalRates_Method3(efaAdjustedPds);
        PdExtrapolationTableDto marginalPds = GenerateMarginalPds_Method3(efaAdjustedPds, survivalRates);

        PdExtrapolationMethod3Dto result = new()
        {
            EfaAdjustedPds = efaAdjustedPds,
            SurvivalRates = survivalRates,
            MarginalPds = marginalPds
        };

        return result;
    }

    #region Extrapolation table generation methods

    /// <summary>
    /// Generates extrapolated cumulative PDs before adjusting for Economic Factor Adjustment (EFA)
    /// </summary>
    /// <param name="segmentAveragePDTable">The segment average PD table containing maturity data</param>
    /// <returns>PD extrapolation table with cumulative PDs before EFA adjustment</returns>
    private PdExtrapolationTableDto GenerateExtrapolatedCumulativePDsBeforeAdjustingEFA(SegmentAveragePDTable segmentAveragePDTable)
    {
        var result = new PdExtrapolationTableDto
        {
            Title = "Extrapolated cumulative PDs before adjusting EFA",
            Rows = new List<PdExtrapolationRowDto>()
        };

        int maxMaturity = segmentAveragePDTable.HighestMaturity;
        int bucketCount = segmentAveragePDTable.BucketCount;
        int currentBucket = 1;

        foreach (AveragePDRow row in segmentAveragePDTable.Rows)
        {
            string ageBucket = row.AgeBucket;
            decimal interpolatedPd = (decimal)row.InterpolatedPD;

            var pdRow = new PdExtrapolationRowDto
            {
                AgeBucket = ageBucket,
                PdValuesByYear = new Dictionary<int, decimal?>()
            };

            // Year 1 → base interpolated PD
            pdRow.PdValuesByYear[1] = interpolatedPd;

            for (int year = 2; year <= maxMaturity; year++)
            {
                if (currentBucket == 1 || currentBucket == 2)
                {
                    // For the first two buckets, extrapolation not applied (set as null)
                    pdRow.PdValuesByYear.Add(year, null);
                    continue;
                }
                else if (currentBucket == bucketCount)
                {
                    // For the last bucket, set PD to 100%
                    pdRow.PdValuesByYear.Add(year, 100m);
                    continue;
                }
                else
                {
                    decimal? basePd = pdRow.PdValuesByYear[1] / 100m;
                    if (basePd is not null)
                    {
                        decimal extrapolatedProb = 1 - (decimal)Math.Pow((double)(1 - basePd.Value), year);
                        decimal extrapolatedPd = extrapolatedProb * 100m;
                        pdRow.PdValuesByYear[year] = extrapolatedPd;
                    }
                    else
                    {
                        pdRow.PdValuesByYear[year] = null;
                    }
                }
            }
            currentBucket++;
            result.Rows.Add(pdRow);
        }
        return result;
    }

    /// <summary>
    /// Generates extrapolated cumulative PDs before adjusting for Economic Factor Adjustment (EFA) using Method 2 (Geometric and Lognormal Approach)
    /// </summary>
    /// <param name="segmentAveragePDTable">The segment average PD table containing maturity data</param>
    /// <returns>PD extrapolation table with cumulative PDs before EFA adjustment for Method 2</returns>
    private PdExtrapolationTableDto GenerateExtrapolatedCumulativePDsBeforeAdjustingEFA_Method2(
        SegmentAveragePDTable segmentAveragePDTable)
    {
        PdExtrapolationTableDto result = new()
        {
            Title = "Extrapolated cumulative PDs before adjusting EFA (Method 2 - Geometric and Lognormal)",
            Rows = new List<PdExtrapolationRowDto>()
        };

        int maxMaturity = segmentAveragePDTable.HighestMaturity;
        int bucketCount = segmentAveragePDTable.BucketCount;
        int currentBucket = 1;

        foreach (AveragePDRow row in segmentAveragePDTable.Rows)
        {
            string ageBucket = row.AgeBucket;
            decimal interpolatedPd = (decimal)row.InterpolatedPD;

            PdExtrapolationRowDto pdRow = new()
            {
                AgeBucket = ageBucket,
                PdValuesByYear = new Dictionary<int, decimal?>()
            };

            // Year 1 → base interpolated PD
            pdRow.PdValuesByYear[1] = interpolatedPd;

            for (int year = 2; year <= maxMaturity; year++)
            {
                if (currentBucket == 1 || currentBucket == 2)
                {
                    // For the first two buckets, extrapolation not applied (set as null)
                    pdRow.PdValuesByYear.Add(year, null);
                    continue;
                }
                else if (currentBucket == bucketCount)
                {
                    // For the last bucket, set PD to 100%
                    pdRow.PdValuesByYear.Add(year, 100m);
                    continue;
                }
                else
                {
                    decimal? basePd = pdRow.PdValuesByYear[1] / 100m;
                    if (basePd is not null)
                    {
                        // Apply lognormal transformation for later years
                        // For years > threshold (e.g., 5), use lognormal distribution
                        // Otherwise use geometric approach similar to Method 1
                        int lognormalThreshold = 5;
                        
                        if (year <= lognormalThreshold)
                        {
                            // Geometric approach
                            decimal extrapolatedProb = 1 - (decimal)Math.Pow((double)(1 - basePd.Value), year);
                            decimal extrapolatedPd = extrapolatedProb * 100m;
                            pdRow.PdValuesByYear[year] = extrapolatedPd;
                        }
                        else
                        {
                            // Lognormal approach for long-term extrapolation
                            // Uses logarithmic scaling to provide smoother curve
                            double logScale = Math.Log(year) / Math.Log(lognormalThreshold);
                            decimal baseProbability = (decimal)Math.Pow((double)basePd.Value, 1.0 / lognormalThreshold);
                            decimal extrapolatedProb = 1 - (decimal)Math.Pow((double)(1 - baseProbability), year * logScale);
                            decimal extrapolatedPd = extrapolatedProb * 100m;
                            pdRow.PdValuesByYear[year] = Math.Min(extrapolatedPd, 100m);
                        }
                    }
                    else
                    {
                        pdRow.PdValuesByYear[year] = null;
                    }
                }
            }
            currentBucket++;
            result.Rows.Add(pdRow);
        }
        
        return result;
    }

    /// <summary>
    /// Generates extrapolated cumulative PDs after adjusting for Economic Factor Adjustment (EFA)
    /// </summary>
    /// <param name="macroEconomicFactorAdjustment">The macro-economic factor adjustment data containing EFA values</param>
    /// <param name="extrapolatedCumulativePdsBeforeEfa">The extrapolated cumulative PDs before EFA adjustment</param>
    /// <param name="highestMaturity">The highest maturity year for column headers</param>
    /// <returns>PD extrapolation table with cumulative PDs after EFA adjustment</returns>
    private PdExtrapolationTableDto GenerateExtrapolatedCumulativePDsAfterEfaAdjustment(
        MacroEconomicFactorAdjustmentDto macroEconomicFactorAdjustment,
        PdExtrapolationTableDto extrapolatedCumulativePdsBeforeEfa)
    {
        PdExtrapolationTableDto result = new()
        {
            Title = "Extrapolated cumulative PDs after EFA adjustment",
            Rows = new List<PdExtrapolationRowDto>()
        };

        // Build EFA lookup: Year -> EFA percentage
        var efaLookup = macroEconomicFactorAdjustment.EfaValues
            .ToDictionary(efa => efa.Year, efa => efa.EfaPercentage);

        // Determine the EFA value for year 5 (to be used for years beyond 5)
        decimal efaYear5 = efaLookup.TryGetValue(5, out decimal year5Value) ? year5Value : 100m;

        foreach (PdExtrapolationRowDto beforeEfaRow in extrapolatedCumulativePdsBeforeEfa.Rows)
        {
            PdExtrapolationRowDto afterEfaRow = new()
            {
                AgeBucket = beforeEfaRow.AgeBucket,
                PdValuesByYear = new Dictionary<int, decimal?>()
            };

            foreach (KeyValuePair<int, decimal?> yearPd in beforeEfaRow.PdValuesByYear)
            {
                int year = yearPd.Key;
                decimal? pdValue = yearPd.Value;

                // Preserve nulls
                if (pdValue is null)
                {
                    afterEfaRow.PdValuesByYear[year] = null;
                    continue;
                }

                // Get EFA percentage for this year
                decimal efaPercentage;
                if (efaLookup.TryGetValue(year, out decimal yearEfaValue))
                {
                    efaPercentage = yearEfaValue;
                }
                else
                {
                    // For years beyond 5, use year 5's EFA
                    efaPercentage = efaYear5;
                }

                // Apply formula: MIN(PD_beforeEFA * EFA%, 100%)
                // PD values are already in percentage form (e.g., 13.47 means 13.47%)
                decimal adjustedPd = pdValue.Value * (efaPercentage / 100m);
                
                // Cap at 100%
                adjustedPd = Math.Min(adjustedPd, 100m);

                afterEfaRow.PdValuesByYear[year] = adjustedPd;
            }

            result.Rows.Add(afterEfaRow);
        }

        return result;
    }

    /// <summary>
    /// Generates marginal PDs after EFA adjustment by calculating differences between consecutive years
    /// </summary>
    /// <param name="extrapolatedCumulativePdsAfterEfa">The extrapolated cumulative PDs after EFA adjustment</param>
    /// <returns>PD extrapolation table with marginal PDs after EFA adjustment</returns>
    private PdExtrapolationTableDto GenerateMarginalPdsAfterEfaAdjustment(
        PdExtrapolationTableDto extrapolatedCumulativePdsAfterEfa)
    {
        PdExtrapolationTableDto result = new()
        {
            Title = "Marginal PDs after EFA adjustment",
            Rows = new List<PdExtrapolationRowDto>()
        };

        foreach (PdExtrapolationRowDto cumulativeRow in extrapolatedCumulativePdsAfterEfa.Rows)
        {
            PdExtrapolationRowDto marginalRow = new()
            {
                AgeBucket = cumulativeRow.AgeBucket,
                PdValuesByYear = new Dictionary<int, decimal?>()
            };

            // Get all years sorted
            var years = cumulativeRow.PdValuesByYear.Keys.OrderBy(y => y).ToList();

            foreach (int year in years)
            {
                if (year == 1)
                {
                    // Year 1 marginal PD equals Year 1 cumulative PD
                    marginalRow.PdValuesByYear[year] = cumulativeRow.PdValuesByYear[year];
                }
                else
                {
                    // Get current year and previous year cumulative PDs
                    decimal? currentYearPd = cumulativeRow.PdValuesByYear[year];
                    decimal? previousYearPd = cumulativeRow.PdValuesByYear.TryGetValue(year - 1, out decimal? prevPd) 
                        ? prevPd 
                        : null;

                    // If either value is null, result is null
                    if (currentYearPd is null || previousYearPd is null)
                    {
                        marginalRow.PdValuesByYear[year] = null;
                    }
                    else
                    {
                        // Calculate marginal PD: current - previous
                        decimal marginalPd = currentYearPd.Value - previousYearPd.Value;
                        
                        // Clamp negative values to 0
                        marginalPd = Math.Max(marginalPd, 0m);
                        
                        marginalRow.PdValuesByYear[year] = marginalPd;
                    }
                }
            }

            result.Rows.Add(marginalRow);
        }

        return result;
    }

    /// <summary>
    /// Generates extrapolated cumulative PDs after adjusting for Economic Factor Adjustment (EFA) using Method 2 (Geometric and Lognormal Approach)
    /// </summary>
    /// <param name="macroEconomicFactorAdjustment">The macro-economic factor adjustment data containing EFA values</param>
    /// <param name="extrapolatedCumulativePdsBeforeEfa">The extrapolated cumulative PDs before EFA adjustment from Method 2</param>
    /// <returns>PD extrapolation table with cumulative PDs after EFA adjustment for Method 2</returns>
    private PdExtrapolationTableDto GenerateExtrapolatedCumulativePDsAfterEfaAdjustment_Method2(
        MacroEconomicFactorAdjustmentDto macroEconomicFactorAdjustment,
        PdExtrapolationTableDto extrapolatedCumulativePdsBeforeEfa)
    {
        PdExtrapolationTableDto result = new()
        {
            Title = "Extrapolated cumulative PDs after EFA adjustment (Method 2 - Geometric & Lognormal)",
            Rows = new List<PdExtrapolationRowDto>()
        };

        // Build EFA lookup: Year -> EFA percentage
        var efaLookup = macroEconomicFactorAdjustment.EfaValues
            .ToDictionary(efa => efa.Year, efa => efa.EfaPercentage);

        // Determine the last defined EFA value (typically year 5) to be used for years beyond
        int lastEfaYear = macroEconomicFactorAdjustment.EfaValues.Max(efa => efa.Year);
        decimal lastEfa = efaLookup[lastEfaYear];

        foreach (PdExtrapolationRowDto beforeEfaRow in extrapolatedCumulativePdsBeforeEfa.Rows)
        {
            PdExtrapolationRowDto afterEfaRow = new()
            {
                AgeBucket = beforeEfaRow.AgeBucket,
                PdValuesByYear = new Dictionary<int, decimal?>()
            };

            foreach (KeyValuePair<int, decimal?> yearPd in beforeEfaRow.PdValuesByYear)
            {
                int year = yearPd.Key;
                decimal? pdValue = yearPd.Value;

                // Preserve nulls
                if (pdValue is null)
                {
                    afterEfaRow.PdValuesByYear[year] = null;
                    continue;
                }

                // Get EFA percentage for this year, or use last EFA if beyond defined range
                decimal efaPercentage = efaLookup.TryGetValue(year, out decimal yearEfaValue)
                    ? yearEfaValue
                    : lastEfa;

                // Apply formula: MIN(PD_beforeEFA * EFA%, 100%)
                // PD values are already in percentage form (e.g., 17.80 means 17.80%)
                decimal adjustedPd = pdValue.Value * (efaPercentage / 100m);

                // Cap at 100%
                adjustedPd = Math.Min(adjustedPd, 100m);

                afterEfaRow.PdValuesByYear[year] = adjustedPd;
            }

            result.Rows.Add(afterEfaRow);
        }

        return result;
    }

    /// <summary>
    /// Generates marginal PDs after EFA adjustment for Method 2 by calculating differences between consecutive years
    /// </summary>
    /// <param name="extrapolatedCumulativePdsAfterEfa">The extrapolated cumulative PDs after EFA adjustment from Method 2</param>
    /// <returns>PD extrapolation table with marginal PDs after EFA adjustment for Method 2</returns>
    private PdExtrapolationTableDto GenerateMarginalPdsAfterEfaAdjustment_Method2(
        PdExtrapolationTableDto extrapolatedCumulativePdsAfterEfa)
    {
        PdExtrapolationTableDto result = new()
        {
            Title = "Marginal PDs after EFA adjustment (Method 2 - Geometric & Lognormal)",
            Rows = new List<PdExtrapolationRowDto>()
        };

        foreach (PdExtrapolationRowDto cumulativeRow in extrapolatedCumulativePdsAfterEfa.Rows)
        {
            PdExtrapolationRowDto marginalRow = new()
            {
                AgeBucket = cumulativeRow.AgeBucket,
                PdValuesByYear = new Dictionary<int, decimal?>()
            };

            // Get all years sorted
            var years = cumulativeRow.PdValuesByYear.Keys.OrderBy(y => y).ToList();

            foreach (int year in years)
            {
                if (year == 1)
                {
                    // Year 1 marginal PD equals Year 1 cumulative PD
                    marginalRow.PdValuesByYear[year] = cumulativeRow.PdValuesByYear[year];
                }
                else
                {
                    // Get current year and previous year cumulative PDs
                    decimal? currentYearPd = cumulativeRow.PdValuesByYear[year];
                    decimal? previousYearPd = cumulativeRow.PdValuesByYear.TryGetValue(year - 1, out decimal? prevPd)
                        ? prevPd
                        : null;

                    // If either value is null, result is null
                    if (currentYearPd is null || previousYearPd is null)
                    {
                        marginalRow.PdValuesByYear[year] = null;
                    }
                    else
                    {
                        // Calculate marginal PD: current - previous
                        decimal marginalValue = currentYearPd.Value - previousYearPd.Value;

                        // Clamp negative values to 0
                        marginalValue = Math.Max(marginalValue, 0m);

                        // Cap at 100% (though marginal should typically be much lower)
                        marginalValue = Math.Min(marginalValue, 100m);

                        marginalRow.PdValuesByYear[year] = marginalValue;
                    }
                }
            }

            result.Rows.Add(marginalRow);
        }

        return result;
    }

    /// <summary>
    /// Generates EFA adjusted PDs for Method 3 (Survival Rate Approach)
    /// Formula: Year 1 = MIN(InterpolatedPD * EFA₁, 100%), Year t = (PD₁ / EFA₁) * EFA_t
    /// </summary>
    /// <param name="segmentAveragePDTable">The segment average PD table containing interpolated PD data</param>
    /// <param name="macroEconomicFactorAdjustment">The macro-economic factor adjustment data containing EFA values</param>
    /// <returns>PD extrapolation table with EFA adjusted PDs for Method 3</returns>
    private PdExtrapolationTableDto GenerateEfaAdjustedPds_Method3(
        SegmentAveragePDTable segmentAveragePDTable,
        MacroEconomicFactorAdjustmentDto macroEconomicFactorAdjustment)
    {
        if (macroEconomicFactorAdjustment?.EfaValues == null || !macroEconomicFactorAdjustment.EfaValues.Any())
        {
            throw new InvalidOperationException("EFA values are required for Method 3 PD extrapolation");
        }

        PdExtrapolationTableDto result = new()
        {
            Title = "EFA adjusted PDs (Method 3 - Survival Rate Approach)",
            Rows = new List<PdExtrapolationRowDto>()
        };

        // Build EFA lookup: Year -> EFA percentage
        var efaLookup = macroEconomicFactorAdjustment.EfaValues
            .ToDictionary(efa => efa.Year, efa => efa.EfaPercentage);

        // Get EFA for year 1 (critical for all calculations)
        if (!efaLookup.TryGetValue(1, out decimal efa1))
        {
            throw new InvalidOperationException("EFA value for Year 1 is required for Method 3 calculations");
        }

        // Determine last available EFA year and value (for years beyond defined range)
        int lastEfaYear = macroEconomicFactorAdjustment.EfaValues.Max(efa => efa.Year);
        decimal fallbackEfa = efaLookup[lastEfaYear];

        int maxMaturity = segmentAveragePDTable.HighestMaturity;
        int bucketCount = segmentAveragePDTable.BucketCount;
        int currentBucket = 1;

        // Guard against division by zero
        if (efa1 == 0m)
        {
            throw new InvalidOperationException("EFA for Year 1 cannot be zero");
        }

        foreach (AveragePDRow row in segmentAveragePDTable.Rows)
        {
            string ageBucket = row.AgeBucket;
            decimal interpolatedPd = (decimal)row.InterpolatedPD;

            PdExtrapolationRowDto pdRow = new()
            {
                AgeBucket = ageBucket,
                PdValuesByYear = new Dictionary<int, decimal?>()
            };

            // Special handling for first two buckets (Current and 1-30 days)
            if (currentBucket == 1 || currentBucket == 2)
            {
                // For first two buckets, set all years to null (extrapolation not applied)
                for (int year = 1; year <= maxMaturity; year++)
                {
                    pdRow.PdValuesByYear[year] = null;
                }
            }
            // Special handling for last bucket (Above 90 days / Default bucket)
            else if (currentBucket == bucketCount)
            {
                // Last bucket remains 100% for all years
                for (int year = 1; year <= maxMaturity; year++)
                {
                    pdRow.PdValuesByYear[year] = 100m;
                }
            }
            else
            {
                // Standard buckets: apply Method 3 EFA adjustment logic

                // Year 1: Apply EFA adjustment to interpolated PD
                // Formula: MIN(InterpolatedPD * (EFA₁ / 100), 100%)
                decimal year1Adjusted = Math.Min(interpolatedPd * (efa1 / 100m), 100m);
                pdRow.PdValuesByYear[1] = year1Adjusted;

                // Years 2 to maxMaturity: Scale Year 1 by ratio of EFA_t to EFA_1
                // Formula: PD_t = (PD₁ / EFA₁) * EFA_t
                for (int year = 2; year <= maxMaturity; year++)
                {
                    // Get EFA for current year, or use fallback for years beyond defined range
                    decimal efaT = efaLookup.TryGetValue(year, out decimal yearEfaValue)
                        ? yearEfaValue
                        : fallbackEfa;

                    // Calculate adjusted PD: (Year1Adjusted / EFA₁) * EFA_t
                    decimal adjustedPd = year1Adjusted / efa1 * efaT;

                    // Cap at 100%
                    adjustedPd = Math.Min(adjustedPd, 100m);

                    pdRow.PdValuesByYear[year] = adjustedPd;
                }
            }

            currentBucket++;
            result.Rows.Add(pdRow);
        }

        return result;
    }

    /// <summary>
    /// Generates survival rates for Method 3 (Survival Rate Approach)
    /// Formula: Year 1 = 100%, Year t = 100 - PD_t
    /// </summary>
    /// <param name="efaAdjustedPds">The EFA adjusted PDs table from Method 3</param>
    /// <returns>PD extrapolation table with survival rates for Method 3</returns>
    private PdExtrapolationTableDto GenerateSurvivalRates_Method3(PdExtrapolationTableDto efaAdjustedPds)
    {
        PdExtrapolationTableDto result = new()
        {
            Title = "Survival rates (Method 3 - Survival Rate Approach)",
            Rows = new List<PdExtrapolationRowDto>()
        };

        foreach (PdExtrapolationRowDto efaRow in efaAdjustedPds.Rows)
        {
            PdExtrapolationRowDto survivalRow = new()
            {
                AgeBucket = efaRow.AgeBucket,
                PdValuesByYear = new Dictionary<int, decimal?>()
            };

            // Get all years sorted
            var years = efaRow.PdValuesByYear.Keys.OrderBy(y => y).ToList();

            foreach (int year in years)
            {
                decimal? pdValue = efaRow.PdValuesByYear[year];

                if (year == 1)
                {
                    // Year 1 always equals 100% for all buckets
                    survivalRow.PdValuesByYear[year] = 100m;
                }
                else
                {
                    // Preserve nulls from EFA adjusted PDs
                    if (pdValue is null)
                    {
                        survivalRow.PdValuesByYear[year] = null;
                        continue;
                    }

                    // For "Above 90 days" bucket (PD = 100%), survival rate = 0%
                    // Formula: SurvivalRate = 100 - PD
                    decimal survivalRate = 100m - pdValue.Value;

                    // Clamp to valid percentage range [0%, 100%]
                    survivalRate = Math.Max(0m, Math.Min(100m, survivalRate));

                    survivalRow.PdValuesByYear[year] = survivalRate;
                }
            }

            result.Rows.Add(survivalRow);
        }

        return result;
    }

    /// <summary>
    /// Generates marginal PDs for Method 3 (Survival Rate Approach)
    /// Formula: Year 1 = EFA_Adjusted_PD[1], Year t = EFA_Adjusted_PD[t] * PRODUCT(SurvivalRate[1..t-1])
    /// </summary>
    /// <param name="efaAdjustedPds">The EFA adjusted PDs table from Method 3</param>
    /// <param name="survivalRates">The survival rates table from Method 3</param>
    /// <returns>PD extrapolation table with marginal PDs for Method 3</returns>
    private PdExtrapolationTableDto GenerateMarginalPds_Method3(
        PdExtrapolationTableDto efaAdjustedPds,
        PdExtrapolationTableDto survivalRates)
    {
        PdExtrapolationTableDto result = new()
        {
            Title = "Marginal PDs (Method 3 - Survival Rate Approach)",
            Rows = new List<PdExtrapolationRowDto>()
        };

        // Build a lookup for survival rates by age bucket for efficient access
        var survivalRatesLookup = survivalRates.Rows
            .ToDictionary(row => row.AgeBucket, row => row);

        foreach (PdExtrapolationRowDto efaRow in efaAdjustedPds.Rows)
        {
            PdExtrapolationRowDto marginalRow = new()
            {
                AgeBucket = efaRow.AgeBucket,
                PdValuesByYear = new Dictionary<int, decimal?>()
            };

            // Find corresponding survival rates row
            if (!survivalRatesLookup.TryGetValue(efaRow.AgeBucket, out PdExtrapolationRowDto? survivalRow))
            {
                // If no matching survival rates found, preserve structure with nulls
                foreach (int year in efaRow.PdValuesByYear.Keys)
                {
                    marginalRow.PdValuesByYear[year] = null;
                }
                result.Rows.Add(marginalRow);
                continue;
            }

            // Get all years sorted
            var years = efaRow.PdValuesByYear.Keys.OrderBy(y => y).ToList();

            foreach (int year in years)
            {
                decimal? efaPdValue = efaRow.PdValuesByYear[year];

                if (year == 1)
                {
                    // Year 1: Marginal PD equals EFA Adjusted PD
                    marginalRow.PdValuesByYear[year] = efaPdValue;
                }
                else
                {
                    // Preserve nulls from EFA adjusted PDs
                    if (efaPdValue is null)
                    {
                        marginalRow.PdValuesByYear[year] = null;
                        continue;
                    }

                    // Calculate product of survival rates from year 1 to year-1
                    decimal survivalProduct = 1m;
                    bool hasNullSurvival = false;

                    for (int survivalYear = 1; survivalYear < year; survivalYear++)
                    {
                        if (survivalRow.PdValuesByYear.TryGetValue(survivalYear, out decimal? survivalRate)
                            && survivalRate.HasValue)
                        {
                            // Convert percentage to fraction (divide by 100)
                            survivalProduct *= survivalRate.Value / 100m;
                        }
                        else
                        {
                            hasNullSurvival = true;
                            break;
                        }
                    }

                    // If any survival rate is missing, result is null
                    if (hasNullSurvival)
                    {
                        marginalRow.PdValuesByYear[year] = null;
                        continue;
                    }

                    // Formula: MarginalPD = EFA_Adjusted_PD * PRODUCT(SurvivalRates)
                    // EFA PD is already in percentage form, survival rates converted to fractions
                    decimal marginalPd = efaPdValue.Value * survivalProduct;

                    // Clamp to valid percentage range [0%, 100%]
                    marginalPd = Math.Max(0m, Math.Min(100m, marginalPd));

                    marginalRow.PdValuesByYear[year] = marginalPd;
                }
            }

            result.Rows.Add(marginalRow);
        }

        return result;
    }

    #endregion


    /// <summary>
    /// Gets the macro-economic factor adjustment values for EFA calculations
    /// </summary>
    /// <returns>Macro-economic factor adjustment data with EFA values for years 1-5</returns>
    private MacroEconomicFactorAdjustmentDto GetMacroEconomicFactorAdjustment()
    {
        MacroEconomicFactorAdjustmentDto macroEconomicFactorAdjustment = new()
        {
            EfaValues = new List<EfaYearValueDto>
            {
                new() { Year = 1, EfaPercentage = 132.16m },
                new() { Year = 2, EfaPercentage = 130.67m },
                new() { Year = 3, EfaPercentage = 121.22m },
                new() { Year = 4, EfaPercentage = 116.35m },
                new() { Year = 5, EfaPercentage = 119.57m }
            }
        };

        return macroEconomicFactorAdjustment;
    }    
}
