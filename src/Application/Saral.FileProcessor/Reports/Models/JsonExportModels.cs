namespace Saral.FileProcessor.Reports.Models;

public sealed record JsonExportResult
{
    [JsonPropertyName("summary")]
    public required JsonSummary Summary { get; init; }

    [JsonPropertyName("data")]
    public required IReadOnlyList<JsonFileData> Data { get; init; }

    [JsonPropertyName("pagination")]
    public required JsonPagination Pagination { get; init; }
}

public sealed record JsonSummary
{
    [JsonPropertyName("totalFiles")]
    public int TotalFiles { get; init; }

    [JsonPropertyName("filesWithErrors")]
    public int FilesWithErrors { get; init; }

    [JsonPropertyName("readyForUse")]
    public int ReadyForUse { get; init; }

    [JsonPropertyName("totalRowsFromValidFiles")]
    public long TotalRowsFromValidFiles { get; init; }

    [JsonPropertyName("reportPeriod")]
    public required string ReportPeriod { get; init; }
}

public sealed record JsonFileData
{
    [JsonPropertyName("fileId")]
    public required string FileId { get; init; }

    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    [JsonPropertyName("totalRows")]
    public long TotalRows { get; init; }

    [JsonPropertyName("schemaErrors")]
    public long SchemaErrors { get; init; }

    [JsonPropertyName("nulls")]
    public long Nulls { get; init; }

    [JsonPropertyName("rangeErrors")]
    public long RangeErrors { get; init; }

    [JsonPropertyName("referentialErrors")]
    public long ReferentialErrors { get; init; }

    [JsonPropertyName("duplicates")]
    public long Duplicates { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("validationErrors")]
    public long ValidationErrors { get; init; }

    [JsonPropertyName("validationSuccessRate")]
    public double ValidationSuccessRate { get; init; }
}

public sealed record JsonPagination
{
    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; init; }

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }
}