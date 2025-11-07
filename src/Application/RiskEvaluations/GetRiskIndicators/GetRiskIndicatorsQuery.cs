using System.Collections.Generic;
using Application.Abstractions.Messaging;
using Domain.RiskEvaluations;

namespace Application.RiskEvaluations.GetRiskIndicators;

public sealed record GetRiskIndicatorsQuery(
    RiskIndicatorCategory? Category = null
) : IQuery<List<RiskIndicatorResponse>>;
