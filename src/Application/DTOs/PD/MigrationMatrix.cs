using System;

namespace Application.DTOs.PD;

/// <summary>
/// Represents a segment-wise migration matrix for PD analysis.
/// This class is used only in memory and not persisted in the database.
/// </summary>
public sealed class SegmentMigrationMatrix
{
    /// <summary>
    /// Gets or sets the segment name
    /// </summary>
    public string SegmentName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product category for this segment
    /// </summary>
    public string ProductCategory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comparison period for this segment
    /// </summary>
    public string ComparisonPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the canonical list of age bucket labels (ordered)
    /// </summary>
    public List<string> Buckets { get; set; } = new();

    /// <summary>
    /// Gets or sets the count-based matrix data
    /// </summary>
    public MigrationMatrix.CountData Counts { get; set; } = new();

    /// <summary>
    /// Gets or sets the percentage-based matrix data (includes Grand Total column)
    /// Only populated when percentage calculations are requested
    /// </summary>
    public MigrationMatrix.PercentageData? Percentages { get; set; }

    /// <summary>
    /// Gets the total number of buckets in the matrix
    /// </summary>
    public int BucketCount => Buckets.Count;

    /// <summary>
    /// Gets whether this matrix contains percentage data
    /// </summary>
    public bool HasPercentageData => Percentages != null;

    /// <summary>
    /// Initializes the matrix with the specified bucket count
    /// </summary>
    /// <param name="bucketCount">Number of buckets</param>
    /// <param name="includePercentages">Whether to include percentage calculations</param>
    public void InitializeMatrix(int bucketCount, bool includePercentages = false)
    {
        // Always initialize count data
        Counts.Initialize(bucketCount);

        // Conditionally initialize percentage data
        if (includePercentages)
        {
            Percentages = new MigrationMatrix.PercentageData();
            Percentages.Initialize(bucketCount);
        }
    }
}

/// <summary>
/// Represents a product category-specific migration matrix containing segments for that product category.
/// This class is used only in memory and not persisted in the database.
/// </summary>
public sealed class ProductCategoryMigrationMatrix
{
    /// <summary>
    /// Gets or sets the product category name
    /// </summary>
    public string ProductCategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of segment migration matrices for this product category
    /// </summary>
    public List<SegmentMigrationMatrix> Segments { get; set; } = new();
}

/// <summary>
/// Represents a period-based migration matrix collection with product category and segment breakdown.
/// This class is used only in memory and not persisted in the database.
/// </summary>
public sealed class PeriodMigrationMatrix
{
    /// <summary>
    /// Gets or sets the N-1 period identifier
    /// </summary>
    public string PeriodNMinus1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the N period identifier
    /// </summary>
    public string PeriodN { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of product category migration matrices for this period pair
    /// </summary>
    public List<ProductCategoryMigrationMatrix> ProductCategories { get; set; } = new();
}

/// <summary>
/// Represents a migration matrix for PD analysis containing transition counts between buckets.
/// This class is used only in memory and not persisted in the database.
/// </summary>
public sealed class MigrationMatrix
{
    /// <summary>
    /// Gets or sets the N-1 period identifier
    /// </summary>
    public string PeriodNMinus1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the N period identifier
    /// </summary>
    public string PeriodN { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the canonical list of age bucket labels (ordered)
    /// </summary>
    public List<string> Buckets { get; set; } = new();

    /// <summary>
    /// Gets or sets the count-based matrix data
    /// </summary>
    public CountData Counts { get; set; } = new();

    /// <summary>
    /// Gets or sets the percentage-based matrix data (includes Grand Total column)
    /// Only populated when percentage calculations are requested
    /// </summary>
    public PercentageData? Percentages { get; set; }

    /// <summary>
    /// Gets the total number of buckets in the matrix
    /// </summary>
    public int BucketCount => Buckets.Count;

    /// <summary>
    /// Gets whether this matrix contains percentage data
    /// </summary>
    public bool HasPercentageData => Percentages != null;

    /// <summary>
    /// Initializes the matrix with the specified bucket count
    /// </summary>
    /// <param name="bucketCount">Number of buckets</param>
    /// <param name="includePercentages">Whether to include percentage calculations</param>
    public void InitializeMatrix(int bucketCount, bool includePercentages = false)
    {
        // Always initialize count data
        Counts.Initialize(bucketCount);

        // Conditionally initialize percentage data
        if (includePercentages)
        {
            Percentages = new PercentageData();
            Percentages.Initialize(bucketCount);
        }
    }

    /// <summary>
    /// Represents count-based matrix data
    /// </summary>
    public sealed class CountData
    {
        /// <summary>
        /// Gets or sets the 2D array holding core transition counts
        /// Using jagged array instead of multidimensional for better performance
        /// Where [i][j] represents count of transitions from bucket i to bucket j
        /// </summary>
        public int[][] Matrix { get; set; } = Array.Empty<int[]>();

        /// <summary>
        /// Gets or sets the exit counts per bucket (transitions to "N/A")
        /// Array size: [Buckets.Count]
        /// Where [i] represents count of exits from bucket i to "N/A" (dropped out customers/facilities)
        /// </summary>
        public int[] ExitCounts { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Gets or sets the total counts per row (matches the "Total" column in BA's sheet)
        /// Array size: [Buckets.Count]
        /// Includes both bucket-to-bucket transitions and exits (Matrix + ExitCounts)
        /// </summary>
        public int[] RowTotals { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Initializes the count data arrays
        /// </summary>
        /// <param name="bucketCount">Number of buckets</param>
        internal void Initialize(int bucketCount)
        {
            Matrix = new int[bucketCount][];
            for (int i = 0; i < bucketCount; i++)
            {
                Matrix[i] = new int[bucketCount];
            }
            ExitCounts = new int[bucketCount];
            RowTotals = new int[bucketCount];
        }
    }

    /// <summary>
    /// Represents percentage-based matrix data (includes Grand Total column)
    /// </summary>
    public sealed class PercentageData
    {
        /// <summary>
        /// Gets or sets the 2D array holding percentage transition rates
        /// Using nullable double to distinguish between 0% and "not applicable"
        /// Where [i][j] represents percentage of transitions from bucket i to bucket j
        /// </summary>
        public double?[][] Matrix { get; set; } = Array.Empty<double?[]>();

        /// <summary>
        /// Gets or sets the exit percentages per bucket (transitions to "N/A")
        /// Array size: [Buckets.Count]
        /// Where [i] represents percentage of exits from bucket i to "N/A"
        /// </summary>
        public double?[] ExitPercentages { get; set; } = Array.Empty<double?>();

        /// <summary>
        /// Gets or sets the Grand Total column from Excel (cumulative probability of default)
        /// Matches Excel cells I13-I17 in the percentage matrix
        /// Where [i] represents the ultimate probability of default starting from bucket i
        /// This is calculated using recursive formula based on transition probabilities
        /// </summary>
        public double[] GrandTotal { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Initializes the percentage data arrays
        /// </summary>
        /// <param name="bucketCount">Number of buckets</param>
        internal void Initialize(int bucketCount)
        {
            Matrix = new double?[bucketCount][];
            for (int i = 0; i < bucketCount; i++)
            {
                Matrix[i] = new double?[bucketCount];
            }
            ExitPercentages = new double?[bucketCount];
            GrandTotal = new double[bucketCount];
        }
    }
}
