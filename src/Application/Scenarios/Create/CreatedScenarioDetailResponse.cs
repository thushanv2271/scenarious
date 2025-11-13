namespace Application.Scenarios.Create;

public sealed record CreatedScenarioDetailResponse(
    Guid Id,
    string ScenarioName,
    decimal Probability,
    bool ContractualCashFlowsEnabled,
    bool LastQuarterCashFlowsEnabled,
    bool OtherCashFlowsEnabled,
    bool CollateralValueEnabled,
    UploadedFileDetailResponse? UploadedFile,
    DateTime CreatedAt,
    DateTime UpdatedAt
);


