namespace Application.DTOs.PD;

/// <summary>
/// Root DTO for PD extrapolation results containing all product categories and their segments.
/// Used in memory for risk modeling calculations.
/// </summary>
public sealed class PdExtrapolationResultDto
{
    /// <summary>
    /// Gets or sets the collection of product category extrapolation results
    /// </summary>
    public List<PdExtrapolationCategoryDto> ProductCategories { get; set; } = new();
}

/// <summary>
/// Represents PD extrapolation results for a specific product category.
/// Contains all segments within this product category.
/// </summary>
public sealed class PdExtrapolationCategoryDto
{
    /// <summary>
    /// Gets or sets the product category name
    /// </summary>
    public string ProductCategory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of segment extrapolation results for this product category
    /// </summary>
    public List<PdExtrapolationSegmentDto> Segments { get; set; } = new();
}

/// <summary>
/// Represents PD extrapolation results for a specific segment within a product category.
/// </summary>
public sealed class PdExtrapolationSegmentDto
{
    /// <summary>
    /// Gets or sets the segment name
    /// </summary>
    public string Segment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the summary containing all three extrapolation methods
    /// </summary>
    public PdExtrapolationSummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Aggregates all three PD extrapolation methods for a specific segment.
/// </summary>
public sealed class PdExtrapolationSummaryDto
{
    /// <summary>
    /// Gets or sets Method 1 results (Geometric Approach)
  /// </summary>
  public PdExtrapolationMethod1Dto Method1 { get; set; } = new();

    /// <summary>
    /// Gets or sets Method 2 results (Geometric and Lognormal Approach)
  /// </summary>
    public PdExtrapolationMethod2Dto Method2 { get; set; } = new();

    /// <summary>
    /// Gets or sets Method 3 results (Survival Rate Approach)
    /// </summary>
    public PdExtrapolationMethod3Dto Method3 { get; set; } = new();
}

/// <summary>
/// Method 1 - Geometric Approach for PD extrapolation.
/// Contains three tables: extrapolated cumulative PDs before EFA, after EFA, and marginal PDs after EFA.
/// </summary>
public sealed class PdExtrapolationMethod1Dto
{
    /// <summary>
    /// Gets or sets extrapolated cumulative PDs before adjusting EFA (Extremely Forward-looking Adjustment)
    /// </summary>
    public PdExtrapolationTableDto ExtrapolatedCumulativePdsBeforeEfa { get; set; } = new();

    /// <summary>
    /// Gets or sets extrapolated cumulative PDs after EFA adjustment
    /// </summary>
    public PdExtrapolationTableDto ExtrapolatedCumulativePdsAfterEfa { get; set; } = new();

    /// <summary>
    /// Gets or sets marginal PDs after EFA adjustment
    /// </summary>
    public PdExtrapolationTableDto MarginalPdsAfterEfa { get; set; } = new();
}

/// <summary>
/// Method 2 - Geometric and Lognormal Approach for PD extrapolation.
/// Contains three tables: extrapolated cumulative PDs before EFA, after EFA, and marginal PDs after EFA.
/// </summary>
public sealed class PdExtrapolationMethod2Dto
{
    /// <summary>
    /// Gets or sets extrapolated cumulative PDs before adjusting EFA (Extremely Forward-looking Adjustment)
    /// </summary>
    public PdExtrapolationTableDto ExtrapolatedCumulativePdsBeforeEfa { get; set; } = new();

    /// <summary>
    /// Gets or sets extrapolated cumulative PDs after EFA adjustment
    /// </summary>
    public PdExtrapolationTableDto ExtrapolatedCumulativePdsAfterEfa { get; set; } = new();

    /// <summary>
    /// Gets or sets marginal PDs after EFA adjustment
    /// </summary>
    public PdExtrapolationTableDto MarginalPdsAfterEfa { get; set; } = new();
}

/// <summary>
/// Method 3 - Survival Rate Approach for PD extrapolation.
/// Contains three tables: EFA adjusted PDs, survival rates, and marginal PDs.
/// </summary>
public sealed class PdExtrapolationMethod3Dto
{
    /// <summary>
    /// Gets or sets EFA adjusted PDs
    /// </summary>
    public PdExtrapolationTableDto EfaAdjustedPds { get; set; } = new();

    /// <summary>
    /// Gets or sets survival rates
    /// </summary>
    public PdExtrapolationTableDto SurvivalRates { get; set; } = new();

    /// <summary>
    /// Gets or sets marginal PDs
    /// </summary>
    public PdExtrapolationTableDto MarginalPds { get; set; } = new();
}

/// <summary>
/// Represents a table containing PD values organized by age buckets and years.
/// Used for various extrapolation calculations in risk modeling.
/// </summary>
public sealed class PdExtrapolationTableDto
{
    /// <summary>
    /// Gets or sets the title of the table
    /// </summary>
 public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column headers (typically year labels or time periods)
    /// </summary>
  public List<string> ColumnHeaders { get; set; } = new();

    /// <summary>
    /// Gets or sets the rows containing age bucket data and PD values by year
    /// </summary>
    public List<PdExtrapolationRowDto> Rows { get; set; } = new();
}

/// <summary>
/// Represents a single row in a PD extrapolation table.
/// Contains an age bucket identifier and corresponding PD values for each year/period.
/// </summary>
public sealed class PdExtrapolationRowDto
{
    /// <summary>
    /// Gets or sets the age bucket label (e.g., "0-30 days", "31-60 days", etc.)
    /// </summary>
 public string AgeBucket { get; set; } = string.Empty;

/// <summary>
    /// Gets or sets the PD values mapped by year/period.
    /// Key: Year or period index (e.g., 1, 2, 3... representing Year 1, Year 2, etc.)
    /// Value: The PD value (decimal) for that year/period
    /// </summary>
    public Dictionary<int, decimal?> PdValuesByYear { get; set; } = new();
}
