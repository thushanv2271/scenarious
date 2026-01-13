using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.Common;
using Application.Files.UploadLgdFiles;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Files.UploadPdFiles;

/// <summary>
/// Handler for uploading PD files and storing metadata in collective_impairment_configs JSON
/// Uses hierarchical directory structure based on time period frequency (yearly/quarterly/monthly)
/// </summary>
internal sealed class UploadPdFilesCommandHandler(
    IApplicationDbContext dbContext,
    IOptions<LgdFileStorageOptions> storageOptions,
    ILogger<UploadPdFilesCommandHandler> logger
) : ICommandHandler<UploadPdFilesCommand, UploadPdFilesResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never // Include null values
    };

    public async Task<Result<UploadPdFilesResponse>> Handle(
        UploadPdFilesCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null || command.Files is null || command.Files.Count == 0)
        {
            return Result.Failure<UploadPdFilesResponse>(Error.Validation(
                "Upload.NoFiles",
                "No files provided for upload"));
        }

        if (string.IsNullOrWhiteSpace(command.TimePeriod))
        {
            return Result.Failure<UploadPdFilesResponse>(Error.Validation(
                "Upload.InvalidTimePeriod",
                "Time period is required"));
        }

        logger.LogInformation("Starting PD file upload for {FileCount} files, TimePeriod: {TimePeriod}",
            command.Files.Count, command.TimePeriod);

        try
        {
            // Fetch PD configuration
            CollectiveImpairmentConfig? pdConfig = await dbContext.CollectiveImpairmentConfigs
                .FirstOrDefaultAsync(c => c.Parameter == ParameterType.PD, cancellationToken);

            if (pdConfig is null)
            {
                return Result.Failure<UploadPdFilesResponse>(Error.NotFound(
                    "Config.NotFound",
                    "PD configuration not found"));
            }

            // Parse existing configuration
            PDConfigurationJson? configJson = JsonSerializer.Deserialize<PDConfigurationJson>(
                pdConfig.ConfigJson, JsonOptions);

            if (configJson is null)
            {
                return Result.Failure<UploadPdFilesResponse>(Error.Validation(
                    "Config.InvalidJson",
                    "Failed to parse PD configuration JSON"));
            }

            // Validate time period against configuration
            Result<string> timePeriodValidation = FileProcessingUtilities.ValidateTimePeriod(
                command.TimePeriod, 
                pdConfig.ConfigJson);
            
            if (!timePeriodValidation.IsSuccess)
            {
                return Result.Failure<UploadPdFilesResponse>(timePeriodValidation.Error);
            }

            // Build hierarchical storage path based on frequency
            string rootPath = GetRootPath();
            string pdPendingPath = Path.Combine(rootPath, "PD", "pending");

            // Create hierarchical folder structure based on frequency (e.g., PD/pending/2025/Q1)
            string timePeriodFolder = FileProcessingUtilities.CreateTimePeriodFolderPath(
                pdPendingPath, 
                command.TimePeriod, 
                pdConfig.ConfigJson);

            if (!Directory.Exists(timePeriodFolder))
            {
                Directory.CreateDirectory(timePeriodFolder);
                logger.LogInformation("Created directory structure: '{DirectoryPath}'", timePeriodFolder);
            }

            // Initialize pdFileUpload if null
            configJson.PdFileUpload ??= new Dictionary<string, PdTimePeriodData>();

            // Get or create time period data
            if (!configJson.PdFileUpload.TryGetValue(command.TimePeriod, out PdTimePeriodData? timePeriodData))
            {
                timePeriodData = new PdTimePeriodData
                {
                    FinancialYear = null,
                    Files = new List<PdFileMetadata>()
                };
                configJson.PdFileUpload[command.TimePeriod] = timePeriodData;
            }

            // Process and upload files
            var uploadedFiles = new List<UploadedFileInfo>();
            long totalSize = 0;
            
            foreach (PdFileUploadData fileData in command.Files)
            {
                // Generate unique stored file name with short GUID (consistent with POST /files)
                string uploadId = Guid.NewGuid().ToString();
                var fileId = Guid.CreateVersion7();
                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                string fileExtension = Path.GetExtension(fileData.FileName);
                
                // Sanitize and format filename (consistent with POST /files)
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileData.FileName);
                string sanitizedBase = FileProcessingUtilities.SanitizeFileNameWithoutExtension(fileNameWithoutExt);
                string baseWithUnderscores = FileProcessingUtilities.ReplaceWhitespaceWithUnderscore(sanitizedBase);
                
                // Use short GUID (8 chars) for consistency with POST /files
                string guidPart = Guid.CreateVersion7().ToString("N")[..8];
                string storedFileName = $"{baseWithUnderscores}_{timestamp}_{guidPart}{fileExtension}";
                
                // Save to hierarchical folder structure
                string filePath = Path.Combine(timePeriodFolder, storedFileName);

                // Save file to disk
                await File.WriteAllBytesAsync(filePath, fileData.Content, cancellationToken);
                logger.LogInformation("Saved file to disk: {FilePath}", filePath);

                // Generate public URL for the file (consistent with POST /files)
                // Use file:// scheme for local paths to create a valid URI
                var fileUrl = new Uri(new Uri("file://"), filePath);

                // Create file metadata
                var fileMetadata = new PdFileMetadata
                {
                    UploadId = uploadId,
                    Name = fileData.FileName,
                    StoredFileName = storedFileName,
                    Size = FormatFileSize(fileData.Content.Length),
                    Type = fileExtension.TrimStart('.').ToUpperInvariant(),
                    Status = "pending",
                    Progress = 0,
                    IsValidated = false,
                    UploadedAt = DateTime.UtcNow,
                    ValidationResult = null
                };

                timePeriodData.Files.Add(fileMetadata);

                // Return response matching POST /files format
                uploadedFiles.Add(new UploadedFileInfo(
                    Id: fileId,
                    Url: fileUrl,
                    StoredFileName: storedFileName,
                    OriginalFileName: fileData.FileName,
                    Size: fileData.Content.Length
                ));

                totalSize += fileData.Content.Length;
            }

            // Update configuration JSON
            string updatedConfigJson = JsonSerializer.Serialize(configJson, JsonOptions);
            pdConfig.ConfigJson = updatedConfigJson;
            pdConfig.UpdatedBy = command.UploadedBy;
            pdConfig.UpdatedDate = DateTime.UtcNow;

            dbContext.CollectiveImpairmentConfigs.Update(pdConfig);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully uploaded {FileCount} PD files for TimePeriod: {TimePeriod} to folder: {Folder}",
                uploadedFiles.Count, command.TimePeriod, timePeriodFolder);

            return Result.Success(new UploadPdFilesResponse(
                UploadedFiles: uploadedFiles,
                TotalFiles: uploadedFiles.Count,
                TotalSize: totalSize
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading PD files for TimePeriod: {TimePeriod}", command.TimePeriod);
            return Result.Failure<UploadPdFilesResponse>(Error.Failure(
                "Upload.Failed",
                $"Failed to upload files: {ex.Message}"));
        }
    }

    private string GetRootPath()
    {
        string configuredRoot = storageOptions.Value.RootPath ?? string.Empty;
        string expandedRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.GetTempPath()
            : Environment.ExpandEnvironmentVariables(configuredRoot);

        return Path.IsPathRooted(expandedRoot)
            ? expandedRoot
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedRoot));
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.#} {sizes[order]}";
    }
}

