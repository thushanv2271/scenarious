using System;

namespace Domain.EclAnalysis;

/// <summary>
/// Represents aggregated data for a group of customers in an impairment category
/// </summary>
public sealed class ImpairmentCategory
{
    // Total number of customers in this category
    public int CustomerCount { get; init; }

    // Sum of outstanding balances for all customers
    public decimal AmortizedCost { get; init; }
}
