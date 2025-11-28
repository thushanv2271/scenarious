namespace Web.Api.Endpoints.RiskEvaluations;

/// <summary>
/// Request model for creating a customer risk evaluation.
/// </summary>
public sealed record CreateEvaluationRequest(
    string CustomerNumber,
    DateTime EvaluationDate,
    List<IndicatorEvaluationRequest> IndicatorEvaluations
);
