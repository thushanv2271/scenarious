using Application.Abstractions.Configuration;
using Application.Models;

namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Helper class for constructing LGD file paths based on type
/// </summary>
internal static class LgdFilePathHelper
{
    /// <summary>
    /// Gets the LGD files path based on the type parameter or returns the full configuration path
    /// </summary>
    /// <param name="appConfiguration">Application configuration containing the ProcessedFilePaths.LGD_ClosedFacility</param>
    /// <param name="type">Type of files (Yearly, Quarterly, Monthly)</param>
    /// <returns>Full path to LGD files directory</returns>
    internal static string GetLgdFilesPath(IAppConfiguration appConfiguration, string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            // Return full path from configuration if no type provided
            return appConfiguration.LGD_ClosedFacilityFilesPath;
        }

        // Get base path from configuration and construct new path based on type
        string configPath = appConfiguration.LGD_ClosedFacilityFilesPath;
        string basePath = Path.GetDirectoryName(configPath) ?? string.Empty;

        // Parse the type and get the suffix
        PDFileType fileType = PDFileTypeExtensions.Parse(type);
        string suffix = fileType.GetSuffix();

        return Path.Combine(basePath, $"LGD-Files-{suffix}");
    }

    /// <summary>
    /// Gets the VC LGD files path based on the type parameter or returns the full configuration path
    /// </summary>
    /// <param name="appConfiguration">Application configuration containing the ProcessedFilePaths.LGD_OpenFacility</param>
    /// <param name="type">Type of files (Yearly, Quarterly, Monthly)</param>
    /// <returns>Full path to VC LGD files directory</returns>
    internal static string GetVCLgdFilesPath(IAppConfiguration appConfiguration, string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            // Return full path from configuration if no type provided
            return appConfiguration.LGD_OpenFacilityFilesPath;
        }

        // Get base path from configuration and construct new path based on type
        string configPath = appConfiguration.LGD_OpenFacilityFilesPath;
        string basePath = Path.GetDirectoryName(configPath) ?? string.Empty;

        // Parse the type and get the suffix
        PDFileType fileType = PDFileTypeExtensions.Parse(type);
        string suffix = fileType.GetSuffix();

        return Path.Combine(basePath, $"VC-LGD-Files-{suffix}");
    }
}