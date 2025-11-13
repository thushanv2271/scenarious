using System.Text.Json;
using SharedKernel;
using Application.Models.PDSummary;
using Domain.PDCalculation;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Infrastructure.PDCalculationSteps.Steps.Summary;

/// <summary>
/// Step 3 of PD Calculation: Generate PD Summary tables from migration matrices
/// </summary>
public static class Step3PDAvgSummary
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    /// <summary>
    /// Generates PD Summary table from migration matrices data
    /// </summary>
    /// <param name="migrationData">Migration matrices response object</param>
    /// <param name="loanDetails">List of loan details for maturity calculation</param>
    /// <param name="logger">Logger for debugging purposes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the PD summary table in JSON format</returns>
    public static Result<string> GeneratePDSummaryTable(
        MigrationMatricesAndSummaryResponse migrationData,
        List<LoanDetails> loanDetails,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var totalProcessingStopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            logger.LogInformation("Starting Step 3 processing with {MatricesCount} matrices and {LoanDetailsCount} loan details",
                migrationData?.Matrices?.Count ?? 0, loanDetails?.Count ?? 0);

            if (migrationData is null)
            {
                logger.LogError("Migration matrices data is null");
                return Result.Failure<string>(Error.Validation(
                    "PDSummary.InvalidInput",
                    "Migration matrices data cannot be null"));
            }

            if (migrationData.Matrices is null || migrationData.Matrices.Count == 0)
            {
                logger.LogError("No migration matrices found. Matrices is null: {IsNull}, Count: {Count}",
                    migrationData.Matrices is null, migrationData.Matrices?.Count ?? 0);
                return Result.Failure<string>(Error.Validation(
                    "PDSummary.NoMatrices",
                    "No migration matrices found in the provided data"));
            }

            // Generate PD Summary Table with both main table and average PD table
            string bucketValuesJson = CreatePDSummaryTableWithAverage(migrationData, loanDetails ?? new List<LoanDetails>(), logger);

            totalProcessingStopwatch.Stop();
            logger.LogInformation("Step 3 processing completed successfully in {ElapsedMs}ms ({ElapsedSec:F2}s)",
                totalProcessingStopwatch.ElapsedMilliseconds, totalProcessingStopwatch.Elapsed.TotalSeconds);

            return Result.Success(bucketValuesJson);
        }
        catch (Exception ex)
        {
            totalProcessingStopwatch.Stop();
            logger.LogError(ex, "Step 3 processing failed after {ElapsedMs}ms", totalProcessingStopwatch.ElapsedMilliseconds);
            return Result.Failure<string>(Error.Failure(
                "PDSummary.UnexpectedError",
                $"An unexpected error occurred: {ex.Message}"));
        }
    }

    /// <summary>
    /// Creates both PD Summary table and Average PD table from migration matrices
    /// </summary>
    /// <param name="migrationData">Migration matrices data</param>
    /// <param name="loanDetails">List of loan details for maturity calculation</param>
    /// <param name="logger">Logger for debugging purposes</param>
    /// <returns>JSON string containing bucket values grouped by product category and segment</returns>
    private static string CreatePDSummaryTableWithAverage(MigrationMatricesAndSummaryResponse migrationData, List<LoanDetails> loanDetails, ILogger logger)
    {
        // ==== STEP 3.2.1: INITIALIZE AND PROCESS MATRICES DATA ====
        var matricesProcessingStopwatch = System.Diagnostics.Stopwatch.StartNew();

        List<string> bucketLabels = migrationData.Matrices[0].ProductCategories[0].Segments[0].Buckets;
        List<MigrationMatrix> matrices = migrationData.Matrices;

        // Create result structure organized by product category and segment
        (_, Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>> combinedData, List<string> periodComparisons) = ProcessMatricesData(matrices, bucketLabels);

        matricesProcessingStopwatch.Stop();
        logger.LogInformation("3.2.1 Process Matrices Data: {ElapsedMs}ms ({ElapsedSec:F2}s) - {ProductCategories} categories",
            matricesProcessingStopwatch.ElapsedMilliseconds, matricesProcessingStopwatch.Elapsed.TotalSeconds, combinedData.Keys.Count);

        // ==== STEP 3.2.2: CALCULATE MATURITY DATA ====
        var maturityStopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Get latest period from loan details
        string latestPeriod = GetLatestPeriod(loanDetails);
        // Calculate highest maturity years per product category from latest period
        Dictionary<string, int> highestMaturityByCategory = CalculateHighestMaturityByCategory(loanDetails, latestPeriod, logger);

        maturityStopwatch.Stop();
        logger.LogDebug("3.2.2 Calculate Maturity Data: {ElapsedMs}ms ({ElapsedSec:F2}s) - Found {Count} product categories",
            maturityStopwatch.ElapsedMilliseconds, maturityStopwatch.Elapsed.TotalSeconds, highestMaturityByCategory.Count);

        // ==== STEP 3.2.3: GENERATE AVERAGE PD TABLES ====
        var avgPDTablesStopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Generate average PD tables for each product category and segment
        Dictionary<string, Dictionary<string, AveragePDTable>> averagePDTables = GenerateAveragePDTables(combinedData, bucketLabels, highestMaturityByCategory, logger);

        avgPDTablesStopwatch.Stop();
        logger.LogInformation("3.2.3 Generate Average PD Tables: {ElapsedMs}ms ({ElapsedSec:F2}s) - Generated {TableCount} tables",
            avgPDTablesStopwatch.ElapsedMilliseconds, avgPDTablesStopwatch.Elapsed.TotalSeconds,
            averagePDTables.Values.Sum(dict => dict.Count));

        // Log the captured period comparisons
        logger.LogDebug("Captured {PeriodComparisonCount} unique period comparisons: [{PeriodComparisons}]",
            periodComparisons.Count, string.Join(", ", periodComparisons));

        // ==== STEP 3.2.4: SERIALIZE TO JSON ====
        var serializationStopwatch = System.Diagnostics.Stopwatch.StartNew();

        var finalResult = new
        {
            AveragePDTables = averagePDTables,
            CombinedData = combinedData,
            Periods = periodComparisons
        };

        string finalJson = JsonSerializer.Serialize(finalResult, JsonOptions);

        serializationStopwatch.Stop();

        logger.LogDebug("CreatePDSummaryTableWithAverage completed successfully");
        return finalJson;
    }

    /// <summary>
    /// Processes matrices data and organizes by product category and segment, combining values across all periods
    /// </summary>
    /// <param name="matrices">List of migration matrices</param>
    /// <param name="bucketLabels">List of bucket labels</param>
    /// <returns>Organized data structure, combined data, and list of period comparisons</returns>
    private static (List<Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>>> result,
                   Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>> combinedData,
                   List<string> periodComparisons) ProcessMatricesData(
        List<MigrationMatrix> matrices,
        List<string> bucketLabels)
    {
        (Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>> combinedData, List<string> periodComparisons) = AccumulateDataAcrossPeriods(matrices, bucketLabels);
        List<Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>>> result = ConvertToOutputFormat(combinedData);

        return (result, combinedData, periodComparisons);
    }

    /// <summary>
    /// Accumulates data across all periods for each product category and segment combination
    /// </summary>
    /// <param name="matrices">List of migration matrices</param>
    /// <param name="bucketLabels">List of bucket labels</param>
    /// <returns>Combined data dictionary and list of period comparisons</returns>
    private static (Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>> combinedData, List<string> periods) AccumulateDataAcrossPeriods(
        List<MigrationMatrix> matrices,
        List<string> bucketLabels)
    {
        Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>> combinedData = new();
        List<string> periodComparisons = new();

        foreach (MigrationMatrix matrix in matrices)
        {
            // Create period comparison string (e.g., "2022vs2023")
            if (!string.IsNullOrEmpty(matrix.PeriodNMinus1) && !string.IsNullOrEmpty(matrix.PeriodN))
            {
                string periodComparison = $"{matrix.PeriodNMinus1}vs{matrix.PeriodN}";
                if (!periodComparisons.Contains(periodComparison))
                {
                    periodComparisons.Add(periodComparison);
                }
            }

            IEnumerable<ProductCategory> validProductCategories = matrix.ProductCategories
                .Where(pc => pc.Segments.Exists(s => s.Percentages?.GrandTotal is not null));

            foreach (ProductCategory productCategory in validProductCategories)
            {
                ProcessProductCategory(productCategory, combinedData, bucketLabels);
            }
        }

        // Sort period comparisons to ensure consistent ordering
        periodComparisons.Sort();

        return (combinedData, periodComparisons);
    }

    /// <summary>
    /// Processes a product category and its segments
    /// </summary>
    /// <param name="productCategory">Product category to process</param>
    /// <param name="combinedData">Combined data dictionary to update</param>
    /// <param name="bucketLabels">List of bucket labels</param>
    private static void ProcessProductCategory(
        ProductCategory productCategory,
        Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>> combinedData,
        List<string> bucketLabels)
    {
        IEnumerable<Segment> validSegments = productCategory.Segments
            .Where(s => s.Percentages?.GrandTotal is not null);

        foreach (Segment segment in validSegments)
        {
            Dictionary<string, List<double>> segmentBuckets = CreateSegmentBuckets(segment, bucketLabels);
            MergeSegmentData(productCategory.ProductCategoryName, segment.SegmentName, segmentBuckets, combinedData, bucketLabels);
        }
    }

    /// <summary>
    /// Merges segment data into the combined data structure
    /// </summary>
    /// <param name="productCategoryName">Name of the product category</param>
    /// <param name="segmentName">Name of the segment</param>
    /// <param name="segmentBuckets">Segment bucket data</param>
    /// <param name="combinedData">Combined data dictionary to update</param>
    /// <param name="bucketLabels">List of bucket labels</param>
    private static void MergeSegmentData(
        string productCategoryName,
        string segmentName,
        Dictionary<string, List<double>> segmentBuckets,
        Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>> combinedData,
        List<string> bucketLabels)
    {
        if (!combinedData.TryGetValue(productCategoryName, out Dictionary<string, Dictionary<string, List<double>>>? productCategoryData))
        {
            productCategoryData = new Dictionary<string, Dictionary<string, List<double>>>();
            combinedData[productCategoryName] = productCategoryData;
        }

        if (!productCategoryData.TryGetValue(segmentName, out Dictionary<string, List<double>>? existingSegmentBuckets))
        {
            existingSegmentBuckets = InitializeSegmentBuckets(bucketLabels);
            productCategoryData[segmentName] = existingSegmentBuckets;
        }

        // Merge bucket values from current period into accumulated data
        foreach (KeyValuePair<string, List<double>> bucketEntry in segmentBuckets)
        {
            existingSegmentBuckets[bucketEntry.Key].AddRange(bucketEntry.Value);
        }
    }

    /// <summary>
    /// Initializes empty bucket lists for a segment
    /// </summary>
    /// <param name="bucketLabels">List of bucket labels</param>
    /// <returns>Dictionary with initialized empty lists</returns>
    private static Dictionary<string, List<double>> InitializeSegmentBuckets(List<string> bucketLabels)
    {
        Dictionary<string, List<double>> segmentBuckets = new();
        foreach (string bucketLabel in bucketLabels)
        {
            segmentBuckets[bucketLabel] = new List<double>();
        }
        return segmentBuckets;
    }

    /// <summary>
    /// Converts combined data to the expected output format
    /// </summary>
    /// <param name="combinedData">Combined data dictionary</param>
    /// <returns>List in the expected output format</returns>
    private static List<Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>>> ConvertToOutputFormat(
        Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>> combinedData)
    {
        List<Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>>> result = new();

        foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, List<double>>>> productCategoryEntry in combinedData)
        {
            Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>> productCategoryData = new()
            {
                { productCategoryEntry.Key, productCategoryEntry.Value }
            };

            result.Add(productCategoryData);
        }

        return result;
    }

    /// <summary>
    /// Creates bucket data for a specific segment
    /// </summary>
    /// <param name="segment">The segment to process</param>
    /// <param name="bucketLabels">List of bucket labels</param>
    /// <returns>Dictionary containing bucket data</returns>
    private static Dictionary<string, List<double>> CreateSegmentBuckets(Segment segment, List<string> bucketLabels)
    {
        Dictionary<string, List<double>> segmentBuckets = new();

        // Initialize each bucket with its value from GrandTotal
        for (int i = 0; i < segment.Percentages!.GrandTotal.Count && i < bucketLabels.Count; i++)
        {
            double? grandTotalValue = segment.Percentages.GrandTotal[i];
            string bucketLabel = bucketLabels[i];

            if (!segmentBuckets.TryGetValue(bucketLabel, out List<double>? bucketList))
            {
                bucketList = new List<double>();
                segmentBuckets[bucketLabel] = bucketList;
            }

            if (grandTotalValue.HasValue)
            {
                bucketList.Add(grandTotalValue.Value);
            }
        }

        return segmentBuckets;
    }

    /// <summary>
    /// Gets the latest period from loan details
    /// </summary>
    /// <param name="loanDetails">List of loan details</param>
    /// <returns>Latest period string</returns>
    private static string GetLatestPeriod(List<LoanDetails> loanDetails)
    {
        if (loanDetails is null || loanDetails.Count == 0)
        {
            return string.Empty;
        }

        // Get all unique periods and find the latest one (assuming period is in format that can be compared as string)
        string latestPeriod = loanDetails
            .Where(ld => !string.IsNullOrEmpty(ld.Period))
            .Select(ld => ld.Period)
            .Distinct()
            .OrderByDescending(p => p)
            .FirstOrDefault() ?? string.Empty;

        return latestPeriod;
    }

    /// <summary>
    /// Calculates the highest remaining maturity years per product category for the latest period
    /// </summary>
    /// <param name="loanDetails">List of loan details</param>
    /// <param name="latestPeriod">Latest period to filter by</param>
    /// <param name="logger">Logger for debugging purposes</param>
    /// <returns>Dictionary mapping product category to highest maturity years</returns>
    private static Dictionary<string, int> CalculateHighestMaturityByCategory(List<LoanDetails> loanDetails, string latestPeriod, ILogger logger)
    {
        Dictionary<string, int> highestMaturityByCategory = new();

        if (loanDetails is null || loanDetails.Count == 0)
        {
            logger.LogWarning("Loan details is null or empty. Count: {Count}", loanDetails?.Count ?? 0);
            return highestMaturityByCategory;
        }

        if (string.IsNullOrEmpty(latestPeriod))
        {
            logger.LogWarning("Latest period is null or empty");
            return highestMaturityByCategory;
        }

        logger.LogDebug("Processing {Count} loan details for latest period '{Period}'", loanDetails.Count, latestPeriod);

        // Log all unique periods to understand the data
        var uniquePeriods = loanDetails
            .Where(ld => !string.IsNullOrEmpty(ld.Period))
            .Select(ld => ld.Period)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        logger.LogDebug("Available periods in loan details: {Periods}", string.Join(", ", uniquePeriods));

        // Filter by latest period and log the results
        var loansForLatestPeriod = loanDetails
            .Where(ld => ld.Period == latestPeriod)
            .ToList();

        logger.LogInformation("Found {Count} loans for latest period '{Period}'", loansForLatestPeriod.Count, latestPeriod);

        if (loansForLatestPeriod.Count == 0)
        {
            logger.LogWarning("No loans found for the latest period '{Period}'", latestPeriod);
            return highestMaturityByCategory;
        }

        // Log unique product categories in loan details for the latest period
        var uniqueProductCategories = loansForLatestPeriod
            .Where(ld => !string.IsNullOrEmpty(ld.ProductCategory))
            .Select(ld => ld.ProductCategory)
            .Distinct()
            .ToList();

        logger.LogInformation("Unique product categories found in loans for period '{Period}': {Categories}",
            latestPeriod, string.Join(", ", uniqueProductCategories));

        // Log product categories and their loan counts
        var categoryGroups = loansForLatestPeriod
            .Where(ld => !string.IsNullOrEmpty(ld.ProductCategory))
            .GroupBy(ld => ld.ProductCategory)
            .ToList();

        foreach (IGrouping<string, LoanDetails> group in categoryGroups)
        {
            int maxMaturity = group.Max(ld => ld.RemainingMaturityYears);
            // Store with the original case from loan details for now
            highestMaturityByCategory[group.Key] = maxMaturity;

            logger.LogDebug("Product Category '{Category}': {Count} loans, max maturity: {MaxMaturity}",
                group.Key, group.Count(), maxMaturity);
        }

        return highestMaturityByCategory;
    }

    /// <summary>
    /// Generates Average PD tables for each product category and segment combination
    /// </summary>
    /// <param name="combinedData">Combined data dictionary</param>
    /// <param name="bucketLabels">List of bucket labels</param>
    /// <param name="highestMaturityByCategory">Dictionary mapping product category to highest maturity years</param>
    /// <param name="logger">Logger for debugging purposes</param>
    /// <returns>Dictionary containing average PD tables organized by product category and segment</returns>
    private static Dictionary<string, Dictionary<string, AveragePDTable>> GenerateAveragePDTables(
        Dictionary<string, Dictionary<string, Dictionary<string, List<double>>>> combinedData,
        List<string> bucketLabels,
        Dictionary<string, int> highestMaturityByCategory,
        ILogger logger)
    {
        Dictionary<string, Dictionary<string, AveragePDTable>> averagePDTables = new();

        foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, List<double>>>> productCategoryEntry in combinedData)
        {
            string productCategoryName = productCategoryEntry.Key;
            Dictionary<string, AveragePDTable> segmentTables = new();

            foreach (KeyValuePair<string, Dictionary<string, List<double>>> segmentEntry in productCategoryEntry.Value)
            {
                string segmentName = segmentEntry.Key;
                Dictionary<string, List<double>> bucketData = segmentEntry.Value;

                // Get the highest maturity for this product category (case-insensitive lookup)
                int? highestMaturity = null;
                string? matchingCategory = highestMaturityByCategory.Keys
                    .FirstOrDefault(key => string.Equals(key, productCategoryName, StringComparison.OrdinalIgnoreCase));

                if (matchingCategory is not null)
                {
                    highestMaturity = highestMaturityByCategory[matchingCategory];
                    logger.LogInformation("✅ Successfully matched migration matrix category '{MigrationCategory}' to loan category '{LoanCategory}', highest maturity: {Maturity}",
                        productCategoryName, matchingCategory, highestMaturity);
                }
                else
                {
                    logger.LogWarning("❌ No matching product category found for migration matrix category '{Category}'. Available loan categories: [{AvailableCategories}]",
                        productCategoryName, string.Join(", ", highestMaturityByCategory.Keys));
                }

                logger.LogDebug("Creating Average PD Table for Product Category: '{Category}', Segment: '{Segment}', Highest Maturity: {HighestMaturity}",
                    productCategoryName, segmentName, highestMaturity?.ToString(CultureInfo.InvariantCulture) ?? "NULL");

                AveragePDTable averageTable = CreateAveragePDTable(bucketData, bucketLabels, highestMaturity, logger);
                segmentTables[segmentName] = averageTable;
            }

            averagePDTables[productCategoryName] = segmentTables;
        }

        return averagePDTables;
    }

    /// <summary>
    /// Creates the Average PD table by calculating historical averages from bucket values,
    /// and also computes interpolated PDs to ensure monotonicity.
    /// </summary>
    /// <param name="bucketData">Dictionary containing bucket data for a specific segment</param>
    /// <param name="bucketLabels">List of bucket labels</param>
    /// <param name="highestMaturity">Highest maturity years for the product category</param>
    /// <param name="logger">Logger for debugging purposes</param>
    /// <returns>Average PD table object</returns>
    private static AveragePDTable CreateAveragePDTable(Dictionary<string, List<double>> bucketData, List<string> bucketLabels, int? highestMaturity, ILogger logger)
    {
        logger.LogDebug("CreateAveragePDTable called with HighestMaturity: {HighestMaturity}", highestMaturity?.ToString(CultureInfo.InvariantCulture) ?? "NULL");

        List<AveragePDRow> averageRows = new();
        List<List<double>> bucketValues = new();

        // Extract bucket values in the correct order
        foreach (string bucketLabel in bucketLabels)
        {
            if (bucketData.TryGetValue(bucketLabel, out List<double>? values))
            {
                bucketValues.Add(values);
                logger.LogDebug("Bucket '{BucketLabel}': {Count} values - {Values}",
                    bucketLabel, values.Count, string.Join(", ", values.Take(5)));
            }
            else
            {
                bucketValues.Add(new List<double>());
                logger.LogDebug("Bucket '{BucketLabel}': No values found", bucketLabel);
            }
        }

        // Step 1: Calculate historical PDs (average of all values for each bucket)
        List<double?> historicalPDs = CalculateHistoricalPDs(bucketValues);

        // Step 2: Calculate interpolated PDs (monotonic, using formulas)
        List<double?> interpolatedPDs = CalculateInterpolatedPDs(historicalPDs);

        // Step 3: Compose result rows
        for (int i = 0; i < bucketLabels.Count; i++)
        {
            averageRows.Add(new AveragePDRow
            {
                AgeBucket = bucketLabels[i],
                HistoricalPD = historicalPDs[i],
                InterpolatedPD = interpolatedPDs[i]
            });
        }

        var averageTable = new AveragePDTable
        {
            ColumnHeaders = new List<string> { "Age Bucket", "Historical PDs", "Interpolated PDs" },
            Rows = averageRows,
            BucketCount = bucketLabels.Count,
            HighestMaturity = highestMaturity
        };

        logger.LogDebug("Created AveragePDTable with HighestMaturity: {HighestMaturity}, BucketCount: {BucketCount}, RowCount: {RowCount}",
            averageTable.HighestMaturity?.ToString(CultureInfo.InvariantCulture) ?? "NULL",
            averageTable.BucketCount,
            averageTable.Rows.Count);

        return averageTable;
    }

    /// <summary>
    /// Calculates historical PDs (average of all values for each bucket)
    /// </summary>
    /// <param name="bucketValues">List of value lists for each bucket</param>
    /// <returns>List of historical PD values</returns>
    private static List<double?> CalculateHistoricalPDs(List<List<double>> bucketValues)
    {
        List<double?> historicalPDs = new();

        foreach (List<double> values in bucketValues)
        {
            double? historicalPD = null;

            if (values.Count > 0)
            {
                // Calculate average of all values
                historicalPD = Math.Round(values.Average(), 2);
            }

            historicalPDs.Add(historicalPD);
        }

        return historicalPDs;
    }

    /// <summary>
    /// Calculates interpolated PDs (monotonic, using formulas)
    /// </summary>
    /// <param name="historicalPDs">List of historical PD values</param>
    /// <returns>List of interpolated PD values</returns>
    private static List<double?> CalculateInterpolatedPDs(List<double?> historicalPDs)
    {
        List<double?> interpolatedPDs = new();

        for (int i = 0; i < historicalPDs.Count; i++)
        {
            double? interpolated = CalculateInterpolatedPDForBucket(historicalPDs, interpolatedPDs, i);
            interpolatedPDs.Add(interpolated);
        }

        return interpolatedPDs;
    }

    /// <summary>
    /// Calculates interpolated PD for a specific bucket index
    /// </summary>
    /// <param name="historicalPDs">List of historical PD values</param>
    /// <param name="interpolatedPDs">List of already calculated interpolated PD values</param>
    /// <param name="index">Current bucket index</param>
    /// <returns>Interpolated PD value for the bucket</returns>
    private static double? CalculateInterpolatedPDForBucket(List<double?> historicalPDs, List<double?> interpolatedPDs, int index)
    {
        if (!historicalPDs[index].HasValue)
        {
            return null;
        }

        if (index == 0)
        {
            // First bucket, just use historical
            return historicalPDs[0];
        }

        if (index == historicalPDs.Count - 1)
        {
            // Last bucket (highest), just use historical
            return historicalPDs[index];
        }

        double prevInterpolated = interpolatedPDs[index - 1] ?? 0.0;
        double currHistorical = historicalPDs[index] ?? 0.0;

        if (currHistorical >= prevInterpolated)
        {
            return Math.Round(currHistorical, 2);
        }

        // Formula 1: Interpolate to highest bucket
        int steps = historicalPDs.Count - index; // including current & up to last
        double highest = historicalPDs[historicalPDs.Count - 1] ?? 0.0;
        double formula1 = prevInterpolated + (highest - prevInterpolated) / steps;

        // Formula 2: Delta of previous two buckets
        double delta = 0.0;
        if (index >= 2 && interpolatedPDs[index - 2].HasValue)
        {
            delta = prevInterpolated - (interpolatedPDs[index - 2] ?? 0.0);
        }
        double formula2 = prevInterpolated + delta;

        // Choose smallest result that is at least prevInterpolated
        double minFormula = Math.Min(formula1, formula2);
        if (minFormula < prevInterpolated)
        {
            minFormula = Math.Max(formula1, formula2); // if both below, take the higher one
        }

        double interpolated = Math.Max(prevInterpolated, minFormula);
        return Math.Round(interpolated, 2);
    }

}