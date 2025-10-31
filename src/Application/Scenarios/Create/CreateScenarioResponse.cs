namespace Application.Scenarios.Create;

public sealed record CreateScenarioResponse(
    bool Success,
    ScenarioData Data
);

public sealed record ScenarioData(
    Guid SegmentId,
    List<Guid> ScenarioIds
);
