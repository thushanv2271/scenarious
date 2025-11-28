using System.Collections.Generic;
using Application.Abstractions.Messaging;

namespace Application.RiskEvaluations.CreateEvaluation;

// Represents a single indicator evaluation entry
public sealed record IndicatorEvaluationItem(
    Guid IndicatorId,     // Unique indicator reference
    string Value        // Yes / No / N/A
);
