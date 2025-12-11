namespace Application.DTOs.PD;

/// <summary>
/// Root configuration model for PD calculation stored in ConfigJson
/// </summary>
public sealed class PDConfigJson
{
    public PDSetupConfig PdSetup { get; set; } = new();
    public PDFileUploadConfig PdFileUpload { get; set; } = new();
    public PDConfigurationsWrapper PdConfigurations { get; set; } = new();
}

/// <summary>
/// PD Setup configuration containing frequency and time period
/// </summary>
public sealed class PDSetupConfig
{
    public string Frequency { get; set; } = string.Empty;
    public TimePeriodConfig TimePeriod { get; set; } = new();
    public List<string> ExcludedPeriods { get; set; } = new();
}

/// <summary>
/// Time period configuration with from and to dates
/// </summary>
public sealed class TimePeriodConfig
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}

/// <summary>
/// PD File Upload configuration - dictionary of period to file upload data
/// Key format examples: "2025-01" (monthly), "2025Q1" (quarterly), "2025" (yearly)
/// </summary>
public sealed class PDFileUploadConfig : Dictionary<string, PeriodFileUploadData>
{
}

/// <summary>
/// File upload data for a specific period
/// </summary>
public sealed class PeriodFileUploadData
{
    public string? FinancialYear { get; set; }
    public List<UploadedFileInfo> Files { get; set; } = new();
}

/// <summary>
/// Information about an uploaded file
/// </summary>
public sealed class UploadedFileInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UploadId { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
}

/// <summary>
/// Wrapper for PD configurations
/// </summary>
public sealed class PDConfigurationsWrapper
{
    public List<PDConfigurationItem> PdConfiguration { get; set; } = new();
    public string MultiFacilityAdjustmentRule { get; set; } = string.Empty;
    public decimal? PercentRuleThreshold { get; set; }
}

/// <summary>
/// Individual PD configuration item for a product category and segment
/// </summary>
public sealed class PDConfigurationItem
{
    public string ProductCategoryId { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string PdEstimationApproach { get; set; } = string.Empty;
    public string ComparisonPeriod { get; set; } = string.Empty;
    public bool ConsiderNormalMaturities { get; set; }
    public bool AdvancedDefaultSearch { get; set; }
  public string Comments { get; set; } = string.Empty;
}
