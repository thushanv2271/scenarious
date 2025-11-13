using SharedKernel;

namespace Domain.FacilityCashFlowTypes;

/// <summary>
/// Represents a cash flow type configuration for a specific facility
/// </summary>
public sealed class FacilityCashFlowType : Entity
{
    public Guid Id { get; set; }
    public string FacilityNumber { get; set; } = string.Empty;
    public Guid SegmentId { get; set; }
    public Guid ScenarioId { get; set; }
    public CashFlowsType CashFlowType { get; set; }
    public string Configuration { get; set; } = "{}"; // JSON string
    public bool IsActive { get; set; } = true;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
