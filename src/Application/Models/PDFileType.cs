namespace Application.Models;

/// <summary>
/// Represents the type of PD files which determines the directory suffix
/// </summary>
public enum PDFileType
{
    /// <summary>
    /// Yearly PD files (suffix: Y)
    /// </summary>
    Yearly,

    /// <summary>
    /// Quarterly PD files (suffix: Q)
    /// </summary>
    Quarterly,

    /// <summary>
    /// Monthly PD files (suffix: M)
    /// </summary>
    Monthly
}

/// <summary>
/// Extension methods for PDFileType
/// </summary>
public static class PDFileTypeExtensions
{
    /// <summary>
    /// Gets the directory suffix for the PD file type
    /// </summary>
    /// <param name="fileType">The PD file type</param>
    /// <returns>The directory suffix (Y, Q, or M)</returns>
    public static string GetSuffix(this PDFileType fileType)
    {
        return fileType switch
        {
            PDFileType.Yearly => "Y",
            PDFileType.Quarterly => "Q",
            PDFileType.Monthly => "M",
            _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, "Invalid PD file type")
        };
    }

    /// <summary>
    /// Parses a string to PDFileType
    /// </summary>
    /// <param name="type">The type string</param>
    /// <returns>The corresponding PDFileType</returns>
    /// <exception cref="ArgumentException">Thrown when the type is invalid</exception>
    public static PDFileType Parse(string type)
    {
        return type.ToUpperInvariant() switch
        {
            "YEARLY" => PDFileType.Yearly,
            "QUARTERLY" => PDFileType.Quarterly,
            "MONTHLY" => PDFileType.Monthly,
            _ => throw new ArgumentException($"Invalid type '{type}'. Valid values are: Yearly, Quarterly, Monthly", nameof(type))
        };
    }
}