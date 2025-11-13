namespace Application.Scenarios.Create;
public sealed record ScenarioDataResponse(
    Guid SegmentId,
    string SegmentName,
    Guid ProductCategoryId,
    string ProductCategoryName,
    List<CreatedScenarioDetailResponse> Scenarios
);
