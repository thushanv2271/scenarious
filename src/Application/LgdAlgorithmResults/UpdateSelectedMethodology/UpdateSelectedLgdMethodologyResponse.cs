namespace Application.LgdAlgorithmResults.UpdateSelectedMethodology;

/// <summary>
/// Response for updating selected LGD methodology
/// </summary>
public sealed record UpdateSelectedLgdMethodologyResponse
{
    public Guid Id { get; init; }
    public string ProductCategory { get; init; } = string.Empty;
    public string Segment { get; init; } = string.Empty;
    public string SelectedMethodology { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}