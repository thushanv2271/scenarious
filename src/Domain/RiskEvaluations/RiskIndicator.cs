using SharedKernel;

namespace Domain.RiskEvaluations;

/// 
/// Master data for risk indicators (SICR and OEIL)
/// 
public sealed class RiskIndicator : Entity
{
    public Guid IndicatorId { get; set; }
    public RiskIndicatorCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PossibleValues { get; set; } = "Yes,No,N/A"; // Comma-separated
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
