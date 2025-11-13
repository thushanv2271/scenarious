namespace Application.DTOs.PD;

/// <summary>
/// DTO for PD calculation migration row data.
/// This DTO is used only in memory and not persisted in the database.
/// </summary>
public sealed class PdMigrationRowDto
{
    /// <summary>
    /// Gets or sets the customer identifier
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the facility identifier
    /// </summary>
    public string FacilityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product category for this migration row
    /// </summary>
    public string ProductCategory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the segment for this migration row
    /// </summary>
    public string Segment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bucket status for N-1 period
    /// </summary>
    public string BucketStatusNMinus1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bucket status for N period
    /// </summary>
    public string BucketStatusN { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the finalized bucket status
    /// </summary>
    public string FinalizedBucketStatus { get; set; } = string.Empty;
}
