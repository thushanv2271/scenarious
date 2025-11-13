namespace Application.Models.PDSummary;

/// <summary>
/// Response model for migration matrices
/// </summary>
public sealed class MigrationMatricesAndSummaryResponse

{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public List<MigrationMatrix> Matrices { get; set; } = new();
}


/// <summary>
/// Individual migration matrix containing product categories
/// </summary>
public sealed class MigrationMatrix
{
    public string PeriodNMinus1 { get; set; } = string.Empty;
    public string PeriodN { get; set; } = string.Empty;
    public List<ProductCategory> ProductCategories { get; set; } = new();
}

/// <summary>
/// Product category containing segments
/// </summary>
public sealed class ProductCategory
{
    public string ProductCategoryName { get; set; } = string.Empty;
    public List<Segment> Segments { get; set; } = new();
}

/// <summary>
/// Individual segment within a product category
/// </summary>
public sealed class Segment
{
    public string SegmentName { get; set; } = string.Empty;
    public string ComparisonPeriod { get; set; } = string.Empty;
    public List<string> Buckets { get; set; } = new();
    public int BucketCount { get; set; }
    public bool HasPercentageData { get; set; }
    public MatrixCounts Counts { get; set; } = new();
    public MatrixPercentages? Percentages { get; set; }
}

/// <summary>
/// Matrix count data
/// </summary>
public sealed class MatrixCounts
{
    public List<List<int>> Matrix { get; set; } = new();
    public List<int> ExitCounts { get; set; } = new();
    public List<int> RowTotals { get; set; } = new();
}

/// <summary>
/// Matrix percentage data
/// </summary>
public sealed class MatrixPercentages
{
    public List<List<double?>> Matrix { get; set; } = new();
    public List<double?> ExitPercentages { get; set; } = new();
    public List<double?> GrandTotal { get; set; } = new();
    public List<double>? PD_summary { get; set; } = new();
    public List<double>? avg_PD { get; set; } = new();
}

/// <summary>
/// Response model for PD average summary
/// </summary>
public sealed class PDAvgSummaryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public List<MigrationMatrix> Matrices { get; set; } = new();
}




/// <summary>
/// Average PD table structure
/// </summary>
public sealed class AveragePDTable
{
    public List<string> ColumnHeaders { get; set; } = new() { "Age Bucket", "Historical PDs", "Interpolated PDs" };
    public List<AveragePDRow> Rows { get; set; } = new();
    public int BucketCount { get; set; }
    public int? HighestMaturity { get; set; }
}

/// <summary>
/// Individual row in Average PD table
/// </summary>
public sealed class AveragePDRow
{
    public string AgeBucket { get; set; } = string.Empty;
    public double? HistoricalPD { get; set; }
    public double? InterpolatedPD { get; set; }
}
