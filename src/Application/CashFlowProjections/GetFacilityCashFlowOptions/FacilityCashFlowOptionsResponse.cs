using Domain.FacilityCashFlowTypes;

namespace Application.CashFlowProjections.GetFacilityCashFlowOptions;

/// <summary>
/// Response containing facility details with available scenarios and configured cash flow types
/// </summary>
public sealed record FacilityCashFlowOptionsResponse
{
    public string FacilityNumber { get; init; } = string.Empty;
    public string CustomerNumber { get; init; } = string.Empty;
    public string ProductCategory { get; init; } = string.Empty;
    public string Segment { get; init; } = string.Empty;
    public Guid SegmentId { get; init; }
    public List<ScenarioOptionResponse> AvailableScenarios { get; init; } = new();
}

/// <summary>
/// Scenario with its cash flow configuration
/// </summary>
public sealed record ScenarioOptionResponse
{
    public Guid ScenarioId { get; init; }
    public string ScenarioName { get; init; } = string.Empty;
    public decimal Probability { get; init; }
    public bool ContractualCashFlowsEnabled { get; init; }
    public bool LastQuarterCashFlowsEnabled { get; init; }
    public bool OtherCashFlowsEnabled { get; init; }
    public bool CollateralValueEnabled { get; init; }
    public CashFlowTypeConfigurationResponse? SavedCashFlowConfiguration { get; init; }
}

/// <summary>
/// Saved cash flow type configuration for a scenario
/// </summary>
public sealed record CashFlowTypeConfigurationResponse
{
    public Guid Id { get; init; }
    public CashFlowsType CashFlowType { get; init; }
    public string CashFlowTypeName { get; init; } = string.Empty;
    public object Configuration { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
}
