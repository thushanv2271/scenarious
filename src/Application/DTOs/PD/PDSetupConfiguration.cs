namespace Application.DTOs.PD;

/// <summary>
/// Strongly typed representation of the PDSetupJson configuration
/// </summary>
public sealed class PDSetupConfiguration
{
    public Step1Configuration Step1 { get; set; } = new();
    public Step2Configuration Step2 { get; set; } = new();
    public Step3Configuration Step3 { get; set; } = new();
    public Step4Configuration Step4 { get; set; } = new();
    public Step5Configuration Step5 { get; set; } = new();
    public Step6Configuration Step6 { get; set; } = new();
}

/// <summary>
/// Step 1 configuration for impairment type
/// </summary>
public sealed class Step1Configuration
{
    public string ImpairmentType { get; set; } = string.Empty;
}

/// <summary>
/// Step 2 configuration - currently empty but can be extended
/// </summary>
public sealed class Step2Configuration
{
    // Currently empty based on schema, but can be extended in the future
}

/// <summary>
/// Step 3 configuration containing date passed due buckets
/// </summary>
public sealed class Step3Configuration
{
    public List<PDSetupBucket> DatePassedDueBuckets { get; set; } = new();
}

/// <summary>
/// Represents a PD setup bucket with label and definition (renamed to avoid conflicts)
/// </summary>
public sealed class PDSetupBucket
{
    public string BucketLabel { get; set; } = string.Empty;
    public BucketDefinition? BucketDefinition { get; set; }
}

/// <summary>
/// Bucket definition with min and max days
/// </summary>
public sealed class BucketDefinition
{
    public int? MinDays { get; set; }
    public int? MaxDays { get; set; }
}

/// <summary>
/// Step 4 configuration for time period settings
/// </summary>
public sealed class Step4Configuration
{
    public string LatestFinancialYearEnd { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public TimePeriod TimePeriod { get; set; } = new();
    public List<string> ExcludedPeriods { get; set; } = new();
    public QuarterDefinitions? QuarterDefinitions { get; set; }
}

/// <summary>
/// Time period configuration
/// </summary>
public sealed class TimePeriod
{
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
}

/// <summary>
/// Quarter definitions for quarterly frequency
/// </summary>
public sealed class QuarterDefinitions
{
    public QuarterPeriod? Q1 { get; set; }
    public QuarterPeriod? Q2 { get; set; }
    public QuarterPeriod? Q3 { get; set; }
    public QuarterPeriod? Q4 { get; set; }
}

/// <summary>
/// Quarter period definition
/// </summary>
public sealed class QuarterPeriod
{
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
}

/// <summary>
/// Step 5 configuration - currently empty but can be extended
/// </summary>
public sealed class Step5Configuration
{
    // Currently empty based on schema, but can be extended in the future
}

/// <summary>
/// Step 6 configuration for PD configurations and multi-facility adjustment rules
/// </summary>
public sealed class Step6Configuration
{
    public List<PDConfiguration> PDConfiguration { get; set; } = new();
    public string MultiFacilityAdjustmentRule { get; set; } = string.Empty;
    public decimal? PercentRuleThreshold { get; set; }
}

/// <summary>
/// PD configuration for specific product category and segment combinations
/// </summary>
public sealed class PDConfiguration
{
    public string ProductCategoryId { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string PDEstimationApproach { get; set; } = string.Empty;
    public string ComparisonPeriod { get; set; } = string.Empty;
    public bool ConsiderNormalMaturities { get; set; }
    public bool ADS1 { get; set; }
    public bool ADS2 { get; set; }
    public string Comments { get; set; } = string.Empty;
}
