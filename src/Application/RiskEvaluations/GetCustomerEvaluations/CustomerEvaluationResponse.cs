using System;
using System.Collections.Generic;

namespace Application.RiskEvaluations.GetCustomerEvaluations;

// Response model for a customer's risk evaluation
public sealed record CustomerEvaluationResponse
{
    public Guid EvaluationId { get; init; }                    // Unique evaluation ID
    public string CustomerNumber { get; init; } = string.Empty; // Customer identifier
    public DateTime EvaluationDate { get; init; }              // Date of evaluation
    public string OverallStatus { get; init; } = string.Empty; // Overall status value
    public Guid EvaluatedBy { get; init; }                     // User who performed evaluation
    public DateTime CreatedAt { get; init; }                   // Record creation time
    public List<IndicatorEvaluationDetail> IndicatorEvaluations { get; init; } = new(); // Indicator list
}
