namespace Application.Models;

/// <summary>
/// Represents the time configuration for PD calculation
/// </summary>
public sealed record TimeConfig
{
    /// <summary>
    /// The frequency of the time period (Yearly, Quarterly, Monthly)
    /// </summary>
    public string Frequency { get; init; } = string.Empty;

    /// <summary>
    /// Validates that the time configuration is valid
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return Frequency.ToUpperInvariant() switch
        {
            "YEARLY" or "QUARTERLY" or "MONTHLY" => true,
            _ => false
        };
    }
}