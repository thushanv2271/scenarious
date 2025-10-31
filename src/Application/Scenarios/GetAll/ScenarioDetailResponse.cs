namespace Application.Scenarios.GetAll;

/// <summary>
/// Detailed scenario information
/// </summary>
public sealed record ScenarioDetailResponse
{
    public Guid Id { get; init; }
    public string ScenarioName { get; init; } = string.Empty;
    public decimal Probability { get; init; }
    public bool ContractualCashFlowsEnabled { get; init; }
    public bool LastQuarterCashFlowsEnabled { get; init; }
    public bool OtherCashFlowsEnabled { get; init; }
    public bool CollateralValueEnabled { get; init; }
    public Guid? UploadedFileId { get; init; }
    public UploadedFileInfo? UploadedFile { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
