using System.Globalization;
using System.Text;
using Application.Abstractions.Messaging;
using Application.Abstractions.Services;
using Application.IndividualImpairment.DTOs;
using SharedKernel;

namespace Application.IndividualImpairment.CalculateFacilityImpairment;

internal sealed class CalculateFacilityImpairmentCommandHandler
    : ICommandHandler<CalculateFacilityImpairmentCommand, FacilityImpairmentResponse>
{
    private readonly ICashFlowDiscountingService _discountingService;

    public CalculateFacilityImpairmentCommandHandler(
        ICashFlowDiscountingService discountingService)
    {
        _discountingService = discountingService;
    }

    public async Task<Result<FacilityImpairmentResponse>> Handle(
        CalculateFacilityImpairmentCommand command,
        CancellationToken cancellationToken)
    {
        // Calculate scenario results
        List<ScenarioResult> scenarioResults = new();

        foreach (ScenarioCashFlowInput scenario in command.Scenarios)
        {
            ScenarioResult result = _discountingService.CalculateScenarioResult(
                scenario,
                command.InterestRate);

            scenarioResults.Add(result);
        }

        // Calculate weighted sum of PV
        decimal sumOfPVOfCashFlows = _discountingService.CalculateWeightedSumOfPV(scenarioResults);

        // Calculate impairment
        decimal impairmentAmount = command.AmortizedCost - sumOfPVOfCashFlows;

        // Calculate impairment percentage
        decimal impairmentPercentage = command.AmortizedCost > 0
            ? impairmentAmount / command.AmortizedCost * 100
            : 0;

        // Build calculation summary
        CalculationSummary calculationSummary = BuildCalculationSummary(scenarioResults, sumOfPVOfCashFlows);

        FacilityImpairmentResponse response = new()
        {
            FacilityNumber = command.FacilityNumber,
            CustomerNumber = command.CustomerNumber,
            CalculationDate = DateTime.UtcNow,
            InterestRate = command.InterestRate,
            AmortizedCost = command.AmortizedCost,
            SumOfPVOfCashFlows = sumOfPVOfCashFlows,
            ImpairmentAmount = Math.Round(impairmentAmount, 2),
            ImpairmentPercentage = Math.Round(impairmentPercentage, 2),
            Scenarios = scenarioResults,
            CalculationSummary = calculationSummary
        };

        return Result.Success(response);
    }

    private static CalculationSummary BuildCalculationSummary(
        List<ScenarioResult> scenarioResults,
        decimal totalWeightedPV)
    {
        StringBuilder formulaBuilder = new();
        StringBuilder calculationBuilder = new();

        for (int i = 0; i < scenarioResults.Count; i++)
        {
            ScenarioResult scenario = scenarioResults[i];

            if (i > 0)
            {
                formulaBuilder.Append(" + ");
                calculationBuilder.Append(" + ");
            }

            formulaBuilder.Append(CultureInfo.InvariantCulture, $"({scenario.ScenarioSumOfPV:N2} × {scenario.Probability:N2})");
            calculationBuilder.Append(CultureInfo.InvariantCulture, $"{scenario.WeightedPV:N2}");
        }

        calculationBuilder.Append(CultureInfo.InvariantCulture, $" = {totalWeightedPV:N2}");

        return new CalculationSummary
        {
            TotalWeightedPV = totalWeightedPV,
            Formula = formulaBuilder.ToString(),
            Calculation = calculationBuilder.ToString()
        };
    }
}
