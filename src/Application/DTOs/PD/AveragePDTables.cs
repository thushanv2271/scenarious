using System;
using System.Collections.Generic;

namespace Application.DTOs.PD;

/// <summary>
/// Represents the root response containing average PD tables organized by product category and segment.
/// This class is used only in memory and not persisted in the database.
/// </summary>
public sealed class AveragePDTablesResponse
{
    /// <summary>
    /// Gets or sets the collection of average PD tables organized by product category
    /// Key: Product Category Name (e.g., "Housing Loan", "Personal Loan")
    /// Value: Dictionary of segments for that product category
    /// </summary>
    public Dictionary<string, Dictionary<string, SegmentAveragePDTable>> AveragePDTables { get; set; } = new();

    /// <summary>
    /// Gets the average PD table for a specific product category and segment
    /// </summary>
    /// <param name="productCategory">The product category name</param>
    /// <param name="segmentName">The segment name</param>
    /// <returns>The segment average PD table if found, null otherwise</returns>
    public SegmentAveragePDTable? GetTable(string productCategory, string segmentName)
    {
        if (AveragePDTables.TryGetValue(productCategory, out Dictionary<string, SegmentAveragePDTable>? segments)
            && segments.TryGetValue(segmentName, out SegmentAveragePDTable? table))
        {
            return table;
        }
        return null;
    }

    /// <summary>
    /// Gets all product category names
    /// </summary>
    public IEnumerable<string> GetProductCategories() => AveragePDTables.Keys;

    /// <summary>
    /// Gets all segment names for a specific product category
    /// </summary>
    /// <param name="productCategory">The product category name</param>
    public IEnumerable<string> GetSegments(string productCategory)
    {
        if (AveragePDTables.TryGetValue(productCategory, out Dictionary<string, SegmentAveragePDTable>? segments))
        {
            return segments.Keys;
        }
        return Array.Empty<string>();
    }
}

/// <summary>
/// Represents the average PD table for a specific segment within a product category.
/// Contains historical PDs, interpolated PDs, and metadata.
/// </summary>
public sealed class SegmentAveragePDTable
{
    /// <summary>
    /// Gets or sets the column headers for the table
    /// Typically: ["Age Bucket", "Historical PDs", "Interpolated PDs"]
    /// </summary>
    public List<string> ColumnHeaders { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of PD data rows for each age bucket
    /// </summary>
    public List<AveragePDRow> Rows { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of age buckets
    /// </summary>
    public int BucketCount { get; set; }

    /// <summary>
    /// Gets or sets the highest maturity value for this segment
    /// Represents the maximum maturity period in months
    /// </summary>
    public int HighestMaturity { get; set; }

    /// <summary>
    /// Gets a specific row by age bucket name
    /// </summary>
    /// <param name="ageBucket">The age bucket name (e.g., "Age Bucket 1")</param>
    public AveragePDRow? GetRowByAgeBucket(string ageBucket)
    {
        return Rows.Find(r => r.AgeBucket == ageBucket);
    }

    /// <summary>
    /// Gets a specific row by index (0-based)
    /// </summary>
    /// <param name="index">The row index</param>
    public AveragePDRow? GetRowByIndex(int index)
    {
        if (index >= 0 && index < Rows.Count)
        {
            return Rows[index];
        }
        return null;
    }

    /// <summary>
    /// Gets all historical PD values
    /// </summary>
    public IEnumerable<double> GetAllHistoricalPDs()
    {
        return Rows.Select(r => r.HistoricalPD);
    }

    /// <summary>
    /// Gets all interpolated PD values
    /// </summary>
    public IEnumerable<double> GetAllInterpolatedPDs()
    {
        return Rows.Select(r => r.InterpolatedPD);
    }
}

/// <summary>
/// Represents a single row in the average PD table containing PD values for a specific age bucket.
/// </summary>
public sealed class AveragePDRow
{
    /// <summary>
    /// Gets or sets the age bucket identifier (e.g., "Age Bucket 1", "Age Bucket 2")
    /// </summary>
    public string AgeBucket { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the historical probability of default (PD) value
    /// Represents the observed/calculated PD from historical data
    /// </summary>
    public double HistoricalPD { get; set; }

    /// <summary>
    /// Gets or sets the interpolated probability of default (PD) value
    /// Represents the PD after interpolation calculations
    /// </summary>
    public double InterpolatedPD { get; set; }

    /// <summary>
    /// Gets the difference between interpolated and historical PD
    /// </summary>
    public double PDDifference => InterpolatedPD - HistoricalPD;

    /// <summary>
    /// Gets whether the PD values are identical (no interpolation was applied)
    /// </summary>
    public bool IsIdentical => Math.Abs(HistoricalPD - InterpolatedPD) < 0.001;
}
