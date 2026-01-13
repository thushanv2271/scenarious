using Application.Files.Common;
using Domain.CollectiveImpairment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Files.Services;

/// <summary>
/// Service for moving processed files to appropriate destinations based on collective impairment type.
/// </summary>
public interface IFileMovementService
{
    /// <summary>
    /// Moves successfully processed files to the appropriate destination based on collective impairment type.
    /// </summary>
    /// <param name="files">Array of file paths to move.</param>
    /// <param name="parameterType">The collective impairment type (PD or LGD).</param>
    /// <param name="timePeriod">The time period for file naming (e.g., "2024-Q1", "2024-03", "2024").</param>
    /// <param name="configJson">Configuration JSON containing frequency information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with details.</returns>
    Task<Result<FileMovementResult>> MoveFilesAsync(
        string[] files, 
        ParameterType parameterType,
        string timePeriod,
        string configJson,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of file movement operation.
/// </summary>
/// <param name="TotalFiles">Total number of files that were attempted to be moved.</param>
/// <param name="SuccessfulMoves">Number of files successfully moved.</param>
/// <param name="FailedMoves">Number of files that failed to move.</param>
/// <param name="Errors">List of errors encountered during the move operation.</param>
/// <param name="MovedFiles">Dictionary mapping original file paths to destination paths for successfully moved files.</param>
public sealed record FileMovementResult(
    int TotalFiles,
    int SuccessfulMoves,
    int FailedMoves,
    List<string> Errors,
    Dictionary<string, string> MovedFiles);

/// <summary>
/// Implementation of file movement service.
/// Moves files from hierarchical pending folders to FLAT processed folder for easier next-step processing.
/// </summary>
public sealed class FileMovementService(
    IOptions<ProcessedFilePathsOptions> processedFilePathsOptions,
    ILogger<FileMovementService> logger) : IFileMovementService
{
    private readonly ProcessedFilePathsOptions _options = processedFilePathsOptions.Value;

    public async Task<Result<FileMovementResult>> MoveFilesAsync(
        string[] files, 
        ParameterType parameterType,
        string timePeriod,
        string configJson,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var movedFiles = new Dictionary<string, string>();
        int successfulMoves = 0;
        int failedMoves = 0;

        try
        {
            // Get flat destination path (e.g., /data/PD/processed) - NO hierarchical structure
            string destinationPath = GetDestinationPath(parameterType);
            string fullDestinationPath = Environment.ExpandEnvironmentVariables(destinationPath);

            // Ensure flat destination directory exists
            if (!Directory.Exists(fullDestinationPath))
            {
                try
                {
                    Directory.CreateDirectory(fullDestinationPath);
                    logger.LogInformation("Created flat destination directory: {Path}", fullDestinationPath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create destination directory: {Path}", fullDestinationPath);
                    return Result.Failure<FileMovementResult>(Error.Problem(
                        "FileMovement.DirectoryCreationFailed",
                        $"Failed to create destination directory '{fullDestinationPath}': {ex.Message}"));
                }
            }

            foreach (string filePath in files)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        string error = $"Source file does not exist: {filePath}";
                        errors.Add(error);
                        failedMoves++;
                        logger.LogWarning("{Error}", error);
                        continue;
                    }

                    // Generate new filename with standardized format
                    string newFileName = GenerateNewFileName(filePath, parameterType, timePeriod, configJson);
                    
                    // Combine with FLAT destination path (no hierarchical subdirectories)
                    string destinationFilePath = Path.Combine(fullDestinationPath, newFileName);

                    // Ensure destination directory exists (should already exist from above)
                    string? destinationDirectory = Path.GetDirectoryName(destinationFilePath);
                    if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    // Perform the file move
                    await Task.Run(() => File.Move(filePath, destinationFilePath, true), cancellationToken);
                    
                    movedFiles[filePath] = destinationFilePath;
                    successfulMoves++;
                    
                    logger.LogInformation("Successfully moved file from {Source} to {Destination}", 
                        filePath, destinationFilePath);
                }
                catch (Exception ex)
                {
                    string error = $"Failed to move file '{filePath}': {ex.Message}";
                    errors.Add(error);
                    failedMoves++;
                    logger.LogError(ex, "Failed to move file from {Source}", filePath);
                }
            }

            var result = new FileMovementResult(
                TotalFiles: files.Length,
                SuccessfulMoves: successfulMoves,
                FailedMoves: failedMoves,
                Errors: errors,
                MovedFiles: movedFiles);

            logger.LogInformation("File movement completed. Total: {Total}, Success: {Success}, Failed: {Failed}, Destination: {Path}", 
                result.TotalFiles, result.SuccessfulMoves, result.FailedMoves, fullDestinationPath);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during file movement operation");
            return Result.Failure<FileMovementResult>(Error.Problem(
                "FileMovement.UnexpectedError",
                $"Unexpected error during file movement: {ex.Message}"));
        }
    }

