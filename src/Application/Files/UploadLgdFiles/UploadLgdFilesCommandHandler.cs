using System.Globalization;
using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.Common;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Files.UploadLgdFiles;

/// <summary>
/// Handles the processing and storage of LGD file uploads with facility status classification.
/// </summary>
internal sealed class UploadLgdFilesCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<UploadLgdFilesCommandHandler> logger,
    IOptions<LgdFileStorageOptions> storageOptions
) : ICommandHandler<UploadLgdFilesCommand, UploadLgdFilesResponse>
{
    private static readonly HashSet<string> AllowedExtensions = [".xlsx", ".xls", ".csv"];
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel",
        "text/csv",
        "application/csv"
    ];

    public async Task<Result<UploadLgdFilesResponse>> Handle(UploadLgdFilesCommand command, CancellationToken cancellationToken)
    {
        if (command is null)
        {
            return Result.Failure<UploadLgdFilesResponse>(Error.NullValue);
        }

        if (command.Files is null || command.Files.Count == 0)
        {
            return Result.Failure<UploadLgdFilesResponse>(Error.Problem(
                "Files.Empty",
                "No files provided for upload."));
        }

        // Validate year format (YYYY)
        if (string.IsNullOrWhiteSpace(command.Year) || !System.Text.RegularExpressions.Regex.IsMatch(command.Year, @"^\d{4}$"))
        {
            return Result.Failure<UploadLgdFilesResponse>(Error.Problem(
                "Year.InvalidFormat",
                "Year must be in format YYYY (e.g., '2025')."));
        }

        // Validate facility status
        if (string.IsNullOrWhiteSpace(command.FacilityStatus) ||
            command.FacilityStatus != "OpenFacility" && command.FacilityStatus != "ClosedFacility")
        {
            return Result.Failure<UploadLgdFilesResponse>(Error.Problem(
                "FacilityStatus.Invalid",
                "Facility status must be either 'OpenFacility' or 'ClosedFacility'."));
        }

        // Validate timePeriod if provided
        if (command.TimePeriod is not null &&
            (!System.Text.RegularExpressions.Regex.IsMatch(command.TimePeriod.From, @"^\d{4}$") ||
             !System.Text.RegularExpressions.Regex.IsMatch(command.TimePeriod.To, @"^\d{4}$")))
        {
            return Result.Failure<UploadLgdFilesResponse>(Error.Problem(
                "TimePeriod.InvalidFormat",
                "Time period from and to must be in format YYYY (e.g., '2025')."));
        }

        // Fetch or create the LGD collective impairment configuration
        CollectiveImpairmentConfig? config = await dbContext.CollectiveImpairmentConfigs
            .FirstOrDefaultAsync(c => c.Parameter == ParameterType.LGD, cancellationToken);

        bool isNewConfig = false;
        if (config is null)
        {
            // Create initial LGD configuration
            logger.LogInformation("No LGD configuration found. Creating initial configuration.");
            config = new CollectiveImpairmentConfig
            {
                Id = Guid.CreateVersion7(),
                Parameter = ParameterType.LGD,
                ConfigJson = "{}",
                CreatedBy = command.UploadedBy,
                CreatedDate = DateTime.UtcNow
            };
            isNewConfig = true;
        }

        // Parse the existing config JSON
        JsonDocument existingDoc;
        try
        {
            existingDoc = JsonDocument.Parse(config.ConfigJson);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse LGD configuration JSON");
            return Result.Failure<UploadLgdFilesResponse>(Error.Problem(
                "LgdConfig.InvalidJson",
                "LGD configuration contains invalid JSON."));
        }

        // Validate each file
        foreach (LgdFileUploadData fileData in command.Files)
        {
            if (fileData.Content is null || fileData.Content.Length == 0)
            {
                return Result.Failure<UploadLgdFilesResponse>(Error.Problem(
                    "File.Empty",
                    $"File '{fileData.FileName}' is empty."));
            }

            string ext = Path.GetExtension(fileData.FileName).ToUpperInvariant();
            if (!AllowedExtensions.Select(x => x.ToUpperInvariant()).Contains(ext))
            {
                return Result.Failure<UploadLgdFilesResponse>(Error.Problem(
                    "File.InvalidType",
                    $"File '{fileData.FileName}' has invalid type. Only .xlsx, .xls and .csv files are allowed."));
            }

            if (!string.IsNullOrWhiteSpace(fileData.ContentType) && !AllowedContentTypes.Contains(fileData.ContentType))
            {
                return Result.Failure<UploadLgdFilesResponse>(Error.Problem(
                    "File.InvalidContentType",
                    $"File '{fileData.FileName}' has invalid content type. Allowed types: Excel or CSV."));
            }
        }

        // Build file storage path: /mnt/data/appdata/saral-backend-dev/data/LGD/pending/{FacilityStatus}/
        string configuredRoot = storageOptions.Value.RootPath ?? string.Empty;
        string expandedRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.GetTempPath()
            : Environment.ExpandEnvironmentVariables(configuredRoot);

        string rootPath = Path.IsPathRooted(expandedRoot)
            ? expandedRoot
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedRoot));

        // Clean up files outside the time period range if timePeriod is provided
        if (command.TimePeriod is not null)
        {
            await CleanupFilesOutsideRangeAsync(
                rootPath,
                command.TimePeriod,
                existingDoc,
                logger);
        }

        // Create path: {rootPath}/LGD/pending/{FacilityStatus}
        string lgdFolder = Path.Combine(rootPath, "LGD", "pending", command.FacilityStatus);

        if (!Directory.Exists(lgdFolder))
        {
            Directory.CreateDirectory(lgdFolder);
            logger.LogInformation("Created LGD directory structure: '{DirectoryPath}'", lgdFolder);
        }

        // Process all files
        var uploadedFileInfos = new List<LgdUploadedFileInfo>();
        long totalSize = 0;

        foreach (LgdFileUploadData fileData in command.Files)
        {
            string sanitizedBase = FileProcessingUtilities.SanitizeFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(fileData.FileName));
            string baseWithUnderscores = FileProcessingUtilities.ReplaceWhitespaceWithUnderscore(sanitizedBase);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string ext = Path.GetExtension(fileData.FileName);
            string guidPart = Guid.CreateVersion7().ToString("N")[..8];
            string storedFileName = $"{baseWithUnderscores}_{timestamp}_{guidPart}{ext}";
            string filePath = Path.Combine(lgdFolder, storedFileName);

            // Write file to disk
            await File.WriteAllBytesAsync(filePath, fileData.Content, cancellationToken);
            logger.LogInformation("Physical file saved at '{PhysicalPath}'", filePath);

            // Generate unique ID for this file
            string fileId = Guid.CreateVersion7().ToString();
            var uploadId = Guid.CreateVersion7();
            DateTimeOffset uploadedAt = DateTimeOffset.UtcNow;

            // Format file size
            string formattedSize = FormatFileSize(fileData.Content.LongLength);

            uploadedFileInfos.Add(new LgdUploadedFileInfo(
                Id: fileId,
                Name: fileData.FileName,
                Size: formattedSize,
                Type: ext.TrimStart('.').ToUpperInvariant(),
                Status: "pending",
                IsValidated: false,
                ValidationResult: null, // No validation yet at upload time
                UploadId: uploadId,
                StoredFileName: storedFileName,
                UploadedAt: uploadedAt
            ));

            totalSize += fileData.Content.LongLength;
        }

        // Update the LGD config JSON with the new file information
        string updatedConfigJson = BuildUpdatedConfigJson(
            existingDoc,
            command.Year,
            command.FacilityStatus,
            uploadedFileInfos,
            command.TimePeriod,
            command.FinancialYearEnd);

        // Update the database
        config.ConfigJson = updatedConfigJson;

        if (isNewConfig)
        {
            await dbContext.CollectiveImpairmentConfigs.AddAsync(config, cancellationToken);
            logger.LogInformation("Created new LGD configuration in database");
        }
        else
        {
            config.UpdatedBy = command.UploadedBy;
            config.UpdatedDate = DateTime.UtcNow;
            dbContext.CollectiveImpairmentConfigs.Update(config);
            logger.LogInformation("Updated existing LGD configuration in database");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Uploaded {FileCount} LGD files by {UserId} for Year {Year} and FacilityStatus {FacilityStatus} with total size {TotalSize} bytes",
            command.Files.Count, command.UploadedBy, command.Year, command.FacilityStatus, totalSize);

        var response = new UploadLgdFilesResponse(
            UploadedFiles: uploadedFileInfos,
            TotalFiles: command.Files.Count,
            TotalSize: totalSize,
            Year: command.Year,
            FacilityStatus: command.FacilityStatus
        );

        return Result.Success(response);
    }

    private async Task CleanupFilesOutsideRangeAsync(
        string rootPath,
        TimePeriodRequest timePeriod,
        JsonDocument existingDoc,
        ILogger logger)
    {
        logger.LogInformation("Starting cleanup of files outside time period range: {From} to {To}",
            timePeriod.From, timePeriod.To);

        // Get years in the valid range
        List<string> validYears = GetYearsInRange(timePeriod.From, timePeriod.To);
        var validYearsSet = new HashSet<string>(validYears);

        // Get all years that currently have data in JSON
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

        // Find years to remove (those not in valid range)
        var yearsToRemove = existingYears.Where(y => !validYearsSet.Contains(y)).ToList();

        if (yearsToRemove.Count == 0)
        {
            logger.LogInformation("No years found outside the time period range. No cleanup needed.");
            return;
        }

        logger.LogInformation("Found {Count} years outside range to clean up: {Years}",
            yearsToRemove.Count, string.Join(", ", yearsToRemove));

        int totalFilesDeleted = 0;
        int totalFoldersDeleted = 0;
        var deletedFileNames = new List<string>();

        // Cleanup pending files - New path: /LGD/pending/{FacilityStatus}/
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

        // Cleanup processed files - New path: /LGD/processed/{FacilityStatus}/
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

        // Clean up empty parent folders
        CleanupEmptyFolders(pendingLgdPath, logger);
        CleanupEmptyFolders(processedLgdPath, logger);

        logger.LogInformation(
            "Cleanup completed: Deleted {FileCount} files and {FolderCount} folders for years outside range",
            totalFilesDeleted, totalFoldersDeleted);

        if (deletedFileNames.Count > 0)
        {
            logger.LogInformation("Sample of deleted files: {Files}",
                string.Join(", ", deletedFileNames.Take(10)));
        }

        await Task.CompletedTask;
    }

    private static void CleanupEmptyFolders(string basePath, ILogger logger)
    {
        if (!Directory.Exists(basePath))
        {
            return;
        }

        try
        {
            // Get all subdirectories
            string[] directories = Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length)
                .ToArray(); // Process deepest folders first

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
        string year,
        string facilityStatus,
        List<LgdUploadedFileInfo> uploadedFileInfos,
        TimePeriodRequest? timePeriod,
        string? financialYearEnd)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("lgdFileUpload");
            writer.WriteStartObject();

            // Determine the time period range
            string fromYear = year;
            string toYear = year;

            if (existingDoc.RootElement.TryGetProperty("lgdFileUpload", out JsonElement lgdFileUpload) &&
                lgdFileUpload.TryGetProperty("timePeriod", out JsonElement existingTimePeriod))
            {
                fromYear = existingTimePeriod.TryGetProperty("from", out JsonElement fromEl) ? fromEl.GetString() ?? year : year;
                toYear = existingTimePeriod.TryGetProperty("to", out JsonElement toEl) ? toEl.GetString() ?? year : year;
            }

            // Override with provided timePeriod if available
            if (timePeriod is not null)
            {
                fromYear = timePeriod.From;
                toYear = timePeriod.To;
            }
            else
            {
                // Update range to include current year if not in range
                if (int.TryParse(year, out int currentYear) &&
                    int.TryParse(fromYear, out int from) &&
                    int.TryParse(toYear, out int to))
                {
                    int minYear = Math.Min(from, to);
                    int maxYear = Math.Max(from, to);

                    if (currentYear < minYear)
                    {
                        if (from > to)
                        {
                            toYear = year;
                        }
                        else
                        {
                            fromYear = year;
                        }
                    }
                    else if (currentYear > maxYear)
                    {
                        if (from > to)
                        {
                            fromYear = year;
                        }
                        else
                        {
                            toYear = year;
                        }
                    }
                }
            }

            // Write timePeriod
            writer.WritePropertyName("timePeriod");
            writer.WriteStartObject();
            writer.WriteString("from", fromYear);
            writer.WriteString("to", toYear);
            writer.WriteEndObject();

            // Get all years in the range
            List<string> yearsInRange = GetYearsInRange(fromYear, toYear);

            // Collect existing year data (only for years in range)
            Dictionary<string, JsonElement> existingYears = new();
            if (existingDoc.RootElement.TryGetProperty("lgdFileUpload", out JsonElement lgdUpload))
            {
                foreach (JsonProperty property in lgdUpload.EnumerateObject())
                {
                    if (property.Name != "timePeriod" && yearsInRange.Contains(property.Name))
                    {
                        existingYears[property.Name] = property.Value;
                    }
                }
            }

            // Write all years in range (only these will be in the JSON)
            foreach (string yearInRange in yearsInRange)
            {
                bool hasExistingData = existingYears.TryGetValue(yearInRange, out JsonElement existingYearData);
                bool isUploadYear = yearInRange == year;

                if (isUploadYear)
                {
                    // This is the year being uploaded - add files to it
                    WriteYearData(
                        writer,
                        yearInRange,
                        hasExistingData ? existingYearData : null,
                        facilityStatus,
                        uploadedFileInfos,
                        financialYearEnd);
                }
                else if (hasExistingData)
                {
                    // Existing year - copy as is
                    writer.WritePropertyName(yearInRange);
                    existingYearData.WriteTo(writer);
                }
                else
                {
                    // New year in range - create empty structure
                    WriteYearData(
                        writer,
                        yearInRange,
                        null,
                        null,
                        [],
                        null);
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

        // If from > to (like 2030 to 2027), we want descending order
        if (from > to)
        {
            for (int y = from; y >= to; y--)
            {
                years.Add(y.ToString(CultureInfo.InvariantCulture));
            }
        }
        else
        {
            // If from < to, ascending order
            for (int y = from; y <= to; y++)
            {
                years.Add(y.ToString(CultureInfo.InvariantCulture));
            }
        }

        return years;
    }

    private static void WriteYearData(
        Utf8JsonWriter writer,
        string year,
        JsonElement? existingYearData,
        string? facilityStatus,
        List<LgdUploadedFileInfo> uploadedFileInfos,
        string? financialYearEnd)
    {
        writer.WritePropertyName(year);
        writer.WriteStartObject();

        // Write financial year end - only if provided by user or exists in JSON
        if (!string.IsNullOrWhiteSpace(financialYearEnd))
        {
            // User provided a value - use it
            writer.WriteString("financialYearEnd", financialYearEnd);
        }
        else if (existingYearData.HasValue &&
            existingYearData.Value.TryGetProperty("financialYearEnd", out JsonElement existingYearEnd))
        {
            // Preserve existing value from JSON
            string? existingValue = existingYearEnd.GetString();
            if (!string.IsNullOrWhiteSpace(existingValue))
            {
                writer.WriteString("financialYearEnd", existingValue);
            }
            else
            {
                writer.WriteNull("financialYearEnd");
            }
        }
        else
        {
            // No value provided and no existing value - write null
            writer.WriteNull("financialYearEnd");
        }

        // Handle openFacility
        writer.WritePropertyName("openFacility");
        writer.WriteStartObject();
        writer.WritePropertyName("files");
        writer.WriteStartArray();

        if (existingYearData.HasValue && existingYearData.Value.TryGetProperty("openFacility", out JsonElement openFacility) &&
            openFacility.TryGetProperty("files", out JsonElement existingOpenFiles))
        {
            foreach (JsonElement file in existingOpenFiles.EnumerateArray())
            {
                file.WriteTo(writer);
            }
        }

        if (facilityStatus == "OpenFacility")
        {
            foreach (LgdUploadedFileInfo fileInfo in uploadedFileInfos)
            {
                WriteFileInfo(writer, fileInfo);
            }
        }

        writer.WriteEndArray(); // end files array
        writer.WriteEndObject(); // end openFacility

        // Handle closedFacility
        writer.WritePropertyName("closedFacility");
        writer.WriteStartObject();
        writer.WritePropertyName("files");
        writer.WriteStartArray();

        if (existingYearData.HasValue && existingYearData.Value.TryGetProperty("closedFacility", out JsonElement closedFacility) &&
            closedFacility.TryGetProperty("files", out JsonElement existingClosedFiles))
        {
            foreach (JsonElement file in existingClosedFiles.EnumerateArray())
            {
                file.WriteTo(writer);
            }
        }

        if (facilityStatus == "ClosedFacility")
        {
            foreach (LgdUploadedFileInfo fileInfo in uploadedFileInfos)
            {
                WriteFileInfo(writer, fileInfo);
            }
        }

        writer.WriteEndArray(); // end files array
        writer.WriteEndObject(); // end closedFacility
        writer.WriteEndObject(); // end year
    }

    private static void WriteFileInfo(Utf8JsonWriter writer, LgdUploadedFileInfo fileInfo)
    {
        writer.WriteStartObject();
        writer.WriteString("id", fileInfo.Id);
        writer.WriteString("name", fileInfo.Name);
        writer.WriteString("size", fileInfo.Size);
        writer.WriteString("type", fileInfo.Type);
        writer.WriteString("status", fileInfo.Status);
        writer.WriteBoolean("isValidated", fileInfo.IsValidated);

        // Write validation result if available
        if (fileInfo.ValidationResult is not null)
        {
            writer.WritePropertyName("validationResult");
            writer.WriteStartObject();
            writer.WriteNumber("total_rows", fileInfo.ValidationResult.TotalRows);
            writer.WriteNumber("total_errors", fileInfo.ValidationResult.TotalErrors);
            writer.WriteString("status", fileInfo.ValidationResult.Status);
            writer.WriteEndObject();
        }

        writer.WriteString("uploadId", fileInfo.UploadId.ToString());
        writer.WriteString("storedFileName", fileInfo.StoredFileName);
        writer.WriteString("uploadedAt", fileInfo.UploadedAt.ToString("O"));
        writer.WriteEndObject();
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "kB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.#} {sizes[order]}";
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
