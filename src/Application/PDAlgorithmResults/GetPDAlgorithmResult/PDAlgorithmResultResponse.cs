using System.Text.Json.Serialization;

namespace Application.PDAlgorithmResults.GetPDAlgorithmResult;

/// <summary>
/// Response containing the PD Algorithm Result data
/// </summary>
public sealed record PDAlgorithmResultResponse
{
    public Guid Id { get; init; }
    public PDAlgorithmData Data { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public Guid? UpdatedBy { get; init; }
}

/// <summary>
/// Root structure for PD Algorithm data
/// </summary>
public sealed record PDAlgorithmData
{
    [JsonPropertyName("productCategories")]
    public List<ProductCategoryData> ProductCategories { get; init; } = new();
}

/// <summary>
/// Product category containing segments
/// </summary>
public sealed record ProductCategoryData
{
    [JsonPropertyName("productCategory")]
    public string ProductCategory { get; init; } = string.Empty;

    [JsonPropertyName("segments")]
    public List<SegmentData> Segments { get; init; } = new();
}

/// <summary>
/// Segment containing method summaries and selected methodology
/// selectedMethodology is at segment level (outside summary)
/// </summary>
public sealed record SegmentData
{
    [JsonPropertyName("segment")]
    public string Segment { get; init; } = string.Empty;

    [JsonPropertyName("selectedMethodology")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SelectedMethodology { get; init; }

    [JsonPropertyName("summary")]
    public SegmentSummary Summary { get; init; } = new();
}

/// <summary>
/// Summary containing method calculations only
/// </summary>
public sealed record SegmentSummary
{
    [JsonPropertyName("method1")]
    public MethodData? Method1 { get; init; }

    [JsonPropertyName("method2")]
    public MethodData? Method2 { get; init; }

    [JsonPropertyName("method3")]
    public MethodData? Method3 { get; init; }
}

/// <summary>
/// Method data containing various PD tables
/// </summary>
public sealed record MethodData
{
    [JsonPropertyName("extrapolatedCumulativePdsBeforeEfa")]
    public PDTable? ExtrapolatedCumulativePdsBeforeEfa { get; init; }

    [JsonPropertyName("extrapolatedCumulativePdsAfterEfa")]
    public PDTable? ExtrapolatedCumulativePdsAfterEfa { get; init; }

    [JsonPropertyName("marginalPdsAfterEfa")]
    public PDTable? MarginalPdsAfterEfa { get; init; }

    [JsonPropertyName("survivalRates")]
    public PDTable? SurvivalRates { get; init; }

    [JsonPropertyName("marginalPds")]
    public PDTable? MarginalPds { get; init; }

    [JsonPropertyName("efaAdjustedPds")]
    public PDTable? EfaAdjustedPds { get; init; }
}

/// <summary>
/// PD Table structure with title, headers and rows
/// </summary>
public sealed record PDTable
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("columnHeaders")]
    public List<string> ColumnHeaders { get; init; } = new();

    [JsonPropertyName("rows")]
    public List<PDTableRow> Rows { get; init; } = new();
}

/// <summary>
/// PD Table row with age bucket and PD values by year
/// </summary>
public sealed record PDTableRow
{
    [JsonPropertyName("ageBucket")]
    public string AgeBucket { get; init; } = string.Empty;

    [JsonPropertyName("pdValuesByYear")]
    public Dictionary<string, decimal?> PdValuesByYear { get; init; } = new();
}