    private string GetDestinationPath(ParameterType parameterType)
    {
        return parameterType switch
        {
            ParameterType.PD => _options.PD,
            ParameterType.LGD => _options.LGD,
            _ => throw new ArgumentException($"Unsupported parameter type: {parameterType}", nameof(parameterType))
        };
    }

    private string GenerateNewFileName(string sourceFilePath, ParameterType parameterType, string timePeriod, string configJson)
    {
        string originalFileName = Path.GetFileNameWithoutExtension(sourceFilePath);
        string extension = Path.GetExtension(sourceFilePath);
        
        // Extract file number from original filename (e.g., "2024_Q1_File_1" -> "1")
        string fileNumber = ExtractFileNumber(originalFileName);
        
        // Get frequency from config and format time period accordingly
        string formattedTimePeriod = FormatTimePeriodForFileName(timePeriod, configJson);
        
        // Generate new filename: {parameterType}_{formattedTimePeriod}_{fileNumber}{extension}
        return $"{parameterType}_{formattedTimePeriod}_{fileNumber}{extension}";
    }

    private string ExtractFileNumber(string fileName)
    {
        // Look for patterns like "PD_2025_01_..." or similar - extract the segment after time period
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(fileName, @"_(\d{8})_(\d{6})_([a-f0-9]{8})");
        if (match.Success)
        {
            // This is a timestamped filename like "Portfolio_Data_20260111_172322_d10c84ce"
            // Use the short GUID part as the file number
            return match.Groups[3].Value;
        }
        
        // Look for patterns like "PD_2025_01_..." - extract the third segment after second underscore
        match = System.Text.RegularExpressions.Regex.Match(fileName, @"^[^_]+_[^_]+_(\d+)");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        
        // Look for patterns like "File_1", "File1" anywhere in the string
        match = System.Text.RegularExpressions.Regex.Match(fileName, @"File_?(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        
        // Fallback: look for any number at the end
        match = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d+)$");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        
        // If no number found, use a hash of the filename for uniqueness
        int hash = Math.Abs(fileName.GetHashCode());
        return hash.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private string FormatTimePeriodForFileName(string timePeriod, string configJson)
    {
        try
        {
            var configDoc = System.Text.Json.JsonDocument.Parse(configJson);
            System.Text.Json.JsonElement root = configDoc.RootElement;

            if (!root.TryGetProperty("pdSetup", out System.Text.Json.JsonElement pdSetup) ||
                !pdSetup.TryGetProperty("frequency", out System.Text.Json.JsonElement frequencyElement))
            {
                return timePeriod; // Fallback to original if config parsing fails
            }

            string frequency = frequencyElement.GetString()?.ToUpperInvariant() ?? "";

            return frequency switch
            {
                "YEARLY" => FormatYearlyTimePeriod(timePeriod),
                "QUARTERLY" => FormatQuarterlyTimePeriod(timePeriod),
                "MONTHLY" => FormatMonthlyTimePeriod(timePeriod),
                _ => timePeriod
            };
        }
        catch (System.Text.Json.JsonException)
        {
            return timePeriod; // Fallback to original if JSON parsing fails
        }
    }

    private string FormatYearlyTimePeriod(string timePeriod)
    {
        // Expected format: "2024" -> "2024"
        if (DateTime.TryParseExact(timePeriod, "yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime date))
        {
            return date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return timePeriod;
    }

    private string FormatQuarterlyTimePeriod(string timePeriod)
    {
        // Expected formats: "2024-Q1", "2024Q1" -> "2024Q1"
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(timePeriod, @"(\d{4})[-_]?Q(\d)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string year = match.Groups[1].Value;
            string quarter = match.Groups[2].Value;
            return $"{year}Q{quarter}";
        }
        return timePeriod;
    }

    private string FormatMonthlyTimePeriod(string timePeriod)
    {
        // Expected formats: "2024-03", "2024-3", "202403" -> "2024-03"
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(timePeriod, @"(\d{4})[-_]?(\d{1,2})");
        if (match.Success)
        {
            string year = match.Groups[1].Value;
            string month = match.Groups[2].Value.PadLeft(2, '0');
            return $"{year}-{month}";
        }
        
        // Try parsing as date
        if (DateTime.TryParse(timePeriod, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime date))
        {
            return $"{date.Year}-{date.Month:D2}";
        }
        
        return timePeriod;
    }
}
