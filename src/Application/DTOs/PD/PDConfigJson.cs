using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.DTOs.PD;

/// <summary>
/// Root configuration model for PD calculation stored in ConfigJson
/// </summary>
public sealed class PDConfigJson
{
    [JsonPropertyName("pdSetup")]
    public PdSetup PdSetup { get; set; } = new();

    [JsonPropertyName("pdFileUpload")]
    public Dictionary<string, PdTimePeriodData> PdFileUpload { get; set; } = new();

    [JsonPropertyName("pdConfigurations")]
    public PdConfigurations PdConfigurations { get; set; } = new();

    [JsonPropertyName("pdResults")]
    public PdResults? PdResults { get; set; }

    [JsonPropertyName("currentStep")]
    public int CurrentStep { get; set; }
}

/// <summary>
/// PD Setup configuration containing frequency and time period
/// </summary>
public sealed class PdSetup
{
    [JsonPropertyName("frequency")]
    public string Frequency { get; set; } = string.Empty;

    [JsonPropertyName("timePeriod")]
    public TimePeriod TimePeriod { get; set; } = new();

    [JsonPropertyName("quarterDefinitions")]
    public Dictionary<string, QuarterDefinition>? QuarterDefinitions { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("excludedPeriods")]
    public List<string> ExcludedPeriods { get; set; } = new();
}

/// <summary>
/// Time period configuration with from and to dates
/// </summary>
public sealed class TimePeriod
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;
}

public sealed class QuarterDefinition
{
    [JsonPropertyName("start")]
    public string? Start { get; set; }
        = null;

    [JsonPropertyName("end")]
    public string? End { get; set; }
        = null;
}

/// <summary>
/// Wrapper for PD configurations
/// </summary>
public sealed class PdConfigurations
{
    [JsonPropertyName("pdConfiguration")]
    public List<PDConfigurationItem> PdConfiguration { get; set; } = new();

    [JsonPropertyName("multiFacilityAdjustmentEnabled")]
    public bool MultiFacilityAdjustmentEnabled { get; set; }
        = false;

    [JsonPropertyName("multiFacilityAdjustmentRule")]
    public string? MultiFacilityAdjustmentRule { get; set; }
        = string.Empty;

    [JsonPropertyName("percentRuleThreshold")]
    public decimal PercentRuleThreshold { get; set; }
        = 0;
}

/// <summary>
/// Individual PD configuration item for a product category and segment
/// </summary>
public sealed class PDConfigurationItem
{
    [JsonPropertyName("productCategoryId")]
    public string ProductCategoryId { get; set; } = string.Empty;

    [JsonPropertyName("productCategory")]
    public string ProductCategory { get; set; } = string.Empty;

    [JsonPropertyName("segment")]
    public string Segment { get; set; } = string.Empty;

    [JsonPropertyName("pdEstimationApproach")]
    public string PdEstimationApproach { get; set; } = string.Empty;

    [JsonPropertyName("comparisonPeriod")]
    public string ComparisonPeriod { get; set; } = string.Empty;

    [JsonPropertyName("considerNormalMaturities")]
    public bool ConsiderNormalMaturities { get; set; }
        = false;

    [JsonPropertyName("advancedDefaultSearch")]
    public bool AdvancedDefaultSearch { get; set; }
        = false;

    [JsonPropertyName("comments")]
    public string? Comments { get; set; }
        = string.Empty;
}

/// <summary>
/// File upload data for a specific period
/// </summary>
public sealed class PdTimePeriodData
{
    [JsonPropertyName("financialYear")]
    public string? FinancialYear { get; set; }
        = null;

    [JsonPropertyName("files")]
    public List<PdFileMetadata> Files { get; set; } = new();
}

/// <summary>
/// Information about an uploaded file, including validation details
/// </summary>
public sealed class PdFileMetadata
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
        = null;

    [JsonPropertyName("file")]
    public JsonElement? File { get; set; }
        = null;

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
        = 0;

    [JsonPropertyName("isValidated")]
    public bool IsValidated { get; set; }
        = false;

    [JsonPropertyName("uploadedAt")]
    public DateTime UploadedAt { get; set; }
        = DateTime.UtcNow;

    [JsonPropertyName("validationResult")]
    public PdValidationResult? ValidationResult { get; set; }
        = null;
}

/// <summary>
/// Validation result data for an uploaded PD file
/// </summary>
public sealed class PdValidationResult
{
    [JsonPropertyName("total_rows")]
    public int TotalRows { get; set; }
        = 0;

    [JsonPropertyName("total_errors")]
    public int TotalErrors { get; set; }
        = 0;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("errors")]
    public List<PdFileValidationError>? Errors { get; set; }
        = null;
}

/// <summary>
/// Detailed validation error information for a file
/// </summary>
public sealed class PdFileValidationError
{
    [JsonPropertyName("row")]
    public int Row { get; set; }
        = 0;

    [JsonPropertyName("column")]
    public string Column { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Stores UI level PD result flags
/// </summary>
public sealed class PdResults
{
    [JsonPropertyName("isMove")]
    public bool IsMove { get; set; }
        = false;

    [JsonPropertyName("isResultsViewShown")]
    public bool IsResultsViewShown { get; set; }
        = false;
}
