namespace Application.DTOs.PD;

/// <summary>
/// Represents segment-specific migration data with comparison period information.
/// This class is used only in memory and not persisted in the database.
/// </summary>
public sealed class PdSegmentMigrationData
{
    /// <summary>
    /// Gets or sets the segment name
    /// </summary>
    public string SegmentName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comparison period for this segment
    /// </summary>
    public string ComparisonPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of migration rows for this segment
    /// </summary>
    public IReadOnlyList<PdMigrationRowDto> Rows { get; set; } = Array.Empty<PdMigrationRowDto>();
}
