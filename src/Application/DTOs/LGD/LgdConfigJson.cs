using System.Text.Json.Serialization;

namespace Application.DTOs.LGD;

/// <summary>
/// Root configuration model for LGD stored in ConfigJson
/// </summary>
public sealed class LgdConfigJson
{
    [JsonPropertyName("lgdFileUpload")]
    public LgdFileUploadConfig LgdFileUpload { get; set; } = new();
}

/// <summary>
/// LGD File Upload configuration
/// </summary>
public sealed class LgdFileUploadConfig
{
    /// <summary>
    /// Time period range configuration
    /// </summary>
    [JsonPropertyName("timePeriod")]
    public TimePeriodRange? TimePeriod { get; set; }
    
    /// <summary>
    /// Year-based file storage (2025, 2024, etc.)
    /// Dictionary with year as key and file upload data as value
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, YearFileUploadData> Years { get; set; } = new();
    
    /// <summary>
    /// Custom JSON converter to handle year-based properties
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? YearData { get; set; }
}

/// <summary>
/// Time period range with from/to years
/// </summary>
public sealed class TimePeriodRange
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;
    
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;
}

/// <summary>
/// File upload data for a specific year, organized by facility status
/// </summary>
public sealed class YearFileUploadData
{
    /// <summary>
    /// Financial year end date (e.g., "2025-12-31")
    /// </summary>
    [JsonPropertyName("financialYearEnd")]
    public string? FinancialYearEnd { get; set; }
    
    /// <summary>
    /// OpenFacility files for this year
    /// </summary>
    [JsonPropertyName("openFacility")]
    public FacilityStatusFileData? OpenFacility { get; set; }
    
    /// <summary>
    /// ClosedFacility files for this year
    /// </summary>
    [JsonPropertyName("closedFacility")]
    public FacilityStatusFileData? ClosedFacility { get; set; }
}

/// <summary>
/// File data for a specific facility status
/// </summary>
public sealed class FacilityStatusFileData
{
    [JsonPropertyName("files")]
    public List<LgdUploadedFileInfo> Files { get; set; } = new();
}

/// <summary>
/// Information about an uploaded LGD file
/// </summary>
public sealed class LgdUploadedFileInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("isValidated")]
    public bool IsValidated { get; set; }
    
    /// <summary>
    /// Validation result containing validation statistics
    /// </summary>
    [JsonPropertyName("validationResult")]
    public FileValidationResultInfo? ValidationResult { get; set; }
    
    [JsonPropertyName("uploadId")]
    public Guid UploadId { get; set; }
    
    [JsonPropertyName("storedFileName")]
    public string StoredFileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the file was uploaded (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("uploadedAt")]
    public DateTimeOffset UploadedAt { get; set; }
}

/// <summary>
/// Validation result information for a file
/// </summary>
public sealed class FileValidationResultInfo
{
    [JsonPropertyName("total_rows")]
    public int TotalRows { get; set; }
    
    [JsonPropertyName("total_errors")]
    public int TotalErrors { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
