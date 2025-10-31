namespace Web.Api.Endpoints.Scenarios;

public sealed record CreateScenarioRequest(
    Guid ProductCategoryId,
    string ProductCategoryName,
    Guid SegmentId,
    string SegmentName,
    List<ScenarioItemRequest> Scenarios
);
