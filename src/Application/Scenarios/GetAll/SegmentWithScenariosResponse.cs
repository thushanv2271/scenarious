namespace Application.Scenarios.GetAll;

/// <summary>
/// Segment information with its scenarios
/// </summary>
public sealed record SegmentWithScenariosResponse
{
    public Guid SegmentId { get; init; }
    public string SegmentName { get; init; } = string.Empty;
    public List<ScenarioDetailResponse> Scenarios { get; init; } = new();
}
