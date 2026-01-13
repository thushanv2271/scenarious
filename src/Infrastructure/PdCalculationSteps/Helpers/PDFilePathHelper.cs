using Application.Files.Common;
using Application.Models;

namespace Infrastructure.PDCalculationSteps.Helpers;

/// <summary>
/// Helper class for constructing PD file paths based on type
/// </summary>
internal static class PDFilePathHelper
{
    /// <summary>
    /// Gets the PD files path based on the type parameter or returns the processed files path
    /// </summary>
    /// <param name="processedFilePathsOptions">Processed file paths configuration containing the base PD path</param>
    /// <param name="type">Type of files (Yearly, Quarterly, Monthly)</param>
    /// <returns>Full path to PD files directory</returns>
    internal static string GetPDFilesPath(ProcessedFilePathsOptions processedFilePathsOptions, string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            // Return PD path from configuration if no type provided
            return processedFilePathsOptions.PD;
        }

        // Get base path from configuration and construct new path based on type
        string configPath = processedFilePathsOptions.PD;
        string basePath = Path.GetDirectoryName(configPath) ?? string.Empty;

        // Parse the type and get the suffix
        PDFileType fileType = PDFileTypeExtensions.Parse(type);
        string suffix = fileType.GetSuffix();

        return Path.Combine(basePath, $"PD-Files-{suffix}");
    }
}