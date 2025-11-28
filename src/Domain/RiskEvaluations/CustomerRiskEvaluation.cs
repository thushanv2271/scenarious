using System.Collections.Generic;
using SharedKernel;

namespace Domain.RiskEvaluations;

/// 
/// Customer risk evaluation header
/// 
public sealed class CustomerRiskEvaluation : Entity
{
    public Guid EvaluationId { get; set; }
    public string CustomerNumber { get; set; } = string.Empty;
    public DateTime EvaluationDate { get; set; }
    public Guid EvaluatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public List<CustomerRiskIndicatorEvaluation> IndicatorEvaluations { get; set; } = new();
}
