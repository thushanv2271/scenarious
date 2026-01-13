using Application.Models;
using Domain.PDCalculation;
using System.Text.RegularExpressions;

namespace Infrastructure.PDCalculationSteps.Helpers;

/// <summary>
/// Helper class for identifying latest portfolio files based on naming patterns
/// </summary>
public static class PortfolioHelper
{
    /// <summary>
    /// Identifies the latest portfolio files from a list of file paths based on frequency type
    /// </summary>
    /// <param name="filePaths">List of file paths to analyze</param>
    /// <param name="frequency">Frequency type to determine parsing pattern</param>
    /// <returns>List of latest portfolio file names</returns>
    public static List<string> GetLatestPortfolioFiles(IEnumerable<string> filePaths, FrequencyType frequency)
    {
        var fileNames = filePaths.Select(Path.GetFileName).Where(f => f is not null).Cast<string>().ToList();

        return frequency switch
        {
            FrequencyType.Yearly => GetLatestYearlyPortfolioFiles(fileNames),
            FrequencyType.Monthly => GetLatestMonthlyPortfolioFiles(fileNames),
            FrequencyType.Quarterly => GetLatestQuarterlyPortfolioFiles(fileNames),
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Gets latest yearly portfolio files (pattern: PD_YYYY_XX)
    /// </summary>
    /// <param name="fileNames">List of file names</param>
    /// <returns>Latest yearly portfolio files</returns>
    private static List<string> GetLatestYearlyPortfolioFiles(List<string> fileNames)
    {
        // Pattern: PD_2025_01, PD_2025_02, etc.
        string yearlyPattern = @"^PD_(\d{4})_(\d{2})";
        Regex regex = new(yearlyPattern, RegexOptions.IgnoreCase);

        List<(string FileName, int Year, int Sequence)> parsedFiles = new();

        foreach (string fileName in fileNames)
        {
            Match match = regex.Match(fileName);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int year) &&
                int.TryParse(match.Groups[2].Value, out int sequence))
            {
                parsedFiles.Add((fileName, year, sequence));
            }
        }

        if (parsedFiles.Count == 0)
        {
            return new List<string>();
        }

        // Find the highest year
        int maxYear = parsedFiles.Max(f => f.Year);

        // Return all files from the highest year
        return parsedFiles
            .Where(f => f.Year == maxYear)
            .Select(f => f.FileName)
            .ToList();
    }

    /// <summary>
    /// Gets latest monthly portfolio files (pattern: PD_YYYY-MM_XX)
    /// </summary>
    /// <param name="fileNames">List of file names</param>
    /// <returns>Latest monthly portfolio files</returns>
    private static List<string> GetLatestMonthlyPortfolioFiles(List<string> fileNames)
    {
        // Pattern: PD_2024-01_01, PD_2024-12_01, etc.
        string monthlyPattern = @"^PD_(\d{4})-(\d{2})_(\d{2})";
        Regex regex = new(monthlyPattern, RegexOptions.IgnoreCase);

        List<(string FileName, int Year, int Month, int Sequence)> parsedFiles = new();

        foreach (string fileName in fileNames)
        {
            Match match = regex.Match(fileName);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int year) &&
                int.TryParse(match.Groups[2].Value, out int month) &&
                int.TryParse(match.Groups[3].Value, out int sequence))
            {
                parsedFiles.Add((fileName, year, month, sequence));
            }
        }

        if (parsedFiles.Count == 0)
        {
            return new List<string>();
        }

        // Find the highest year
        int maxYear = parsedFiles.Max(f => f.Year);

        // From the highest year, find the highest month
        int maxMonth = parsedFiles
            .Where(f => f.Year == maxYear)
            .Max(f => f.Month);

        // Return all files from the highest year and month
        return parsedFiles
            .Where(f => f.Year == maxYear && f.Month == maxMonth)
            .Select(f => f.FileName)
            .ToList();
    }

    /// <summary>
    /// Gets latest quarterly portfolio files (pattern: PD_YYYYQX_XX)
    /// </summary>
    /// <param name="fileNames">List of file names</param>
    /// <returns>Latest quarterly portfolio files</returns>
    private static List<string> GetLatestQuarterlyPortfolioFiles(List<string> fileNames)
    {
        // Pattern: PD_2025Q4_01, PD_2025Q4_02, etc.
        string quarterlyPattern = @"^PD_(\d{4})Q([1-4])_(\d{2})";
        Regex regex = new(quarterlyPattern, RegexOptions.IgnoreCase);

        List<(string FileName, int Year, int Quarter, int Sequence)> parsedFiles = new();

        foreach (string fileName in fileNames)
        {
            Match match = regex.Match(fileName);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int year) &&
                int.TryParse(match.Groups[2].Value, out int quarter) &&
                int.TryParse(match.Groups[3].Value, out int sequence))
            {
                parsedFiles.Add((fileName, year, quarter, sequence));
            }
        }

        if (parsedFiles.Count == 0)
        {
            return new List<string>();
        }

        // Find the highest year
        int maxYear = parsedFiles.Max(f => f.Year);

        // From the highest year, find the highest quarter
        int maxQuarter = parsedFiles
            .Where(f => f.Year == maxYear)
            .Max(f => f.Quarter);

        // Return all files from the highest year and quarter
        return parsedFiles
            .Where(f => f.Year == maxYear && f.Quarter == maxQuarter)
            .Select(f => f.FileName)
            .ToList();
    }

    /// <summary>
    /// Determines if a file name is part of the latest portfolio
    /// </summary>
    /// <param name="fileName">File name to check</param>
    /// <param name="latestPortfolioFiles">List of latest portfolio file names</param>
    /// <returns>True if the file is part of the latest portfolio</returns>
    public static bool IsLatestPortfolioFile(string fileName, List<string> latestPortfolioFiles)
    {
        return latestPortfolioFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase);
    }
}