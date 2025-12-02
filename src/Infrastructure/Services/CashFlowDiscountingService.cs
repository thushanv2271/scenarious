using Application.Abstractions.Services;
using Application.IndividualImpairment.DTOs;

namespace Infrastructure.Services;

public sealed class CashFlowDiscountingService : ICashFlowDiscountingService
{
    public decimal CalculateDiscountFactor(decimal interestRate, int month)
    {
        if (month <= 0)
        {
            throw new ArgumentException("Month must be greater than zero", nameof(month));
        }

        if (interestRate < 0 || interestRate > 1)
        {
            throw new ArgumentException("Interest rate must be between 0 and 1", nameof(interestRate));
        }

        // Formula: DF = 1 / (1 + r)^n
        // Using monthly compounding
        decimal monthlyRate = interestRate / 12;
        decimal discountFactor = 1 / (decimal)Math.Pow((double)(1 + monthlyRate), month);

        return Math.Round(discountFactor, 10);
    }

    public decimal CalculatePresentValue(decimal cashFlow, decimal discountFactor)
    {
        if (cashFlow < 0)
        {
            throw new ArgumentException("Cash flow cannot be negative", nameof(cashFlow));
        }

        // Formula: PV = CF × DF
        decimal presentValue = cashFlow * discountFactor;

        return Math.Round(presentValue, 2);
    }

    public ScenarioResult CalculateScenarioResult(
        ScenarioCashFlowInput scenario,
        decimal interestRate)
    {
        var discountedCashFlows = new List<DiscountedCashFlowItem>();

        foreach (CashFlowItemInput cashFlow in scenario.CashFlows)
        {
            decimal discountFactor = CalculateDiscountFactor(interestRate, cashFlow.Month);
            decimal presentValue = CalculatePresentValue(cashFlow.CashFlowAmount, discountFactor);

            discountedCashFlows.Add(new DiscountedCashFlowItem
            {
                Month = cashFlow.Month,
                CashFlowAmount = cashFlow.CashFlowAmount,
                DiscountFactor = discountFactor,
                PresentValue = presentValue
            });
        }

        // Calculate scenario sum of PV
        decimal scenarioSumOfPV = discountedCashFlows.Sum(cf => cf.PresentValue);

        // Calculate weighted PV (sum × probability)
        decimal weightedPV = scenarioSumOfPV * scenario.Probability;

        return new ScenarioResult
        {
            ScenarioId = scenario.ScenarioId,
            ScenarioName = scenario.ScenarioName,
            Probability = scenario.Probability,
            DiscountedCashFlows = discountedCashFlows,
            ScenarioSumOfPV = Math.Round(scenarioSumOfPV, 2),
            WeightedPV = Math.Round(weightedPV, 2)
        };
    }

    public decimal CalculateWeightedSumOfPV(List<ScenarioResult> scenarioResults)
    {
        if (scenarioResults == null || scenarioResults.Count == 0)
        {
            return 0;
        }

        // Formula: Σ(Scenario PV × Probability)
        decimal totalWeightedPV = scenarioResults.Sum(s => s.WeightedPV);

        return Math.Round(totalWeightedPV, 2);
    }
}