/// <summary>
/// PD Configuration JSON structure
/// </summary>
public sealed class PDConfigurationJson
{
    public PdSetup PdSetup { get; set; } = new();
    public Dictionary<string, PdTimePeriodData>? PdFileUpload { get; set; }
    public PdConfigurations PdConfigurations { get; set; } = new();
}

public sealed class PdSetup
{
    public string Frequency { get; set; } = string.Empty;
    public TimePeriod TimePeriod { get; set; } = new();
}

public sealed class TimePeriod
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}

public sealed class PdConfigurations
{
    public List<PDConfigurationItem> PdConfiguration { get; set; } = new();
    public bool MultiFacilityAdjustmentEnabled { get; set; }
    public string? MultiFacilityAdjustmentRule { get; set; }
    public decimal PercentRuleThreshold { get; set; }
}

public sealed class PDConfigurationItem
{
    public string ProductCategoryId { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string PdEstimationApproach { get; set; } = string.Empty;
    public string ComparisonPeriod { get; set; } = string.Empty;
    public bool ConsiderNormalMaturities { get; set; }
    public bool AdvancedDefaultSearch { get; set; }
    public string? Comments { get; set; }
}

public sealed class PdTimePeriodData
{
    public string? FinancialYear { get; set; }
    public List<PdFileMetadata> Files { get; set; } = new();
}

public sealed class PdFileMetadata
{
    [JsonPropertyName("uploadId")]
    public string UploadId { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("storedFileName")]
    public string StoredFileName { get; set; } = string.Empty;
    
    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("progress")]
    public int Progress { get; set; }
    
    [JsonPropertyName("isValidated")]
    public bool IsValidated { get; set; }
    
    [JsonPropertyName("uploadedAt")]
    public DateTime UploadedAt { get; set; }
    
    [JsonPropertyName("validationResult")]
    public PdValidationResult? ValidationResult { get; set; }
}

public sealed class PdValidationResult
{
    [JsonPropertyName("total_rows")]
    public int TotalRows { get; set; }
    
    [JsonPropertyName("total_errors")]
    public int TotalErrors { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("errors")]
    public List<ValidationError>? Errors { get; set; }
}

public sealed class ValidationError
{
    [JsonPropertyName("row")]
    public int Row { get; set; }
    
    [JsonPropertyName("column")]
    public string Column { get; set; } = string.Empty;
    
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}
