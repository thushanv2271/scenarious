// src/Application/EclAnalysis/CalculateThresholdSummary/ThresholdCalculator.cs
using System.Collections.Generic;
using System.Linq;
using Domain.EclAnalysis;
using SharedKernel;

namespace Application.EclAnalysis.CalculateThresholdSummary;

/// <summary>
/// Calculates threshold classifications based on different threshold types
/// </summary>
internal static class ThresholdCalculator
{
    public static Result<ThresholdCalculationResult> Calculate(
        ThresholdType thresholdType,
        List<LoanDetailRawDto> customerExposures,
        decimal? individualSignificantThreshold,
        int? topNCount,
        decimal? cumulativePercentageThreshold)
    {
        return thresholdType switch
        {
            ThresholdType.CustomAbsolute => CalculateCustomAbsolute(
                customerExposures,
                individualSignificantThreshold!.Value),

            ThresholdType.TopNCustomers => CalculateTopNCustomers(
                customerExposures,
                topNCount!.Value),

            ThresholdType.CumulativePercentage => CalculateCumulativePercentage(
                customerExposures,
                cumulativePercentageThreshold!.Value),

            _ => Result.Failure<ThresholdCalculationResult>(EclAnalysisErrors.InvalidThresholdType)
        };
    }

    /// <summary>
    /// Calculate threshold using custom absolute amount
    /// </summary>
    private static Result<ThresholdCalculationResult> CalculateCustomAbsolute(
        List<LoanDetailRawDto> customerExposures,
        decimal threshold)
    {
        if (threshold <= 0)
        {
            return Result.Failure<ThresholdCalculationResult>(EclAnalysisErrors.InvalidThreshold);
        }

        var individualCustomers = customerExposures
            .Where(c => c.TotalOs >= threshold)
            .ToList();

        var collectiveCustomers = customerExposures
            .Where(c => c.TotalOs < threshold)
            .ToList();

        return Result.Success(new ThresholdCalculationResult
        {
            ThresholdDescription = $"LKR {threshold:N0}",
            IndividualCustomerCount = individualCustomers.Count,
            IndividualCost = individualCustomers.Sum(c => c.TotalOs),
            CollectiveCustomerCount = collectiveCustomers.Count,
            CollectiveCost = collectiveCustomers.Sum(c => c.TotalOs)
        });
    }

    /// <summary>
    /// Calculate threshold by selecting top N customers
    /// </summary>
    private static Result<ThresholdCalculationResult> CalculateTopNCustomers(
        List<LoanDetailRawDto> customerExposures,
        int topN)
    {
        if (topN <= 0)
        {
            return Result.Failure<ThresholdCalculationResult>(EclAnalysisErrors.InvalidTopNCount);
        }

        int totalCustomers = customerExposures.Count;

        if (topN > totalCustomers)
        {
            return Result.Failure<ThresholdCalculationResult>(
                EclAnalysisErrors.TopNExceedsCustomers(topN, totalCustomers));
        }

        // Data is already sorted in descending order by the SQL query
        var individualCustomers = customerExposures.Take(topN).ToList();
        var collectiveCustomers = customerExposures.Skip(topN).ToList();

        // The threshold is the minimum exposure in the individual group
        decimal effectiveThreshold = individualCustomers[individualCustomers.Count - 1].TotalOs;

        return Result.Success(new ThresholdCalculationResult
        {
            ThresholdDescription = $"Top {topN} Customers (Threshold: LKR {effectiveThreshold:N0})",
            IndividualCustomerCount = individualCustomers.Count,
            IndividualCost = individualCustomers.Sum(c => c.TotalOs),
            CollectiveCustomerCount = collectiveCustomers.Count,
            CollectiveCost = collectiveCustomers.Sum(c => c.TotalOs)
        });
    }

    /// <summary>
    /// Calculate threshold by cumulative percentage of portfolio
    /// </summary>
    private static Result<ThresholdCalculationResult> CalculateCumulativePercentage(
        List<LoanDetailRawDto> customerExposures,
        decimal cumulativePercentage)
    {
        if (cumulativePercentage <= 0 || cumulativePercentage > 100)
        {
            return Result.Failure<ThresholdCalculationResult>(EclAnalysisErrors.InvalidCumulativePercentage);
        }

        // Calculate total portfolio exposure
        decimal totalPortfolio = customerExposures.Sum(c => c.TotalOs);
        decimal targetAmount = totalPortfolio * (cumulativePercentage / 100);

        // Data is already sorted in descending order by the SQL query
        var individualCustomers = new List<LoanDetailRawDto>();
        decimal cumulativeAmount = 0;

        foreach (LoanDetailRawDto customer in customerExposures)
        {
            individualCustomers.Add(customer);
            cumulativeAmount += customer.TotalOs;

            if (cumulativeAmount >= targetAmount)
            {
                break;
            }
        }

        var collectiveCustomers = customerExposures
            .Skip(individualCustomers.Count)
            .ToList();

        // The threshold is the minimum exposure in the individual group
        decimal effectiveThreshold = individualCustomers.Count > 0
            ? individualCustomers[individualCustomers.Count - 1].TotalOs
            : 0;

        return Result.Success(new ThresholdCalculationResult
        {
            ThresholdDescription = $"{cumulativePercentage}% of Portfolio (Threshold: LKR {effectiveThreshold:N0})",
            IndividualCustomerCount = individualCustomers.Count,
            IndividualCost = individualCustomers.Sum(c => c.TotalOs),
            CollectiveCustomerCount = collectiveCustomers.Count,
            CollectiveCost = collectiveCustomers.Sum(c => c.TotalOs)
        });
    }
}
