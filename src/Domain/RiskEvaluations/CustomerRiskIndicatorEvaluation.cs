using SharedKernel;

namespace Domain.RiskEvaluations;

/// 
/// Individual indicator evaluation for a customer
/// 
public sealed class CustomerRiskIndicatorEvaluation : Entity
{
    public Guid EvalDetailId { get; set; }
    public Guid EvaluationId { get; set; }
    public Guid IndicatorId { get; set; }
    public string Value { get; set; } = "N/A"; // Yes, No, or N/A
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public CustomerRiskEvaluation Evaluation { get; set; } = null!;
    public RiskIndicator Indicator { get; set; } = null!;
}
