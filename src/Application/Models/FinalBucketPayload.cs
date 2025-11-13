namespace Application.Models;

/// <summary>
/// Represents the configuration for final bucket calculation
/// </summary>
public sealed record FinalBucketPayload
{
    /// <summary>
    /// The type of final bucket calculation to perform
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// The percentage threshold for percentage-based final bucket calculation
    /// Valid only when Type is "percentage"
    /// </summary>
    public decimal? Percentage { get; init; }

    /// <summary>
    /// Validates that the payload configuration is valid
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return Type switch
        {
            "worst" => true,
            "percentage" => Percentage.HasValue && Percentage.Value > 0 && Percentage.Value <= 100,
            _ => false
        };
    }
}
