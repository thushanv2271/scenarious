using System;
using System.Globalization;
using System.Text.Json;
using Application.Abstractions.Data;
using Application.DTOs.PD;
using Application.Models;
using Application.Services;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Infrastructure.PDCalculationSteps.Steps;

/// <summary>
/// Step 2 of PD Calculation: Migration matrix generation with counts and percentages (segment-wise)
/// </summary>
public class Step2GenerateMigrationMatrices
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPDSetupConfigurationService _pdSetupConfigurationService;
    private readonly ILogger<Step2GenerateMigrationMatrices> _logger;
    private readonly TimeConfig _timeConfig;
    private readonly List<DatePassedDueBucket> _datePassedDueBuckets;
    private readonly List<PDConfiguration> _pdConfiguration;

    public Step2GenerateMigrationMatrices(
        IApplicationDbContext dbContext,
        IPDSetupConfigurationService pdSetupConfigurationService,
        ILogger<Step2GenerateMigrationMatrices> logger,
        TimeConfig timeConfig,
        List<DatePassedDueBucket> datePassedDueBuckets,
        List<PDConfiguration> pdConfiguration)
    {
        _dbContext = dbContext;
        _pdSetupConfigurationService = pdSetupConfigurationService;
        _logger = logger;
        _timeConfig = timeConfig;
        _datePassedDueBuckets = datePassedDueBuckets;
        _pdConfiguration = pdConfiguration;
    }

    /// <summary>
    /// Executes Step 2: generates migration matrices segment-wise with both counts and percentages
    /// </summary>
    /// <param name="createdBy">User who initiated the process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the generated period-based migration matrices with segments</returns>
    public async Task<Result<IReadOnlyList<PeriodMigrationMatrix>>> ExecuteAsync(
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing Step 2: Segment-wise migration matrix generation");

            // First prepare the datasets segment-wise
            IReadOnlyList<PdMigrationDataset> datasets = await PrepareDatasetForMigrationMatrixAsync(cancellationToken);

            if (datasets.Count == 0)
            {
                _logger.LogWarning("No migration datasets available for matrix generation");
                return Result.Failure<IReadOnlyList<PeriodMigrationMatrix>>(Error.NotFound(
                    "Step2.NoDatasets",
                    "No migration datasets available for matrix generation"));
            }

            // Generate migration matrices segment-wise with both counts and percentages
            IReadOnlyList<PeriodMigrationMatrix> matrices = await GenerateSegmentMigrationMatricesAsync(datasets, cancellationToken);

            _logger.LogInformation("Step 2 completed successfully. Generated {Count} period matrices with segment breakdown",
                matrices.Count);

            return Result.Success(matrices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Step 2 execution: {ErrorMessage}", ex.Message);
            return Result.Failure<IReadOnlyList<PeriodMigrationMatrix>>(Error.Failure(
                "PDCalculation.Step2.UnexpectedError",
                $"An unexpected error occurred during Step 2 execution: {ex.Message}"));
        }
    }

    /// <summary>
    /// Prepares PD datasets for migration matrix analysis segment-wise.
    /// Creates one dataset for each available period pair with segment breakdown.
    /// </summary>
    /// <returns>A collection of PD migration datasets with segment data</returns>
    public async Task<IReadOnlyList<PdMigrationDataset>> PrepareDatasetForMigrationMatrixAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Preparing PD dataset for migration matrix (segment-wise)");

        try
        {
            // Get all distinct periods from loan details, ordered chronologically
            List<string> distinctPeriods = await _dbContext.LoanDetails
                .Where(ld => !string.IsNullOrEmpty(ld.Period))
                .Select(ld => ld.Period)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync(cancellationToken);

            if (distinctPeriods.Count < 2)
            {
                _logger.LogWarning("Not enough periods found for migration matrix. Found {Count} periods", distinctPeriods.Count);
                return Array.Empty<PdMigrationDataset>();
            }

            _logger.LogInformation("Found {Count} distinct periods: {Periods}", distinctPeriods.Count, string.Join(", ", distinctPeriods));

            // Get segment configurations and frequency for validation
            List<PDConfiguration> segmentConfigurations = _pdConfiguration;
            string frequency = _timeConfig.Frequency;

            if (segmentConfigurations.Count == 0)
            {
                _logger.LogWarning("No segment configurations found in Step6");
                return Array.Empty<PdMigrationDataset>();
            }

            _logger.LogInformation("Found {Count} segment configurations and frequency: {Frequency}",
                segmentConfigurations.Count, frequency);

            // Get the worst bucket
            string? worstBucket = await GetWorstBucketAsync(cancellationToken);

            List<PdMigrationDataset> datasets = new();

            // Create period pairs (N-1, N) for migration analysis
            for (int i = 1; i < distinctPeriods.Count; i++)
            {
                string periodN = distinctPeriods[i];

                // Calculate period differences and create product category datasets
                IReadOnlyList<PdProductCategoryMigrationData> productCategoryDatasets = await CreateProductCategoryMigrationDataAsync(
                    periodN, segmentConfigurations, distinctPeriods, frequency, worstBucket ?? string.Empty, cancellationToken);

                _logger.LogInformation("Processing period pair for period: {PeriodN}", periodN);
                if (productCategoryDatasets.Count > 0)
                {
                    // Get the period N-1 from the first product category's first segment (they should all have the same adjusted period)
                    string periodNMinus1 = string.Empty;
                    if (productCategoryDatasets.Count > 0 && productCategoryDatasets[0].Segments.Count > 0)
                    {
                        // We need to calculate this based on the first configuration
                        PDConfiguration? firstConfig = segmentConfigurations.FirstOrDefault();
                        if (firstConfig != null)
                        {
                            periodNMinus1 = CalculateAdjustedPeriodNMinus1(periodN, distinctPeriods, frequency, firstConfig.ComparisonPeriod);
                        }
                    }

                    PdMigrationDataset dataset = new()
                    {
                        PeriodNMinus1 = periodNMinus1,
                        PeriodN = periodN,
                        ProductCategories = productCategoryDatasets
                    };

                    datasets.Add(dataset);

                    _logger.LogInformation("Created dataset for period pair {PeriodNMinus1} -> {PeriodN} with {ProductCategoryCount} product categories",
                        periodNMinus1, periodN, productCategoryDatasets.Count);
                }
            }

            _logger.LogInformation("Successfully prepared {DatasetCount} PD migration datasets", datasets.Count);
            return datasets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while preparing PD dataset for migration matrix: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException("Failed to prepare PD dataset for migration matrix. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Gets the worst bucket (bucket with the highest range end value) from the date passed due buckets configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The bucket label of the worst bucket, or null if no buckets are configured</returns>
    private async Task<string?> GetWorstBucketAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_datePassedDueBuckets is null || _datePassedDueBuckets.Count == 0)
            {
                _logger.LogWarning("No date passed due buckets configured");
                throw new InvalidOperationException("No date passed due buckets configured");
            }

            // Find the bucket with the highest range end value
            DatePassedDueBucket? worstBucket = await Task.Run(() => _datePassedDueBuckets
                .Where(bucket => !string.IsNullOrWhiteSpace(bucket.RangeEnd))
                .OrderByDescending(bucket => int.TryParse(bucket.RangeEnd, out int rangeEnd) ? rangeEnd : 0)
                .FirstOrDefault(), cancellationToken);

            if (worstBucket is null)
            {
                _logger.LogWarning("Could not determine worst bucket from configured buckets");
                throw new InvalidOperationException("Could not determine worst bucket from configured buckets");
            }

            _logger.LogDebug("Worst bucket determined: {BucketLabel} (Range: {RangeStart}-{RangeEnd})",
                worstBucket.BucketLabel, worstBucket.RangeStart, worstBucket.RangeEnd);

            return worstBucket.BucketLabel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while determining worst bucket: {ErrorMessage}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Calculates the adjusted period N-1 based on comparison period
    /// </summary>
    private string CalculateAdjustedPeriodNMinus1(string periodN, List<string> distinctPeriods, string frequency, string comparisonFrequency)
    {
        // Normalize to uppercase to make lookup case-insensitive
        string freq = frequency.ToUpperInvariant();
        string comp = comparisonFrequency.ToUpperInvariant();

        var stepMap = new Dictionary<(string, string), int>
        {
            { ("YEARLY", "YEARLY"), 1 },
            { ("QUARTERLY", "YEARLY"), 4 },
            { ("QUARTERLY", "QUARTERLY"), 1 },
            { ("MONTHLY", "YEARLY"), 12 },
            { ("MONTHLY", "QUARTERLY"), 3 },
            { ("MONTHLY", "MONTHLY"), 1 }
        };

        if (!stepMap.TryGetValue((freq, comp), out int step))
        {
            return periodN; // unknown frequency combo -> return same period
        }

        int currentIndex = distinctPeriods.IndexOf(periodN);

        // if period not found or not enough history, return original periodN
        if (currentIndex < step || currentIndex < 0)
        {
            return periodN;
        }

        return distinctPeriods[currentIndex - step];
    }

    /// <summary>
    /// Creates migration rows for a specific segment and product category combination for a period pair
    /// </summary>
    private async Task<IReadOnlyList<PdMigrationRowDto>> CreateMigrationRowsForSegmentAsync(
        string periodNMinus1,
        string periodN,
        PDConfiguration segmentConfig,
        string worstBucket,
        CancellationToken cancellationToken = default)
    {
        // Get loan details for both periods filtered by both product category and segment
        var loanDetailsNMinus1 = await _dbContext.LoanDetails
            .Where(ld => ld.Period == periodNMinus1
                && ld.ProductCategory == segmentConfig.ProductCategory
                && ld.Segment == segmentConfig.Segment)
            .Select(ld => new
            {
                ld.CustomerNumber,
                ld.FacilityNumber,
                ld.FinalBucket,
                ld.ProductCategory,
                ld.Segment
            })
            .ToListAsync(cancellationToken);

        var loanDetailsN = await _dbContext.LoanDetails
            .Where(ld => ld.Period == periodN
                && ld.ProductCategory == segmentConfig.ProductCategory
                && ld.Segment == segmentConfig.Segment)
            .Select(ld => new
            {
                ld.CustomerNumber,
                ld.FacilityNumber,
                ld.FinalBucket,
                ld.ProductCategory,
                ld.Segment
            })
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {CountNMinus1} loans in period {PeriodNMinus1} and {CountN} loans in period {PeriodN} for ProductCategory {ProductCategory}, Segment {Segment}",
            loanDetailsNMinus1.Count, periodNMinus1, loanDetailsN.Count, periodN, segmentConfig.ProductCategory, segmentConfig.Segment);

        // Create lookup for period N data
        var periodNLookup = loanDetailsN
            .ToLookup(ld => new { ld.CustomerNumber, ld.FacilityNumber }, ld => ld.FinalBucket);

        // Find matching customer/facility combinations and create migration rows
        List<PdMigrationRowDto> migrationRows = new();

        foreach (var loanNMinus1 in loanDetailsNMinus1)
        {
            var key = new { loanNMinus1.CustomerNumber, loanNMinus1.FacilityNumber };
            string loanNFinalBucket = periodNLookup[key].FirstOrDefault() ?? string.Empty;

            // Check if either period has the worst bucket
            if (loanNMinus1.FinalBucket == worstBucket || loanNFinalBucket == worstBucket)
            {
                if (string.IsNullOrEmpty(loanNFinalBucket))
                {
                    PdMigrationRowDto migrationRow = new()
                    {
                        CustomerId = loanNMinus1.CustomerNumber,
                        FacilityId = loanNMinus1.FacilityNumber,
                        ProductCategory = loanNMinus1.ProductCategory,
                        Segment = loanNMinus1.Segment,
                        BucketStatusNMinus1 = loanNMinus1.FinalBucket,
                        BucketStatusN = "N/A",
                        FinalizedBucketStatus = "N/A"
                    };
                    migrationRows.Add(migrationRow);
                }
                else
                {
                    PdMigrationRowDto migrationRow = new()
                    {
                        CustomerId = loanNMinus1.CustomerNumber,
                        FacilityId = loanNMinus1.FacilityNumber,
                        ProductCategory = loanNMinus1.ProductCategory,
                        Segment = loanNMinus1.Segment,
                        BucketStatusNMinus1 = loanNMinus1.FinalBucket,
                        BucketStatusN = loanNFinalBucket,
                        FinalizedBucketStatus = worstBucket
                    };
                    migrationRows.Add(migrationRow);
                }
            }
            else
            {
                string loanNFinalBucketFixed = string.IsNullOrEmpty(loanNFinalBucket) ? "N/A" : loanNFinalBucket;
                PdMigrationRowDto migrationRow = new()
                {
                    CustomerId = loanNMinus1.CustomerNumber,
                    FacilityId = loanNMinus1.FacilityNumber,
                    ProductCategory = loanNMinus1.ProductCategory,
                    Segment = loanNMinus1.Segment,
                    BucketStatusNMinus1 = loanNMinus1.FinalBucket,
                    BucketStatusN = loanNFinalBucketFixed,
                    FinalizedBucketStatus = loanNFinalBucketFixed
                };

                migrationRows.Add(migrationRow);
            }
        }

        _logger.LogDebug("Created {MigrationRowCount} migration rows for ProductCategory {ProductCategory}, Segment {Segment}, period pair {PeriodNMinus1} -> {PeriodN}",
            migrationRows.Count, segmentConfig.ProductCategory, segmentConfig.Segment, periodNMinus1, periodN);

        return migrationRows;
    }

    /// <summary>
    /// Generates segment-wise migration matrices from PD datasets with both counts and percentages.
    /// </summary>
    public async Task<IReadOnlyList<PeriodMigrationMatrix>> GenerateSegmentMigrationMatricesAsync(
        IReadOnlyList<PdMigrationDataset> datasets,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating segment-wise migration matrices from {Count} datasets", datasets.Count);

        try
        {
            // Get bucket labels (single call, cached)
            List<string> bucketLabels = await GetBucketLabelsAsync(cancellationToken);

            if (bucketLabels.Count == 0)
            {
                _logger.LogWarning("No bucket labels found in PDSetup configuration");
                return Array.Empty<PeriodMigrationMatrix>();
            }

            _logger.LogInformation("Found {BucketCount} bucket labels: {Labels}",
                bucketLabels.Count, string.Join(", ", bucketLabels));

            List<PeriodMigrationMatrix> periodMatrices = new();

            // For each PdMigrationDataset (per period pair), create a PeriodMigrationMatrix
            foreach (PdMigrationDataset dataset in datasets)
            {
                _logger.LogInformation("Processing dataset for period pair: {PeriodNMinus1} -> {PeriodN} with {ProductCategoryCount} product categories",
                    dataset.PeriodNMinus1, dataset.PeriodN, dataset.ProductCategories.Count);

                PeriodMigrationMatrix periodMatrix = new()
                {
                    PeriodNMinus1 = dataset.PeriodNMinus1,
                    PeriodN = dataset.PeriodN
                };

                // Process each product category and create ProductCategoryMigrationMatrix
                foreach (PdProductCategoryMigrationData productCategoryData in dataset.ProductCategories)
                {
                    ProductCategoryMigrationMatrix productCategoryMatrix = new()
                    {
                        ProductCategoryName = productCategoryData.ProductCategoryName
                    };

                    // Process each segment within the product category
                    foreach (PdSegmentMigrationData segmentData in productCategoryData.Segments)
                    {
                        SegmentMigrationMatrix segmentMatrix = new()
                        {
                            SegmentName = segmentData.SegmentName,
                            ProductCategory = productCategoryData.ProductCategoryName,
                            ComparisonPeriod = segmentData.ComparisonPeriod,
                            Buckets = new List<string>(bucketLabels)
                        };

                        // Initialize matrix with both counts and percentages
                        segmentMatrix.InitializeMatrix(bucketLabels.Count, includePercentages: true);

                        // Populate counts and row totals
                        PopulateSegmentMatrixCounts(segmentMatrix, segmentData.Rows);

                        // Calculate percentages
                        CalculateSegmentPercentages(segmentMatrix);

                        // Calculate Grand Total (Cumulative PD)
                        CalculateSegmentGrandTotal(segmentMatrix);

                        // Add segment matrix to product category
                        productCategoryMatrix.Segments.Add(segmentMatrix);

                        _logger.LogDebug("Created segment matrix for ProductCategory {ProductCategory}, Segment {Segment} with {RowCount} migrations",
                            productCategoryData.ProductCategoryName, segmentData.SegmentName, segmentData.Rows.Count);
                    }

                    // Add product category matrix to period matrix
                    periodMatrix.ProductCategories.Add(productCategoryMatrix);

                    _logger.LogDebug("Created product category matrix for {ProductCategory} with {SegmentCount} segments",
                        productCategoryData.ProductCategoryName, productCategoryMatrix.Segments.Count);
                }

                periodMatrices.Add(periodMatrix);

                _logger.LogInformation("Created period matrix for {PeriodNMinus1} -> {PeriodN} with {ProductCategoryCount} product categories and {SegmentCount} total segments",
                    dataset.PeriodNMinus1, dataset.PeriodN, periodMatrix.ProductCategories.Count,
                    periodMatrix.ProductCategories.Sum(pc => pc.Segments.Count));
            }

            _logger.LogInformation("Successfully generated {MatrixCount} period migration matrices with hierarchical product category and segment breakdown", periodMatrices.Count);
            return periodMatrices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating segment migration matrices: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException("Failed to generate segment migration matrices. See inner exception for details.", ex);
        }
    }

    private async Task<List<string>> GetBucketLabelsAsync(CancellationToken cancellationToken = default)
    {
        if (_datePassedDueBuckets is null || _datePassedDueBuckets.Count == 0)
        {
            _logger.LogWarning("No date passed due buckets configured");
            return new List<string>();
        }

        // Sort buckets by range start value in ascending order
        List<string> bucketLabels = await Task.Run(() => _datePassedDueBuckets
            .OrderBy(bucket => int.TryParse(bucket.RangeStart, out int rangeStart) ? rangeStart : int.MaxValue)
            .Select(bucket => bucket.BucketLabel)
            .ToList(), cancellationToken);

        _logger.LogDebug("Retrieved {Count} bucket labels in ascending order: {Labels}",
            bucketLabels.Count, string.Join(", ", bucketLabels));

        return bucketLabels;
    }

    private void PopulateSegmentMatrixCounts(SegmentMigrationMatrix matrix, IReadOnlyList<PdMigrationRowDto> rows)
    {
        var transitionGroups = rows
            .GroupBy(r => new { FromBucket = r.BucketStatusNMinus1, ToBucket = r.FinalizedBucketStatus })
            .ToList();

        foreach (var group in transitionGroups)
        {
            string fromBucket = group.Key.FromBucket;
            string toBucket = group.Key.ToBucket;
            int count = group.Count();

            int fromIndex = matrix.Buckets.IndexOf(fromBucket);

            if (fromIndex >= 0)
            {
                if (toBucket == "N/A")
                {
                    matrix.Counts.ExitCounts[fromIndex] += count;
                }
                else
                {
                    int toIndex = matrix.Buckets.IndexOf(toBucket);
                    if (toIndex >= 0)
                    {
                        matrix.Counts.Matrix[fromIndex][toIndex] += count;
                    }
                }

                matrix.Counts.RowTotals[fromIndex] += count;
            }
        }
    }

    private void CalculateSegmentPercentages(SegmentMigrationMatrix matrix)
    {
        if (matrix.Percentages == null)
        {
            throw new InvalidOperationException("Percentage data not initialized");
        }

        int bucketCount = matrix.BucketCount;

        for (int fromIndex = 0; fromIndex < bucketCount; fromIndex++)
        {
            int rowTotal = matrix.Counts.RowTotals[fromIndex];

            for (int toIndex = 0; toIndex < bucketCount; toIndex++)
            {
                if (fromIndex == toIndex || toIndex < fromIndex)
                {
                    matrix.Percentages.Matrix[fromIndex][toIndex] = null;
                }
                else if (rowTotal > 0)
                {
                    double percentage = (double)matrix.Counts.Matrix[fromIndex][toIndex] / rowTotal * 100.0;
                    matrix.Percentages.Matrix[fromIndex][toIndex] = Math.Round(percentage, 2);
                }
                else
                {
                    matrix.Percentages.Matrix[fromIndex][toIndex] = null;
                }
            }

            if (rowTotal > 0)
            {
                double exitPercentage = (double)matrix.Counts.ExitCounts[fromIndex] / rowTotal * 100.0;
                matrix.Percentages.ExitPercentages[fromIndex] = Math.Round(exitPercentage, 2);
            }
            else
            {
                matrix.Percentages.ExitPercentages[fromIndex] = null;
            }
        }
    }

    private void CalculateSegmentGrandTotal(SegmentMigrationMatrix matrix)
    {
        if (matrix.Percentages == null)
        {
            throw new InvalidOperationException("Percentage data not initialized");
        }

        int bucketCount = matrix.BucketCount;
        double[] grandTotal = new double[bucketCount];

        grandTotal[bucketCount - 1] = 100.0;

        for (int fromBucket = bucketCount - 2; fromBucket >= 0; fromBucket--)
        {
            double cumulativePD = 0.0;

            for (int toBucket = fromBucket + 1; toBucket < bucketCount; toBucket++)
            {
                double? transitionPercentage = matrix.Percentages.Matrix[fromBucket][toBucket];
                if (transitionPercentage.HasValue)
                {
                    cumulativePD += transitionPercentage.Value / 100.0 * grandTotal[toBucket];
                }
            }

            double? exitPercentage = matrix.Percentages.ExitPercentages[fromBucket];
            if (exitPercentage.HasValue)
            {
                cumulativePD += exitPercentage.Value / 100.0 * 100.0;
            }

            grandTotal[fromBucket] = Math.Round(cumulativePD, 2);
        }

        matrix.Percentages.GrandTotal = grandTotal;
    }

    /// <summary>
    /// Creates product category-specific migration data for a period pair, grouping segments by product category
    /// </summary>
    private async Task<IReadOnlyList<PdProductCategoryMigrationData>> CreateProductCategoryMigrationDataAsync(
        string periodN,
        List<PDConfiguration> segmentConfigurations,
        List<string> distinctPeriods,
        string frequency,
        string worstBucket,
        CancellationToken cancellationToken = default)
    {
        List<PdProductCategoryMigrationData> productCategoryDatasets = new();

        // Group configurations by ProductCategory
        IEnumerable<IGrouping<string, PDConfiguration>> configsByProductCategory = segmentConfigurations
            .Where(config => _pdSetupConfigurationService.ValidateComparisonPeriodCompatibility(frequency, config.ComparisonPeriod))
            .GroupBy(config => config.ProductCategory)
            .ToList();

        foreach (IGrouping<string, PDConfiguration> productCategoryGroup in configsByProductCategory)
        {
            string productCategoryName = productCategoryGroup.Key;
            List<PdSegmentMigrationData> segmentDatasets = new();

            foreach (PDConfiguration segmentConfig in productCategoryGroup)
            {
                // Determine the correct periodNMinus1 based on comparison period
                string adjustedPeriodNMinus1 = CalculateAdjustedPeriodNMinus1(periodN, distinctPeriods, frequency, segmentConfig.ComparisonPeriod);

                if (adjustedPeriodNMinus1 == periodN)
                {
                    _logger.LogWarning("Skipping segment {Segment} for ProductCategory {ProductCategory} due to insufficient history for comparison period {ComparisonPeriod}",
                        segmentConfig.Segment, productCategoryName, segmentConfig.ComparisonPeriod);
                    continue;
                }

                // Create migration rows for this specific segment and product category combination
                IReadOnlyList<PdMigrationRowDto> migrationRows = await CreateMigrationRowsForSegmentAsync(
                    adjustedPeriodNMinus1, periodN, segmentConfig, worstBucket, cancellationToken);

                if (migrationRows.Count > 0)
                {
                    PdSegmentMigrationData segmentData = new()
                    {
                        SegmentName = segmentConfig.Segment,
                        ComparisonPeriod = segmentConfig.ComparisonPeriod,
                        Rows = migrationRows
                    };

                    segmentDatasets.Add(segmentData);

                    _logger.LogDebug("Created segment data for {Segment} (ProductCategory: {ProductCategory}) with {RowCount} migration rows",
                        segmentConfig.Segment, productCategoryName, migrationRows.Count);
                }
            }

            if (segmentDatasets.Count > 0)
            {
                PdProductCategoryMigrationData productCategoryData = new()
                {
                    ProductCategoryName = productCategoryName,
                    Segments = segmentDatasets
                };

                productCategoryDatasets.Add(productCategoryData);

                _logger.LogDebug("Created product category data for {ProductCategory} with {SegmentCount} segments",
                    productCategoryName, segmentDatasets.Count);
            }
        }

        return productCategoryDatasets;
    }
}
