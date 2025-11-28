using SharedKernel;

namespace Application.ProductCategories;

public sealed record SearchProductCategoriesAndSegmentsCommand(string? Type = null);

public sealed record ProductCategoryWithSegmentsResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<SegmentResponse> Segments { get; init; } = [];
}

public sealed record SegmentResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record SearchProductCategoriesAndSegmentsResponse
{
    public List<ProductCategoryWithSegmentsResponse> ProductCategories { get; init; } = [];
    public int TotalCount { get; init; }
    public string? FilteredByType { get; init; }
}