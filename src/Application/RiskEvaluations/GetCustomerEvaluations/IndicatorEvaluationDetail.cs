using System;
using System.Collections.Generic;

namespace Application.RiskEvaluations.GetCustomerEvaluations;

// Represents a single indicator evaluation detail in the response
public sealed record IndicatorEvaluationDetail
{
    public Guid EvalDetailId { get; init; }             // Unique detail ID
    public Guid IndicatorId { get; init; }              // Reference to indicator
    public string IndicatorDescription { get; init; } = string.Empty; // Indicator name/description
    public string Category { get; init; } = string.Empty; // Indicator category
    public string Value { get; init; } = string.Empty;  // Yes / No / N/A
}
