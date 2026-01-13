using System.Text.Json.Serialization;

namespace Application.Models;

/// <summary>
/// Represents the type of LGD calculation to perform
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LgdCalculationType
{
    /// <summary>
    /// Standard LGD calculation using ProcessedFilePaths.LGD
    /// </summary>
    LGD = 0,

    /// <summary>
    /// VC LGD calculation using ProcessedFilePaths.VC_LGD
    /// </summary>
    VC_LGD = 1
}

/// <summary>
/// Helper class for converting between string and LgdCalculationType enum values
/// </summary>
public static class LgdCalculationTypeHelper
{
    /// <summary>
    /// Parses a string value to LgdCalculationType enum
    /// </summary>
    /// <param name="value">The string value to parse ("LGD", "VC_LGD", "0", "1")</param>
    /// <returns>Corresponding LgdCalculationType enum value</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not recognized</exception>
    public static LgdCalculationType Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return LgdCalculationType.LGD; // Default
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "LGD" => LgdCalculationType.LGD,
            "VC_LGD" => LgdCalculationType.VC_LGD,
            "0" => LgdCalculationType.LGD,
            "1" => LgdCalculationType.VC_LGD,
            _ => throw new ArgumentException($"Invalid LgdCalculationType value: '{value}'. Valid values are: 'LGD', 'VC_LGD', '0', '1'")
        };
    }

    /// <summary>
    /// Converts LgdCalculationType enum to string
    /// </summary>
    /// <param name="calculationType">The enum value to convert</param>
    /// <returns>String representation of the calculation type</returns>
    public static string ToString(LgdCalculationType calculationType)
    {
        return calculationType switch
        {
            LgdCalculationType.LGD => "LGD",
            LgdCalculationType.VC_LGD => "VC_LGD",
            _ => "LGD"
        };
    }
}