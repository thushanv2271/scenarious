using System;
using Application.Abstractions.Messaging;

namespace Application.EclAnalysis.CalculateThresholdSummary;

/// <summary>
/// Command to calculate ECL threshold summary for a user
/// </summary>
public sealed record CalculateEclThresholdSummaryCommand(
    // The threshold value for individual significance
    decimal IndividualSignificantThreshold,
    // ID of the user requesting the calculation
    Guid UserId) : ICommand<EclThresholdSummaryResponse>;
