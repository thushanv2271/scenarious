using System.Collections.Generic;

namespace Application.RiskEvaluations.GetRiskIndicators;

// Represents a risk indicator returned to the client
public sealed record RiskIndicatorResponse
{
    public Guid IndicatorId { get; init; }                // Unique indicator ID
    public string Category { get; init; } = string.Empty; // Indicator category
    public string Description { get; init; } = string.Empty; // Indicator description
    public List<string> PossibleValues { get; init; } = new(); // Allowed values list
    public int DisplayOrder { get; init; }               // Order for UI display
}
