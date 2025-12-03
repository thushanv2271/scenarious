using System.Text.Json;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Services;
using Application.IndividualImpairment.DTOs;
using Domain.IndividualImpairment;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.IndividualImpairment.CalculateCustomerImpairment;

/// <summary>
/// Handler to calculate and aggregate impairment across all facilities of a customer
/// </summary>
internal sealed class CalculateCustomerImpairmentCommandHandler(
    ICashFlowDiscountingService discountingService,
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider,
    ILogger<CalculateCustomerImpairmentCommandHandler> logger)
    : ICommandHandler<CalculateCustomerImpairmentCommand, CustomerImpairmentResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<Result<CustomerImpairmentResponse>> Handle(
        CalculateCustomerImpairmentCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!command.Facilities.Any())
            {
                return Result.Failure<CustomerImpairmentResponse>(
                    IndividualImpairmentErrors.NoFacilitiesProvided);
            }

            var facilityDetails = new List<FacilityImpairmentDetail>();
            decimal customerAmortizedCost = 0;
            decimal customerSumOfPV = 0;

            // Process each facility
            foreach (FacilityScenarioInput facility in command.Facilities)
            {
                // Validate scenarios
                Result validationResult = ValidateScenarios(facility);
                if (validationResult.IsFailure)
                {
                    return Result.Failure<CustomerImpairmentResponse>(validationResult.Error);
                }

                // Calculate facility impairment
                var scenarioResults = new List<ScenarioResult>();

                foreach (ScenarioCashFlowInput scenario in facility.Scenarios)
                {
                    ScenarioResult result = discountingService.CalculateScenarioResult(
                        scenario,
                        facility.InterestRate);

                    scenarioResults.Add(result);
                }

                // Calculate weighted sum of PV for this facility
                decimal facilityPV = discountingService.CalculateWeightedSumOfPV(scenarioResults);
                decimal facilityImpairment = facility.AmortizedCost - facilityPV;
                decimal facilityImpairmentPercentage = facility.AmortizedCost > 0
                    ? facilityImpairment / facility.AmortizedCost * 100
                    : 0;

                // Accumulate customer-level totals
                customerAmortizedCost += facility.AmortizedCost;
                customerSumOfPV += facilityPV;

                // Save to database if requested
                var calculationId = Guid.NewGuid();
                if (command.SaveToDatabase)
                {
                    calculationId = await SaveFacilityCalculationAsync(
                        facility,
                        facilityPV,
                        facilityImpairment,
                        scenarioResults,
                        command.CustomerNumber,
                        cancellationToken);
                }

                // Add to response
                facilityDetails.Add(new FacilityImpairmentDetail
                {
                    CalculationId = calculationId,
                    FacilityNumber = facility.FacilityNumber,
                    InterestRate = facility.InterestRate,
                    AmortizedCost = facility.AmortizedCost,
                    SumOfPVOfCashFlows = Math.Round(facilityPV, 2),
                    ImpairmentAmount = Math.Round(facilityImpairment, 2),
                    ImpairmentPercentage = Math.Round(facilityImpairmentPercentage, 2)
                });

                logger.LogInformation(
                    "Calculated impairment for facility {FacilityNumber}: Amortized Cost={AmortizedCost}, PV={PV}, Impairment={Impairment}",
                    facility.FacilityNumber, facility.AmortizedCost, facilityPV, facilityImpairment);
            }

            // Calculate customer-level aggregates
            decimal customerImpairmentAmount = customerAmortizedCost - customerSumOfPV;
            decimal customerImpairmentPercentage = customerAmortizedCost > 0
                ? customerImpairmentAmount / customerAmortizedCost * 100
                : 0;

            var response = new CustomerImpairmentResponse
            {
                CustomerNumber = command.CustomerNumber,
                CalculationDate = dateTimeProvider.UtcNow,
                TotalFacilities = facilityDetails.Count,
                CustomerAmortizedCost = Math.Round(customerAmortizedCost, 2),
                CustomerSumOfPV = Math.Round(customerSumOfPV, 2),
                CustomerImpairmentAmount = Math.Round(customerImpairmentAmount, 2),
                CustomerImpairmentPercentage = Math.Round(customerImpairmentPercentage, 2),
                Facilities = facilityDetails
            };

            logger.LogInformation(
                "Completed customer-level impairment calculation for {CustomerNumber}: {FacilityCount} facilities, Total Impairment={Impairment}",
                command.CustomerNumber, facilityDetails.Count, customerImpairmentAmount);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating customer impairment for {CustomerNumber}", command.CustomerNumber);
            return Result.Failure<CustomerImpairmentResponse>(
                Error.Failure("CustomerImpairment.CalculationError", $"Error calculating customer impairment: {ex.Message}"));
        }
    }

    /// <summary>
    /// Validates facility scenarios
    /// </summary>
    private static Result ValidateScenarios(FacilityScenarioInput facility)
    {
        if (!facility.Scenarios.Any())
        {
            return Result.Failure(
                IndividualImpairmentErrors.NoScenarios);
        }

        decimal totalProbability = facility.Scenarios.Sum(s => s.Probability);
        if (Math.Abs(totalProbability - 1.0m) > 0.01m)
        {
            return Result.Failure(
                IndividualImpairmentErrors.InvalidProbabilitySum);
        }

        foreach (ScenarioCashFlowInput scenario in facility.Scenarios)
        {
            if (scenario.Probability < 0 || scenario.Probability > 1)
            {
                return Result.Failure(
                    IndividualImpairmentErrors.InvalidScenarioProbability);
            }

            if (!scenario.CashFlows.Any())
            {
                return Result.Failure(
                    IndividualImpairmentErrors.NoCashFlows);
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Saves facility calculation to database
    /// </summary>
    private async Task<Guid> SaveFacilityCalculationAsync(
        FacilityScenarioInput facility,
        decimal sumOfPV,
        decimal impairmentAmount,
        List<ScenarioResult> scenarioResults,
        string customerNumber,
        CancellationToken cancellationToken)
    {
        string scenarioDetailsJson = JsonSerializer.Serialize(scenarioResults, JsonOptions);

        var calculation = IndividualImpairmentCalculation.Create(
            facility.FacilityNumber,
            customerNumber,
            facility.InterestRate,
            facility.AmortizedCost,
            sumOfPV,
            impairmentAmount,
            scenarioDetailsJson,
            userContext.UserId);

        context.IndividualImpairmentCalculations.Add(calculation);
        await context.SaveChangesAsync(cancellationToken);

        return calculation.Id;
    }
}
