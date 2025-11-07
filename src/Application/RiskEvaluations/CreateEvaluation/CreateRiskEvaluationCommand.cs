using System.Collections.Generic;
using Application.Abstractions.Messaging;

namespace Application.RiskEvaluations.CreateEvaluation;

// Command used to create a new risk evaluation
public sealed record CreateRiskEvaluationCommand(
    string CustomerNumber,                     // Customer identifier
    DateTime EvaluationDate,                   // Date of evaluation
    string OverallStatus,                      // Overall evaluation status
    List<IndicatorEvaluationItem> IndicatorEvaluations // List of indicator evaluations
) : ICommand<Guid>;                            // Returns created record Id
