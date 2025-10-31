namespace Application.Scenarios.Create;

/// <summary>
/// Represents an individual scenario item to be created.
/// </summary>
public sealed record ScenarioItem(
    string ScenarioName,
    decimal Probability,
    bool ContractualCashFlowsEnabled,
    bool LastQuarterCashFlowsEnabled,
    bool OtherCashFlowsEnabled,
    bool CollateralValueEnabled,
    UploadFileItem? UploadFile
);
