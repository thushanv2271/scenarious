namespace Application.ProductCategories;

public sealed record CsvRowData
{
    public string Type { get; init; } = string.Empty;
    public string ProductCategory { get; init; } = string.Empty;
    public string Segment { get; init; } = string.Empty;
}

public sealed record CsvUploadResult
{
    public int ProcessedRecords { get; init; }
    public int CategoriesCreated { get; init; }
    public int SegmentsCreated { get; init; }
    public List<string> Errors { get; init; } = [];
}