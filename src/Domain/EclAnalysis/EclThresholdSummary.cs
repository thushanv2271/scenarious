// src/Domain/EclAnalysis/EclThresholdSummary.cs
using System;

namespace Domain.EclAnalysis;

/// <summary>
/// Domain entity representing ECL threshold summary with categorized impairment data
/// </summary>
public sealed class EclThresholdSummary
{
    // Customers above threshold (individually assessed)
    public ImpairmentCategory Individual { get; init; } = new();

    // Customers below threshold (collectively assessed)
    public ImpairmentCategory Collective { get; init; } = new();

    // Total of all customers
    public ImpairmentCategory GrandTotal { get; init; } = new();

    // Date when summary was calculated
    public DateTime AsOfDate { get; init; }

    // Branch identifier
    public string BranchCode { get; init; } = string.Empty;

    // Threshold value used for categorization
    public decimal IndividualSignificantThreshold { get; init; }

    // Currency code for amounts
    public string Currency { get; init; } = string.Empty;
}

