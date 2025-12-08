namespace Application.PDAlgorithmResults.UpdateSelectedMethodology;

/// <summary>
/// Response after successfully updating the selected methodology
/// </summary>
public sealed record UpdateSelectedMethodologyResponse
{
    public Guid Id { get; init; }
    public string ProductCategory { get; init; } = string.Empty;
    public string Segment { get; init; } = string.Empty;
    public string SelectedMethodology { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public Guid UpdatedBy { get; init; }
}
