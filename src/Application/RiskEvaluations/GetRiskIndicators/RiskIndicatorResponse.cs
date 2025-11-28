using System;
using System.Collections.Generic;

namespace Application.RiskEvaluations.GetRiskIndicators;

public sealed record RiskIndicatorResponse
{
    public Guid IndicatorId { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> PossibleValues { get; init; } = new();
    public int DisplayOrder { get; init; }
}
