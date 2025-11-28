using SharedKernel;

namespace Domain.RiskEvaluations;

public static class RiskEvaluationErrors
{
    public static Error CustomerNotFound(string customerNumber) => Error.NotFound(
        "RiskEvaluation.CustomerNotFound",
        $"Customer with number '{customerNumber}' was not found in loan details");

    public static Error EvaluationNotFound(int evaluationId) => Error.NotFound(
        "RiskEvaluation.EvaluationNotFound",
        $"Risk evaluation with ID '{evaluationId}' was not found");

    public static Error IndicatorNotFound(int indicatorId) => Error.NotFound(
        "RiskEvaluation.IndicatorNotFound",
        $"Risk indicator with ID '{indicatorId}' was not found");

    public static Error InvalidValue(string value) => Error.Problem(
        "RiskEvaluation.InvalidValue",
        $"Invalid value '{value}'. Allowed values are: Yes, No, N/A");

    public static Error DuplicateEvaluation => Error.Conflict(
        "RiskEvaluation.DuplicateEvaluation",
        "An evaluation for this customer on this date already exists");
}
