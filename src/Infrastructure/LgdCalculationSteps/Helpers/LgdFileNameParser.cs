namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Information extracted from LGD file name
/// </summary>
/// <param name="Year">Year from filename</param>
/// <param name="Part">Part number from filename</param>
public sealed record LgdFileNameInfo(int Year, int Part);

/// <summary>
/// Helper class for parsing LGD file names and extracting metadata
/// </summary>
public static class LgdFileNameParser
{
    /// <summary>
    /// Parses a LGD file name to extract year and part information
    /// Expected formats: 
    /// - LGD_YYYY_PP (e.g., LGD_2022_01)
    /// - VC_LGD_YYYY_PP (e.g., VC_LGD_2022_01)
    /// Additional suffixes after the required pattern are ignored
    /// </summary>
    /// <param name="fileName">The file name to parse</param>
    /// <returns>Parsed file information or null if parsing fails</returns>
    public static LgdFileNameInfo? ParseFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        // Remove file extension if present
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        string[] parts = fileNameWithoutExtension.Split('_');

        // Need at least 3 parts for LGD format or 4 parts for VC_LGD format
        if (parts.Length < 3)
        {
            return null;
        }

        // Try VC_LGD format first (4+ parts)
        if (parts.Length >= 4)
        {
            LgdFileNameInfo? vcLgdResult = ParseVcLgdFormat(parts);
            if (vcLgdResult is not null)
            {
                return vcLgdResult;
            }
        }

        // Try LGD format (3+ parts)
        return ParseLgdFormat(parts);
    }

    /// <summary>
    /// Parses LGD format: LGD_YYYY_PP (ignores additional suffixes)
    /// </summary>
    /// <param name="parts">File name parts split by underscore</param>
    /// <returns>Parsed file information or null if invalid</returns>
    private static LgdFileNameInfo? ParseLgdFormat(string[] parts)
    {
        if (parts.Length < 3 || !string.Equals(parts[0], "LGD", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return TryParseYearAndPart(parts[1], parts[2]);
    }

    /// <summary>
    /// Parses VC_LGD format: VC_LGD_YYYY_PP (ignores additional suffixes)
    /// </summary>
    /// <param name="parts">File name parts split by underscore</param>
    /// <returns>Parsed file information or null if invalid</returns>
    private static LgdFileNameInfo? ParseVcLgdFormat(string[] parts)
    {
        if (parts.Length < 4 ||
            !string.Equals(parts[0], "VC", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(parts[1], "LGD", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return TryParseYearAndPart(parts[2], parts[3]);
    }

    /// <summary>
    /// Attempts to parse year and part from string values
    /// </summary>
    /// <param name="yearString">Year string to parse</param>
    /// <param name="partString">Part string to parse</param>
    /// <returns>Parsed file information or null if invalid</returns>
    private static LgdFileNameInfo? TryParseYearAndPart(string yearString, string partString)
    {
        if (!int.TryParse(yearString, out int year) || !int.TryParse(partString, out int part))
        {
            return null;
        }

        // Validate year is reasonable (between 2000 and 2100)
        if (year < 2000 || year > 2100)
        {
            return null;
        }

        // Validate part is reasonable (between 1 and 12 for months)
        if (part < 1 || part > 12)
        {
            return null;
        }

        return new LgdFileNameInfo(year, part);
    }

    /// <summary>
    /// Extracts the period string from parsed file name information
    /// </summary>
    /// <param name="info">File name information</param>
    /// <returns>Period string (e.g., "2022_01")</returns>
    public static string ExtractPeriod(LgdFileNameInfo info)
    {
        return $"{info.Year}_{info.Part:D2}";
    }

    /// <summary>
    /// Creates a file identifier for lookup purposes
    /// </summary>
    /// <param name="info">File name information</param>
    /// <returns>File identifier string</returns>
    public static string CreateFileIdentifier(LgdFileNameInfo info)
    {
        return $"LGD_{info.Year}_{info.Part:D2}";
    }
}