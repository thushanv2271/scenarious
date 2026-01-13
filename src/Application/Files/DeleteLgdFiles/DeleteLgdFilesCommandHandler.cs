using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.UploadLgdFiles;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Files.DeleteLgdFiles;

/// <summary>
/// Handles the deletion of LGD files from file system, database JSON, and validation records.
/// </summary>
internal sealed class DeleteLgdFilesCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<DeleteLgdFilesCommandHandler> logger,
    IOptions<LgdFileStorageOptions> storageOptions
) : ICommandHandler<DeleteLgdFilesCommand, DeleteLgdFilesResponse>
{
    public async Task<Result<DeleteLgdFilesResponse>> Handle(
        DeleteLgdFilesCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null)
        {
            return Result.Failure<DeleteLgdFilesResponse>(Error.NullValue);
        }

        if (command.FileIds is null || command.FileIds.Count == 0)
        {
            return Result.Failure<DeleteLgdFilesResponse>(Error.Problem(
                "FileIds.Empty",
                "No file IDs provided for deletion."));
        }

        // Validate year format
        if (string.IsNullOrWhiteSpace(command.Year) ||
            !System.Text.RegularExpressions.Regex.IsMatch(command.Year, @"^\d{4}$"))
        {
            return Result.Failure<DeleteLgdFilesResponse>(Error.Problem(
                "Year.InvalidFormat",
                "Year must be in format YYYY (e.g., '2025')."));
        }

        // Validate facility status
        if (string.IsNullOrWhiteSpace(command.FacilityStatus) ||
            command.FacilityStatus != "OpenFacility" && command.FacilityStatus != "ClosedFacility")
        {
            return Result.Failure<DeleteLgdFilesResponse>(Error.Problem(
                "FacilityStatus.Invalid",
                "Facility status must be either 'OpenFacility' or 'ClosedFacility'."));
        }

        logger.LogInformation(
            "DeleteLgdFiles request for {Count} files in Year: {Year}, FacilityStatus: {FacilityStatus}",
            command.FileIds.Count, command.Year, command.FacilityStatus);

        // Fetch LGD configuration
        CollectiveImpairmentConfig? config = await dbContext.CollectiveImpairmentConfigs
            .FirstOrDefaultAsync(c => c.Parameter == ParameterType.LGD, cancellationToken);

        if (config is null)
        {
            return Result.Failure<DeleteLgdFilesResponse>(Error.Problem(
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
            return Result.Failure<DeleteLgdFilesResponse>(Error.Problem(
                "LgdConfig.InvalidJson",
                "LGD configuration contains invalid JSON."));
        }

        // Build file storage paths
        string configuredRoot = storageOptions.Value.RootPath ?? string.Empty;
        string expandedRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.GetTempPath()
            : Environment.ExpandEnvironmentVariables(configuredRoot);

        string rootPath = Path.IsPathRooted(expandedRoot)
            ? expandedRoot
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedRoot));

        var deletedFiles = new List<string>();
        var failedFiles = new Dictionary<string, string>();
        var filesToDeleteFromJson = new List<string>();

        // Get file information from JSON and delete physical files
        if (existingDoc.RootElement.TryGetProperty("lgdFileUpload", out JsonElement lgdFileUpload) &&
            lgdFileUpload.TryGetProperty(command.Year, out JsonElement yearData))
        {
            string facilityPropertyName = command.FacilityStatus == "OpenFacility"
                ? "openFacility"
                : "closedFacility";

            if (yearData.TryGetProperty(facilityPropertyName, out JsonElement facilityData) &&
                facilityData.TryGetProperty("files", out JsonElement filesArray))
            {
                foreach (JsonElement fileElement in filesArray.EnumerateArray())
                {
                    // Changed from "id" to "uploadId"
                    if (!fileElement.TryGetProperty("uploadId", out JsonElement uploadIdElement))
                    {
                        continue;
                    }

                    string? uploadId = uploadIdElement.GetString();
                    if (uploadId is null || !command.FileIds.Contains(uploadId))
                    {
                        continue;
                    }

                    // Get stored file name
                    string? storedFileName = fileElement.TryGetProperty("storedFileName", out JsonElement sfn)
                        ? sfn.GetString()
                        : null;

                    if (string.IsNullOrWhiteSpace(storedFileName))
                    {
                        failedFiles[uploadId] = "Stored file name not found in configuration";
                        continue;
                    }

                    // Try to delete from both pending and processed directories
                    bool physicalFileDeleted = false;
                    var deletionErrors = new List<string>();

                    // Check pending directory
                    string pendingPath = Path.Combine(rootPath, "LGD", "pending",
                        command.FacilityStatus, storedFileName);

                    if (File.Exists(pendingPath))
                    {
                        try
                        {
                            File.Delete(pendingPath);
                            logger.LogInformation("Deleted file from pending: {Path}", pendingPath);
                            physicalFileDeleted = true;
                        }
                        catch (Exception ex)
                        {
                            deletionErrors.Add($"Pending: {ex.Message}");
                            logger.LogError(ex, "Failed to delete file from pending: {Path}", pendingPath);
                        }
                    }

                    // Check processed directory
                    string processedPath = Path.Combine(rootPath, "LGD", "processed",
                        command.FacilityStatus, storedFileName);

                    if (File.Exists(processedPath))
                    {
                        try
                        {
                            File.Delete(processedPath);
                            logger.LogInformation("Deleted file from processed: {Path}", processedPath);
                            physicalFileDeleted = true;
                        }
                        catch (Exception ex)
                        {
                            deletionErrors.Add($"Processed: {ex.Message}");
                            logger.LogError(ex, "Failed to delete file from processed: {Path}", processedPath);
                        }
                    }

                    if (!physicalFileDeleted && deletionErrors.Count == 0)
                    {
                        // File doesn't exist in either location - still remove from JSON
                        logger.LogWarning("Physical file not found: {FileName}, removing from JSON anyway",
                            storedFileName);
                    }

                    if (deletionErrors.Count > 0 && !physicalFileDeleted)
                    {
                        failedFiles[uploadId] = string.Join("; ", deletionErrors);
                        continue;
                    }

                    // Delete from file_validation_results table if exists
                    try
                    {
                        List<Domain.Files.FileValidationResult> validationResults = await dbContext.FileValidationResults
                            .Where(fvr => fvr.Filename == storedFileName)
                            .ToListAsync(cancellationToken);

                        if (validationResults.Count > 0)
                        {
                            dbContext.FileValidationResults.RemoveRange(validationResults);
                            logger.LogInformation("Deleted {Count} validation records for file: {FileName}",
                                validationResults.Count, storedFileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to delete validation records for file: {FileName}",
                            storedFileName);
                        // Continue anyway - don't fail the entire operation
                    }

                    deletedFiles.Add(uploadId);
                    filesToDeleteFromJson.Add(uploadId);
                }
            }
        }

        // Check if any files were found
        if (deletedFiles.Count == 0 && failedFiles.Count == 0)
        {
            return Result.Failure<DeleteLgdFilesResponse>(Error.Problem(
                "Files.NotFound",
                "No files found matching the provided uploadIds in the configuration."));
        }

        // Update JSON configuration to remove deleted files
        if (filesToDeleteFromJson.Count > 0)
        {
            string updatedConfigJson = BuildUpdatedConfigJsonWithoutFiles(
                existingDoc,
                command.Year,
                command.FacilityStatus,
                filesToDeleteFromJson);

            config.ConfigJson = updatedConfigJson;
            config.UpdatedBy = command.DeletedBy;
            config.UpdatedDate = DateTime.UtcNow;

            dbContext.CollectiveImpairmentConfigs.Update(config);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Updated JSON configuration, removed {Count} files from Year: {Year}, FacilityStatus: {FacilityStatus}",
                filesToDeleteFromJson.Count, command.Year, command.FacilityStatus);
        }

        var response = new DeleteLgdFilesResponse(
            DeletedFiles: deletedFiles,
            FailedFiles: failedFiles,
            TotalDeleted: deletedFiles.Count,
            TotalFailed: failedFiles.Count
        );

        logger.LogInformation(
            "DeleteLgdFiles completed: {Deleted} deleted, {Failed} failed",
            deletedFiles.Count, failedFiles.Count);

        return Result.Success(response);
    }

    private static string BuildUpdatedConfigJsonWithoutFiles(
        JsonDocument existingDoc,
        string year,
        string facilityStatus,
        List<string> fileIdsToRemove)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("lgdFileUpload");
            writer.WriteStartObject();

            if (!existingDoc.RootElement.TryGetProperty("lgdFileUpload", out JsonElement lgdFileUpload))
            {
                writer.WriteEndObject();
                writer.WriteEndObject();
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }

            // Copy all properties
            foreach (JsonProperty property in lgdFileUpload.EnumerateObject())
            {
                if (property.Name == year)
                {
                    // Process the target year - remove specified files
                    WriteYearDataWithoutFiles(writer, year, property.Value, facilityStatus, fileIdsToRemove);
                }
                else
                {
                    // Copy other years as-is
                    writer.WritePropertyName(property.Name);
                    property.Value.WriteTo(writer);
                }
            }

            writer.WriteEndObject(); // end lgdFileUpload
            writer.WriteEndObject(); // end root
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteYearDataWithoutFiles(
        Utf8JsonWriter writer,
        string year,
        JsonElement yearData,
        string targetFacilityStatus,
        List<string> fileIdsToRemove)
    {
        writer.WritePropertyName(year);
        writer.WriteStartObject();

        foreach (JsonProperty property in yearData.EnumerateObject())
        {
            if (property.Name == "openFacility" && targetFacilityStatus == "OpenFacility")
            {
                WriteFacilityDataWithoutFiles(writer, "openFacility", property.Value, fileIdsToRemove);
            }
            else if (property.Name == "closedFacility" && targetFacilityStatus == "ClosedFacility")
            {
                WriteFacilityDataWithoutFiles(writer, "closedFacility", property.Value, fileIdsToRemove);
            }
            else
            {
                writer.WritePropertyName(property.Name);
                property.Value.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
    }

    private static void WriteFacilityDataWithoutFiles(
        Utf8JsonWriter writer,
        string facilityName,
        JsonElement facilityData,
        List<string> fileIdsToRemove)
    {
        writer.WritePropertyName(facilityName);
        writer.WriteStartObject();
        writer.WritePropertyName("files");
        writer.WriteStartArray();

        if (facilityData.TryGetProperty("files", out JsonElement filesArray))
        {
            foreach (JsonElement file in filesArray.EnumerateArray())
            {
                // Changed from "id" to "uploadId"
                string? uploadId = file.TryGetProperty("uploadId", out JsonElement uploadIdElement)
                    ? uploadIdElement.GetString()
                    : null;

                // Only write files that are NOT in the removal list
                if (uploadId is null || !fileIdsToRemove.Contains(uploadId))
                {
                    file.WriteTo(writer);
                }
            }
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
