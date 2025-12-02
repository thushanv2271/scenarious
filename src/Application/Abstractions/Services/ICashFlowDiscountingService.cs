using Application.IndividualImpairment.DTOs;

namespace Application.Abstractions.Services;

public interface ICashFlowDiscountingService
{
    /// <summary>
    /// Calculates the discount factor for a given month
    /// Formula: DF = 1 / (1 + InterestRate)^Month
    /// </summary>
    decimal CalculateDiscountFactor(decimal interestRate, int month);

    /// <summary>
    /// Calculates the present value of a cash flow
    /// Formula: PV = CashFlow × DiscountFactor
    /// </summary>
    decimal CalculatePresentValue(decimal cashFlow, decimal discountFactor);

    /// <summary>
    /// Processes all cash flows for a scenario and returns discounted results
    /// </summary>
    ScenarioResult CalculateScenarioResult(
        ScenarioCashFlowInput scenario,
        decimal interestRate);

    /// <summary>
    /// Calculates the weighted sum of PV across all scenarios
    /// Formula: Sum of (Scenario PV × Probability)
    /// </summary>
    decimal CalculateWeightedSumOfPV(List<ScenarioResult> scenarioResults);
}
