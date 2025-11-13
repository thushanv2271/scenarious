using Domain.PDCalculation;
using System.Text.RegularExpressions;

namespace Infrastructure.PDCalculationSteps.Helpers;

/// <summary>
/// Helper class for parsing file names and extracting metadata
/// </summary>
public static class FileNameParser
{
    /// <summary>
    /// Parses a PD file name to extract frequency, part, and date information
    /// </summary>
    /// <param name="fileName">The file name to parse</param>
    /// <returns>Parsed file information or null if parsing fails</returns>
    public static FileNameInfo? ParseFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        // Remove file extension
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        // Pattern for yearly: PD_year_part (e.g., PD_2022_01)
        string yearlyPattern = @"^PD_(\d{4})_(\d+)$";
        Match yearlyMatch = Regex.Match(nameWithoutExtension, yearlyPattern);
        if (yearlyMatch.Success &&
            int.TryParse(yearlyMatch.Groups[1].Value, out int year) &&
            int.TryParse(yearlyMatch.Groups[2].Value, out int part))
        {
            return new FileNameInfo(FrequencyType.Yearly, year, 0, 0, part);
        }

        // Pattern for monthly: PD_year-month_part (e.g., PD_2024-01_01)
        string monthlyPattern = @"^PD_(\d{4})-(\d{2})_(\d+)$";
        Match monthlyMatch = Regex.Match(nameWithoutExtension, monthlyPattern);
        if (monthlyMatch.Success &&
            int.TryParse(monthlyMatch.Groups[1].Value, out year) &&
            int.TryParse(monthlyMatch.Groups[2].Value, out int month) &&
            int.TryParse(monthlyMatch.Groups[3].Value, out part))
        {
            return new FileNameInfo(FrequencyType.Monthly, year, month, 0, part);
        }

        // Pattern for quarterly: PD_yearQuarter_part (e.g., PD_2024Q1_01)
        string quarterlyPattern = @"^PD_(\d{4})Q(\d)_(\d+)$";
        Match quarterlyMatch = Regex.Match(nameWithoutExtension, quarterlyPattern);
        if (quarterlyMatch.Success &&
            int.TryParse(quarterlyMatch.Groups[1].Value, out year) &&
            int.TryParse(quarterlyMatch.Groups[2].Value, out int quarter) &&
            int.TryParse(quarterlyMatch.Groups[3].Value, out part))
        {
            return new FileNameInfo(FrequencyType.Quarterly, year, 0, quarter, part);
        }

        return null;
    }

    /// <summary>
    /// Creates a file identifier for quarter ended date lookup
    /// </summary>
    /// <param name="info">File name information</param>
    /// <returns>File identifier string</returns>
    public static string CreateFileIdentifier(FileNameInfo info)
    {
        return info.Frequency switch
        {
            FrequencyType.Yearly => $"PD_{info.Year}",
            FrequencyType.Monthly => $"PD_{info.Year}-{info.Month:D2}",
            FrequencyType.Quarterly => $"PD_{info.Year}Q{info.Quarter}",
            _ => throw new ArgumentException($"Unknown frequency type: {info.Frequency}")
        };
    }

    /// <summary>
    /// Extracts the period string from parsed file name information
    /// </summary>
    /// <param name="info">File name information</param>
    /// <returns>Period string for database storage</returns>
    public static string ExtractPeriod(FileNameInfo info)
    {
        return info.Frequency switch
        {
            FrequencyType.Yearly => $"{info.Year}",
            FrequencyType.Monthly => $"{info.Year}-{info.Month:D2}",
            FrequencyType.Quarterly => $"{info.Year}Q{info.Quarter}",
            _ => throw new ArgumentException($"Unknown frequency type: {info.Frequency}")
        };
    }
}

/// <summary>
/// Information parsed from a PD file name
/// </summary>
/// <param name="Frequency">The frequency type of the file</param>
/// <param name="Year">The year from the file name</param>
/// <param name="Month">The month from the file name (0 if not applicable)</param>
/// <param name="Quarter">The quarter from the file name (0 if not applicable)</param>
/// <param name="Part">The part number from the file name</param>
public sealed record FileNameInfo(
    FrequencyType Frequency,
    int Year,
    int Month,
    int Quarter,
    int Part);
