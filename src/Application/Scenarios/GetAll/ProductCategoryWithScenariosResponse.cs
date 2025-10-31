namespace Application.Scenarios.GetAll;

/// <summary>
/// Response containing product category with all its segments and scenarios
/// </summary>
public sealed record ProductCategoryWithScenariosResponse
{
    public Guid ProductCategoryId { get; init; }
    public string ProductCategoryName { get; init; } = string.Empty;
    public List<SegmentWithScenariosResponse> Segments { get; init; } = new();
}
