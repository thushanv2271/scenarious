using System.Globalization;
using System.Text;
using System.Text.Json;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Services;
using Application.IndividualImpairment.DTOs;
using Application.IndividualImpairment.Services;
using Domain.IndividualImpairment;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.IndividualImpairment.CalculateFacilityImpairment;

/// <summary>
/// Handler to calculate individual impairment for a single facility
/// Automatically fetches data from database - minimal payload required
/// </summary>
internal sealed class CalculateFacilityImpairmentCommandHandler(
    ILoanDetailsRepository loanRepository,
    ICashFlowOrchestrationService orchestrationService,
    ICashFlowDiscountingService discountingService,
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider,
    ILogger<CalculateFacilityImpairmentCommandHandler> logger)
    : ICommandHandler<CalculateFacilityImpairmentCommand, FacilityImpairmentResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<Result<FacilityImpairmentResponse>> Handle(
        CalculateFacilityImpairmentCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Starting facility impairment calculation for facility {FacilityNumber}",
                command.FacilityNumber);

            // Step 1: Get facility loan details from database
            FacilityLoanDetail? loanDetail = await loanRepository
                .GetFacilityLoanDetailsAsync(command.FacilityNumber, cancellationToken);

            if (loanDetail == null)
            {
                logger.LogWarning("Facility not found: {FacilityNumber}", command.FacilityNumber);
                return Result.Failure<FacilityImpairmentResponse>(
                    IndividualImpairmentErrors.FacilityNotFound(command.FacilityNumber));
            }

            // Step 2: Apply overrides or use database values
            decimal amortizedCost = command.Overrides?.AmortizedCost ?? loanDetail.TotalOutstanding;
            decimal interestRate = command.Overrides?.InterestRate ?? loanDetail.InterestRate / 100;

            logger.LogInformation(
                "Facility {FacilityNumber}: AmortizedCost={AmortizedCost}, InterestRate={InterestRate}",
                command.FacilityNumber, amortizedCost, interestRate);

            // Step 3: Build scenario cash flows from saved configurations
            Result<List<ScenarioCashFlowInput>> scenariosResult = await orchestrationService
                .BuildScenarioCashFlowsAsync(
                    command.FacilityNumber,
                    interestRate,
                    cancellationToken);

            if (scenariosResult.IsFailure)
            {
                logger.LogError(
                    "Failed to build scenario cash flows for facility {FacilityNumber}: {Error}",
                    command.FacilityNumber, scenariosResult.Error.Description);
                return Result.Failure<FacilityImpairmentResponse>(scenariosResult.Error);
            }

            List<ScenarioCashFlowInput> scenarios = scenariosResult.Value;

            // Step 4: Filter to specific scenario if override provided
            if (command.Overrides?.ScenarioId.HasValue == true)
            {
                scenarios = scenarios
                    .Where(s => s.ScenarioId == command.Overrides.ScenarioId.Value)
                    .ToList();

                if (!scenarios.Any())
                {
                    return Result.Failure<FacilityImpairmentResponse>(
                        Error.NotFound(
                            "Scenario.NotFound",
                            $"Scenario {command.Overrides.ScenarioId} not found for facility {command.FacilityNumber}"));
                }

                // Adjust probability to 100% for single scenario
                scenarios = scenarios
                    .Select(s => s with { Probability = 1.0m })
                    .ToList();
            }

            if (!scenarios.Any())
            {
                logger.LogWarning(
                    "No scenarios found for facility {FacilityNumber}",
                    command.FacilityNumber);
                return Result.Failure<FacilityImpairmentResponse>(
                    Error.NotFound(
                        "Scenarios.NotFound",
                        $"No cash flow configurations found for facility {command.FacilityNumber}"));
            }

            logger.LogInformation(
                "Built {ScenarioCount} scenarios for facility {FacilityNumber}",
                scenarios.Count, command.FacilityNumber);

            // Step 5: Calculate scenario results
            var scenarioResults = new List<ScenarioResult>();
            foreach (ScenarioCashFlowInput scenario in scenarios)
            {
                ScenarioResult result = discountingService.CalculateScenarioResult(
                    scenario,
                    interestRate);

                scenarioResults.Add(result);

                logger.LogDebug(
                    "Scenario {ScenarioName}: {CashFlowCount} cash flows, SumOfPV={SumOfPV}, WeightedPV={WeightedPV}",
                    scenario.ScenarioName,
                    scenario.CashFlows.Count,
                    result.ScenarioSumOfPV,
                    result.WeightedPV);
            }

            // Step 6: Calculate weighted sum and impairment
            decimal sumOfPVOfCashFlows = discountingService.CalculateWeightedSumOfPV(scenarioResults);
            decimal impairmentAmount = amortizedCost - sumOfPVOfCashFlows;
            decimal impairmentPercentage = amortizedCost > 0
                ? impairmentAmount / amortizedCost * 100
                : 0;

            logger.LogInformation(
                "Impairment calculation completed for facility {FacilityNumber}: " +
                "AmortizedCost={AmortizedCost}, SumOfPV={SumOfPV}, Impairment={Impairment} ({Percentage}%)",
                command.FacilityNumber,
                amortizedCost,
                sumOfPVOfCashFlows,
                impairmentAmount,
                impairmentPercentage);

            // Step 7: Save to database if requested
            Guid calculationId = Guid.Empty;
            if (command.SaveToDatabase)
            {
                calculationId = await SaveCalculationAsync(
                    command.FacilityNumber,
                    loanDetail.CustomerNumber,
                    interestRate,
                    amortizedCost,
                    sumOfPVOfCashFlows,
                    impairmentAmount,
                    scenarioResults,
                    cancellationToken);
            }

            // Step 8: Build calculation summary
            CalculationSummary calculationSummary = BuildCalculationSummary(
                scenarioResults,
                sumOfPVOfCashFlows);

            // Step 9: Return response
            var response = new FacilityImpairmentResponse
            {
                CalculationId = calculationId,
                FacilityNumber = command.FacilityNumber,
                CustomerNumber = loanDetail.CustomerNumber,
                CalculationDate = dateTimeProvider.UtcNow,
                InterestRate = interestRate,
                AmortizedCost = amortizedCost,
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
            logger.LogError(ex,
                "Error calculating impairment for facility {FacilityNumber}",
                command.FacilityNumber);

            return Result.Failure<FacilityImpairmentResponse>(
                Error.Failure(
                    "FacilityImpairment.CalculationError",
                    $"An error occurred while calculating impairment: {ex.Message}"));
        }
    }

    /// <summary>
    /// Saves calculation to database
    /// </summary>
    private async Task<Guid> SaveCalculationAsync(
        string facilityNumber,
        string customerNumber,
        decimal interestRate,
        decimal amortizedCost,
        decimal sumOfPV,
        decimal impairmentAmount,
        List<ScenarioResult> scenarioResults,
        CancellationToken cancellationToken)
    {
        string scenarioDetailsJson = JsonSerializer.Serialize(scenarioResults, JsonOptions);

        var calculation = IndividualImpairmentCalculation.Create(
            facilityNumber,
            customerNumber,
            interestRate,
            amortizedCost,
            sumOfPV,
            impairmentAmount,
            scenarioDetailsJson,
            userContext.UserId);

        context.IndividualImpairmentCalculations.Add(calculation);
        await context.SaveChangesAsync(cancellationToken);

        return calculation.Id;
    }

    /// <summary>
    /// Builds calculation summary with formula
    /// </summary>
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
