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
        if(Type == null)
        {
            return false;
        }

        string type = Type.Trim();

        if (type.Equals(FinalBucketTypes.Worst, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (type.Equals(FinalBucketTypes.Percentage, StringComparison.OrdinalIgnoreCase))
        {
            return Percentage is > 0 and <= 100;
        }

        if (type.Equals(FinalBucketTypes.None, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}

public static class FinalBucketTypes
{
    public const string Worst = "WORST";
    public const string Percentage = "PERCENTAGE";
    public const string None = "NONE";
}

