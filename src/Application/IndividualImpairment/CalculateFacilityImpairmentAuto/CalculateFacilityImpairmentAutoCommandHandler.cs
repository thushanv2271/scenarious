using System.Text;
using System.Globalization;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Services;
using Application.IndividualImpairment.CalculateFacilityImpairment;
using Application.IndividualImpairment.DTOs;
using Application.IndividualImpairment.Services;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.IndividualImpairment.CalculateFacilityImpairmentAuto;

/// <summary>
/// Handler that automatically calculates facility impairment by:
/// 1. Fetching loan details from portfolio snapshot
/// 2. Retrieving saved cash flow configurations
/// 3. Generating cash flows from various sources
/// 4. Running CF discounting model
/// </summary>
internal sealed class CalculateFacilityImpairmentAutoCommandHandler
    : ICommandHandler<CalculateFacilityImpairmentAutoCommand, FacilityImpairmentResponse>
{
    private readonly ILoanDetailsRepository _loanRepository;
    private readonly ICashFlowOrchestrationService _orchestrationService;
    private readonly ICashFlowDiscountingService _discountingService;
    private readonly ILogger<CalculateFacilityImpairmentAutoCommandHandler> _logger;

    public CalculateFacilityImpairmentAutoCommandHandler(
        ILoanDetailsRepository loanRepository,
        ICashFlowOrchestrationService orchestrationService,
        ICashFlowDiscountingService discountingService,
        ILogger<CalculateFacilityImpairmentAutoCommandHandler> logger)
    {
        _loanRepository = loanRepository;
        _orchestrationService = orchestrationService;
        _discountingService = discountingService;
        _logger = logger;
    }

    public async Task<Result<FacilityImpairmentResponse>> Handle(
        CalculateFacilityImpairmentAutoCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Starting automatic impairment calculation for facility {FacilityNumber}",
                command.FacilityNumber);

            // Step 1: Get facility loan details (Amortized Cost, Interest Rate)
            FacilityLoanDetail? loanDetail = await _loanRepository
                .GetFacilityLoanDetailsAsync(command.FacilityNumber, cancellationToken);

            if (loanDetail == null)
            {
                _logger.LogWarning("Facility not found: {FacilityNumber}", command.FacilityNumber);
                return Result.Failure<FacilityImpairmentResponse>(
                    Domain.IndividualImpairment.IndividualImpairmentErrors
                        .FacilityNotFound(command.FacilityNumber));
            }

            _logger.LogInformation(
                "Retrieved loan details for facility {FacilityNumber}: AmortizedCost={AmortizedCost}, InterestRate={InterestRate}",
                command.FacilityNumber, loanDetail.TotalOutstanding, loanDetail.InterestRate);

            // Step 2: Build scenario cash flows from saved configurations
            Result<List<ScenarioCashFlowInput>> scenariosResult = await _orchestrationService
                .BuildScenarioCashFlowsAsync(
                    command.FacilityNumber,
                    loanDetail.InterestRate,
                    cancellationToken);

            if (scenariosResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to build scenario cash flows for facility {FacilityNumber}: {Error}",
                    command.FacilityNumber, scenariosResult.Error.Description);
                return Result.Failure<FacilityImpairmentResponse>(scenariosResult.Error);
            }

            List<ScenarioCashFlowInput> scenarios = scenariosResult.Value;

            if (!scenarios.Any())
            {
                _logger.LogWarning(
                    "No scenarios found for facility {FacilityNumber}",
                    command.FacilityNumber);
                return Result.Failure<FacilityImpairmentResponse>(
                    Error.NotFound(
                        "Scenarios.NotFound",
                        $"No cash flow configurations found for facility {command.FacilityNumber}"));
            }

            _logger.LogInformation(
                "Built {ScenarioCount} scenarios for facility {FacilityNumber}",
                scenarios.Count, command.FacilityNumber);

            // Step 3: Calculate impairment using CF Discounting
            var scenarioResults = new List<ScenarioResult>();

            foreach (ScenarioCashFlowInput scenario in scenarios)
            {
                ScenarioResult result = _discountingService.CalculateScenarioResult(
                    scenario,
                    loanDetail.InterestRate);

                scenarioResults.Add(result);

                _logger.LogDebug(
                    "Scenario {ScenarioName}: {CashFlowCount} cash flows, SumOfPV={SumOfPV}, WeightedPV={WeightedPV}",
                    scenario.ScenarioName,
                    scenario.CashFlows.Count,
                    result.ScenarioSumOfPV,
                    result.WeightedPV);
            }

            decimal sumOfPVOfCashFlows = _discountingService.CalculateWeightedSumOfPV(scenarioResults);
            decimal impairmentAmount = loanDetail.TotalOutstanding - sumOfPVOfCashFlows;
            decimal impairmentPercentage = loanDetail.TotalOutstanding > 0
                ? impairmentAmount / loanDetail.TotalOutstanding * 100
                : 0;

            _logger.LogInformation(
                "Impairment calculation completed for facility {FacilityNumber}: " +
                "AmortizedCost={AmortizedCost}, SumOfPV={SumOfPV}, Impairment={Impairment} ({Percentage}%)",
                command.FacilityNumber,
                loanDetail.TotalOutstanding,
                sumOfPVOfCashFlows,
                impairmentAmount,
                impairmentPercentage);

            // Step 4: Build calculation summary
            CalculationSummary calculationSummary = BuildCalculationSummary(
                scenarioResults,
                sumOfPVOfCashFlows);

            // Step 5: Return response
            var response = new FacilityImpairmentResponse
            {
                FacilityNumber = command.FacilityNumber,
                CustomerNumber = loanDetail.CustomerNumber,
                CalculationDate = DateTime.UtcNow,
                InterestRate = loanDetail.InterestRate,
                AmortizedCost = loanDetail.TotalOutstanding,
                SumOfPVOfCashFlows = sumOfPVOfCashFlows,
                ImpairmentAmount = Math.Round(impairmentAmount, 2),
                ImpairmentPercentage = Math.Round(impairmentPercentage, 2),
                Scenarios = scenarioResults,
                CalculationSummary = calculationSummary
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error calculating impairment for facility {FacilityNumber}",
                command.FacilityNumber);

            return Result.Failure<FacilityImpairmentResponse>(
                Error.Failure(
                    "FacilityImpairment.CalculationError",
                    $"An error occurred while calculating impairment: {ex.Message}"));
        }
    }

    private static CalculationSummary BuildCalculationSummary(
        List<ScenarioResult> scenarioResults,
        decimal totalWeightedPV)
    {
        var formulaBuilder = new StringBuilder();
        var calculationBuilder = new StringBuilder();

        for (int i = 0; i < scenarioResults.Count; i++)
        {
            ScenarioResult scenario = scenarioResults[i];

            if (i > 0)
            {
                formulaBuilder.Append(" + ");
                calculationBuilder.Append(" + ");
            }

            formulaBuilder.Append(CultureInfo.InvariantCulture,
                $"({scenario.ScenarioSumOfPV:N2} × {scenario.Probability:N2})");
            calculationBuilder.Append(CultureInfo.InvariantCulture,
                $"{scenario.WeightedPV:N2}");
        }

        calculationBuilder.Append(CultureInfo.InvariantCulture,
            $" = {totalWeightedPV:N2}");

        return new CalculationSummary
        {
            TotalWeightedPV = totalWeightedPV,
            Formula = formulaBuilder.ToString(),
            Calculation = calculationBuilder.ToString()
        };
    }
}
