namespace Web.Api.Endpoints.Scenarios;

public sealed record ScenarioItemRequest(
    string ScenarioName,
    decimal Probability,
    bool ContractualCashFlowsEnabled,
    bool LastQuarterCashFlowsEnabled,
    bool OtherCashFlowsEnabled,
    bool CollateralValueEnabled,
    UploadFileRequest? UploadFile
);
