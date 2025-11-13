namespace Application.DTOs.PD;

/// <summary>
/// Represents product category-specific migration data containing segments for that product category.
/// This class is used only in memory and not persisted in the database.
/// </summary>
public sealed class PdProductCategoryMigrationData
{
    /// <summary>
    /// Gets or sets the product category name
    /// </summary>
    public string ProductCategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of segment-specific migration data for this product category
    /// </summary>
    public IReadOnlyList<PdSegmentMigrationData> Segments { get; set; } = Array.Empty<PdSegmentMigrationData>();
}

/// <summary>
/// Represents a dataset for PD migration matrix containing data for a specific pair of periods.
/// Now supports product category-wise analysis with nested segments.
/// This class is used only in memory and not persisted in the database.
/// </summary>
public sealed class PdMigrationDataset
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
    /// Gets or sets the collection of product category-specific migration data for this period pair
    /// </summary>
    public IReadOnlyList<PdProductCategoryMigrationData> ProductCategories { get; set; } = Array.Empty<PdProductCategoryMigrationData>();
}
