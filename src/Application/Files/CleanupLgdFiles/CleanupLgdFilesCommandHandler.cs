using System.Globalization;
using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.UploadLgdFiles;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Files.CleanupLgdFiles;

/// <summary>
/// Handles cleanup of LGD files outside the specified time period range.
/// </summary>
internal sealed class CleanupLgdFilesCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<CleanupLgdFilesCommandHandler> logger,
    IOptions<LgdFileStorageOptions> storageOptions
) : ICommandHandler<CleanupLgdFilesCommand, CleanupLgdFilesResponse>
{
    public async Task<Result<CleanupLgdFilesResponse>> Handle(
        CleanupLgdFilesCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null)
        {
            return Result.Failure<CleanupLgdFilesResponse>(Error.NullValue);
        }

        // Validate time period from format
        if (string.IsNullOrWhiteSpace(command.TimePeriodFrom) ||
            !System.Text.RegularExpressions.Regex.IsMatch(command.TimePeriodFrom, @"^\d{4}$"))
        {
            return Result.Failure<CleanupLgdFilesResponse>(Error.Problem(
                "TimePeriodFrom.InvalidFormat",
                "Time period from must be in format YYYY (e.g., '2027')."));
        }

        // Check if this is a full cleanup (timePeriodTo is null or empty)
        bool isFullCleanup = string.IsNullOrWhiteSpace(command.TimePeriodTo);

        // Validate time period to format if provided
        if (!isFullCleanup &&
            !System.Text.RegularExpressions.Regex.IsMatch(command.TimePeriodTo!, @"^\d{4}$"))
        {
            return Result.Failure<CleanupLgdFilesResponse>(Error.Problem(
                "TimePeriodTo.InvalidFormat",
                "Time period to must be in format YYYY (e.g., '2023') or null for full cleanup."));
        }

        logger.LogInformation(
            "CleanupLgdFiles starting - TimePeriodFrom: {From}, TimePeriodTo: {To}, FullCleanup: {IsFullCleanup}",
            command.TimePeriodFrom, command.TimePeriodTo ?? "null", isFullCleanup);

        // Fetch LGD configuration
        CollectiveImpairmentConfig? config = await dbContext.CollectiveImpairmentConfigs
            .FirstOrDefaultAsync(c => c.Parameter == ParameterType.LGD, cancellationToken);

        if (config is null)
        {
            return Result.Failure<CleanupLgdFilesResponse>(Error.Problem(
                "LgdConfig.NotFound",
                "No LGD configuration found."));
        }

        // Parse existing config JSON
        JsonDocument existingDoc;
        try
        {
            existingDoc = JsonDocument.Parse(config.ConfigJson);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse LGD configuration JSON");
            return Result.Failure<CleanupLgdFilesResponse>(Error.Problem(
                "LgdConfig.InvalidJson",
                "LGD configuration contains invalid JSON."));
        }

        // Build file storage path
        string configuredRoot = storageOptions.Value.RootPath ?? string.Empty;
        string expandedRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.GetTempPath()
            : Environment.ExpandEnvironmentVariables(configuredRoot);

        string rootPath = Path.IsPathRooted(expandedRoot)
            ? expandedRoot
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedRoot));

        // Get all existing years from JSON
        var existingYears = new List<string>();
        if (existingDoc.RootElement.TryGetProperty("lgdFileUpload", out JsonElement lgdFileUpload))
        {
            foreach (JsonProperty property in lgdFileUpload.EnumerateObject())
            {
                if (property.Name != "timePeriod" &&
                    System.Text.RegularExpressions.Regex.IsMatch(property.Name, @"^\d{4}$"))
                {
                    existingYears.Add(property.Name);
                }
            }
        }

        List<string> validYears;
        List<string> yearsToRemove;

        if (isFullCleanup)
        {
            // Full cleanup: remove all existing years
            validYears = [];
            yearsToRemove = existingYears.ToList();
            logger.LogInformation("Full cleanup mode: All {Count} years will be removed", yearsToRemove.Count);
        }
        else
        {
            // Partial cleanup: keep years in range
            validYears = GetYearsInRange(command.TimePeriodFrom, command.TimePeriodTo!);
            var validYearsSet = new HashSet<string>(validYears);
            yearsToRemove = existingYears.Where(y => !validYearsSet.Contains(y)).ToList();
            logger.LogInformation("Partial cleanup mode: {ValidCount} years to keep, {RemoveCount} years to remove",
                validYears.Count, yearsToRemove.Count);
        }

        if (yearsToRemove.Count == 0)
        {
            logger.LogInformation("No years found to clean up.");

            return Result.Success(new CleanupLgdFilesResponse(
                DeletedYears: [],
                TotalFilesDeleted: 0,
                TotalFoldersDeleted: 0,
                TotalValidationRecordsDeleted: 0,
                RemainingYears: existingYears,
                UpdatedTimePeriod: new TimePeriodInfo(
                    command.TimePeriodFrom,
                    command.TimePeriodTo ?? "null")
            ));
        }

        logger.LogInformation(
            "Found {Count} years to clean up: {Years}",
            yearsToRemove.Count, string.Join(", ", yearsToRemove));

        int totalFilesDeleted = 0;
        int totalFoldersDeleted = 0;
        var deletedFileNames = new HashSet<string>();

        // Cleanup pending files and collect file names
        string pendingLgdPath = Path.Combine(rootPath, "LGD", "pending");
        if (Directory.Exists(pendingLgdPath))
        {
            // Get all facility status directories
            string[] facilityDirectories = Directory.GetDirectories(pendingLgdPath);
            foreach (string facilityDir in facilityDirectories)
            {
                // Get all files in this facility directory and filter by years to remove
                string[] files = Directory.GetFiles(facilityDir, "*.*", SearchOption.TopDirectoryOnly);
                var filesToDelete = new List<string>();

                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    // Extract year from filename if it contains year info, otherwise skip cleanup for this file
                    if (ExtractYearFromFileName(fileName) is string fileYear && yearsToRemove.Contains(fileYear))
                    {
                        filesToDelete.Add(file);
                        deletedFileNames.Add(fileName);
                    }
                }

                // Delete the identified files
                foreach (string fileToDelete in filesToDelete)
                {
                    try
                    {
                        File.Delete(fileToDelete);
                        totalFilesDeleted++;
                        logger.LogInformation("Deleted pending file: {Path}", fileToDelete);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to delete pending file: {Path}", fileToDelete);
                    }
                }
            }
        }

        // Cleanup processed files and collect file names
        string processedLgdPath = Path.Combine(rootPath, "LGD", "processed");
        if (Directory.Exists(processedLgdPath))
        {
            // Get all facility status directories
            string[] facilityDirectories = Directory.GetDirectories(processedLgdPath);
            foreach (string facilityDir in facilityDirectories)
            {
                // Get all files in this facility directory and filter by years to remove
                string[] files = Directory.GetFiles(facilityDir, "*.*", SearchOption.TopDirectoryOnly);
                var filesToDelete = new List<string>();

                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    // Extract year from filename if it contains year info, otherwise skip cleanup for this file
                    if (ExtractYearFromFileName(fileName) is string fileYear && yearsToRemove.Contains(fileYear))
                    {
                        filesToDelete.Add(file);
                        deletedFileNames.Add(fileName);
                    }
                }

                // Delete the identified files
                foreach (string fileToDelete in filesToDelete)
                {
                    try
                    {
                        File.Delete(fileToDelete);
                        totalFilesDeleted++;
                        logger.LogInformation("Deleted processed file: {Path}", fileToDelete);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to delete processed file: {Path}", fileToDelete);
                    }
                }
            }
        }

        // Clean up empty folders
        CleanupEmptyFolders(pendingLgdPath, logger);
        CleanupEmptyFolders(processedLgdPath, logger);

        // Cleanup file_validation_results table
        int totalValidationRecordsDeleted = 0;
        if (deletedFileNames.Count > 0)
        {
            try
            {
                logger.LogInformation("Cleaning up validation records for {Count} deleted files", deletedFileNames.Count);

                List<Domain.Files.FileValidationResult> validationResults = await dbContext.FileValidationResults
                    .Where(fvr => deletedFileNames.Contains(fvr.Filename))
                    .ToListAsync(cancellationToken);

                if (validationResults.Count > 0)
                {
                    dbContext.FileValidationResults.RemoveRange(validationResults);
                    totalValidationRecordsDeleted = validationResults.Count;
                    logger.LogInformation("Deleted {Count} validation records from database", totalValidationRecordsDeleted);
                }
                else
                {
                    logger.LogInformation("No validation records found for deleted files");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete validation records. Continuing with cleanup.");
                // Don't fail the entire operation if validation records cleanup fails
            }
        }

        // Update JSON configuration
        string updatedConfigJson = BuildUpdatedConfigJson(
            existingDoc,
            command.TimePeriodFrom,
            command.TimePeriodTo,
            validYears,
            isFullCleanup);

        config.ConfigJson = updatedConfigJson;
        config.UpdatedBy = command.CleanedBy;
        config.UpdatedDate = DateTime.UtcNow;

        dbContext.CollectiveImpairmentConfigs.Update(config);
        await dbContext.SaveChangesAsync(cancellationToken);

        string cleanupType = isFullCleanup ? "Full cleanup" : "Partial cleanup";
        logger.LogInformation(
            "{CleanupType} completed: Deleted {FileCount} files, {FolderCount} folders, {ValidationCount} validation records, {YearCount} years",
            cleanupType, totalFilesDeleted, totalFoldersDeleted, totalValidationRecordsDeleted, yearsToRemove.Count);

        var response = new CleanupLgdFilesResponse(
            DeletedYears: yearsToRemove,
            TotalFilesDeleted: totalFilesDeleted,
            TotalFoldersDeleted: totalFoldersDeleted,
            TotalValidationRecordsDeleted: totalValidationRecordsDeleted,
            RemainingYears: validYears,
            UpdatedTimePeriod: new TimePeriodInfo(
                command.TimePeriodFrom,
                command.TimePeriodTo ?? "null")
        );

        return Result.Success(response);
    }

    private static void CleanupEmptyFolders(string basePath, ILogger logger)
    {
        if (!Directory.Exists(basePath))
        {
            return;
        }

        try
        {
            string[] directories = Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length)
                .ToArray();

            foreach (string dir in directories)
            {
                if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                    logger.LogInformation("Deleted empty directory: {Path}", dir);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cleanup empty folders in {Path}", basePath);
        }
    }

    private static string BuildUpdatedConfigJson(
        JsonDocument existingDoc,
        string timePeriodFrom,
        string? timePeriodTo,
        List<string> validYears,
        bool isFullCleanup)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("lgdFileUpload");
            writer.WriteStartObject();

            // Write updated timePeriod
            writer.WritePropertyName("timePeriod");
            writer.WriteStartObject();
            writer.WriteString("from", timePeriodFrom);
            if (isFullCleanup)
            {
                writer.WriteNull("to");
            }
            else
            {
                writer.WriteString("to", timePeriodTo);
            }
            writer.WriteEndObject();

            // Copy only years within the valid range (none if full cleanup)
            if (!isFullCleanup && existingDoc.RootElement.TryGetProperty("lgdFileUpload", out JsonElement lgdFileUpload))
            {
                foreach (JsonProperty property in lgdFileUpload.EnumerateObject())
                {
                    if (property.Name == "timePeriod")
                    {
                        continue;
                    }

                    if (validYears.Contains(property.Name))
                    {
                        writer.WritePropertyName(property.Name);
                        property.Value.WriteTo(writer);
                    }
                }
            }

            writer.WriteEndObject(); // end lgdFileUpload
            writer.WriteEndObject(); // end root
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static List<string> GetYearsInRange(string fromYear, string toYear)
    {
        if (!int.TryParse(fromYear, out int from) || !int.TryParse(toYear, out int to))
        {
            return [fromYear];
        }

        var years = new List<string>();

        if (from > to)
        {
            for (int y = from; y >= to; y--)
            {
                years.Add(y.ToString(CultureInfo.InvariantCulture));
            }
        }
        else
        {
            for (int y = from; y <= to; y++)
            {
                years.Add(y.ToString(CultureInfo.InvariantCulture));
            }
        }

        return years;
    }

    /// <summary>
    /// Extracts year from filename. Expects filenames to contain year in format YYYY.
    /// </summary>
    /// <param name="fileName">The filename to extract year from</param>
    /// <returns>Year as string if found, null otherwise</returns>
    private static string? ExtractYearFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        // Look for a 4-digit year pattern (2000-2099)
        System.Text.RegularExpressions.Match yearMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"\b(20\d{2})\b");
        return yearMatch.Success ? yearMatch.Groups[1].Value : null;
    }
}
