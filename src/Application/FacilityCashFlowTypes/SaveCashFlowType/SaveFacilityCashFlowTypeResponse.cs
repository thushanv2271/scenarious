using Domain.FacilityCashFlowTypes;

namespace Application.FacilityCashFlowTypes.SaveCashFlowType;

/// <summary>
/// Response after successfully saving a facility cash flow type
/// </summary>
public sealed record SaveFacilityCashFlowTypeResponse
{
    public Guid Id { get; init; }
    public string FacilityNumber { get; init; } = string.Empty;
    public Guid SegmentId { get; init; }
    public string SegmentName { get; init; } = string.Empty;
    public Guid ScenarioId { get; init; }
    public string ScenarioName { get; init; } = string.Empty;
    public CashFlowsType CashFlowType { get; init; }
    public string CashFlowTypeName { get; init; } = string.Empty;
    public object Configuration { get; init; } = new();
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid CreatedBy { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
}
