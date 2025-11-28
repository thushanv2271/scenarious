namespace Web.Api.Endpoints.RiskEvaluations;

/// <summary>
/// Request model for each indicator evaluation entry.
/// </summary>
public sealed record IndicatorEvaluationRequest(
    Guid IndicatorId,
    string Value
);
