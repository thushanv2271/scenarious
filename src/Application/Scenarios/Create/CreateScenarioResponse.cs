namespace Application.Scenarios.Create;

public sealed record CreateScenarioResponse(
    bool Success,
    ScenarioDataResponse Data
);
