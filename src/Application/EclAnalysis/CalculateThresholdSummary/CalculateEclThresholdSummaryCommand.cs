using System;
using Application.Abstractions.Messaging;
using Domain.EclAnalysis;

namespace Application.EclAnalysis.CalculateThresholdSummary;

/// <summary>
/// Command to calculate ECL threshold summary for a user
/// </summary>
public sealed record CalculateEclThresholdSummaryCommand(
    // Type of threshold calculation
    ThresholdType ThresholdType,
    // The threshold value for individual significance (used for CustomAbsolute)
    decimal? IndividualSignificantThreshold,
    // Number of top customers (used for TopNCustomers)
    int? TopNCount,
    // Cumulative percentage threshold (used for CumulativePercentage)
    decimal? CumulativePercentageThreshold,
    // ID of the user requesting the calculation
    Guid UserId) : ICommand<EclThresholdSummaryResponse>;
