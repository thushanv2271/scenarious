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

namespace Application.IndividualImpairment.CalculateCustomerImpairment;

/// <summary>
/// Handler to calculate and aggregate impairment across all facilities of a customer
/// Automatically fetches data from database - minimal payload required
/// </summary>
internal sealed class CalculateCustomerImpairmentCommandHandler(
    ILoanDetailsRepository loanRepository,
    ICashFlowOrchestrationService orchestrationService,
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
            logger.LogInformation(
                "Starting customer impairment calculation for customer {CustomerNumber}",
                command.CustomerNumber);

            // Step 1: Get all facilities for the customer from database
            List<CustomerFacilityDetail> allFacilities = await loanRepository
                .GetCustomerFacilitiesAsync(command.CustomerNumber, cancellationToken);

            if (!allFacilities.Any())
            {
                logger.LogWarning(
                    "No facilities found for customer {CustomerNumber}",
                    command.CustomerNumber);
                return Result.Failure<CustomerImpairmentResponse>(
                    IndividualImpairmentErrors.FacilityNotFound(command.CustomerNumber));
            }

            // Step 2: Filter facilities if specific ones are requested
            List<CustomerFacilityDetail> facilitiesToProcess = FilterFacilities(
                allFacilities,
                command.Facilities);

            if (!facilitiesToProcess.Any())
            {
                logger.LogWarning(
                    "No matching facilities found for customer {CustomerNumber}",
                    command.CustomerNumber);
                return Result.Failure<CustomerImpairmentResponse>(
                    Error.NotFound(
                        "Facilities.NotFound",
                        "No matching facilities found for the specified facility numbers"));
            }

            // Step 3: Build override lookup for quick access
            Dictionary<string, FacilityOverrides?> overrideLookup = BuildOverrideLookup(command.Facilities);

            // Step 4: Process each facility
            var facilityDetails = new List<FacilityImpairmentDetail>();
            decimal customerAmortizedCost = 0;
            decimal customerSumOfPV = 0;

            foreach (CustomerFacilityDetail facility in facilitiesToProcess)
            {
                Result<FacilityImpairmentDetail> facilityResult = await ProcessFacilityAsync(
                    facility,
                    overrideLookup.GetValueOrDefault(facility.FacilityNumber),
                    command.CustomerNumber,
                    command.SaveToDatabase,
                    cancellationToken);

                if (facilityResult.IsFailure)
                {
                    logger.LogWarning(
                        "Failed to process facility {FacilityNumber}: {Error}",
                        facility.FacilityNumber, facilityResult.Error.Description);
                    continue; // Continue with other facilities
                }

                FacilityImpairmentDetail detail = facilityResult.Value;
                facilityDetails.Add(detail);

                customerAmortizedCost += detail.AmortizedCost;
                customerSumOfPV += detail.SumOfPVOfCashFlows;
            }

            if (!facilityDetails.Any())
            {
                return Result.Failure<CustomerImpairmentResponse>(
                    Error.Failure(
                        "CustomerImpairment.NoFacilitiesProcessed",
                        "Failed to process any facilities for the customer"));
            }

            // Step 5: Calculate customer-level aggregates
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
                "Completed customer impairment calculation for {CustomerNumber}: " +
                "{FacilityCount} facilities, Total Impairment={Impairment}",
                command.CustomerNumber, facilityDetails.Count, customerImpairmentAmount);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error calculating customer impairment for {CustomerNumber}",
                command.CustomerNumber);
            return Result.Failure<CustomerImpairmentResponse>(
                Error.Failure(
                    "CustomerImpairment.CalculationError",
                    $"Error calculating customer impairment: {ex.Message}"));
        }
    }

    /// <summary>
    /// Filters facilities based on the provided facility list
    /// If no facilities specified, returns all facilities
    /// </summary>
    private static List<CustomerFacilityDetail> FilterFacilities(
        List<CustomerFacilityDetail> allFacilities,
        List<FacilityOverrideInput>? requestedFacilities)
    {
        if (requestedFacilities == null || !requestedFacilities.Any())
        {
            return allFacilities;
        }

        var requestedNumbers = requestedFacilities
            .Select(f => f.FacilityNumber)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allFacilities
            .Where(f => requestedNumbers.Contains(f.FacilityNumber))
            .ToList();
    }

    /// <summary>
    /// Builds a lookup dictionary for facility overrides
    /// </summary>
    private static Dictionary<string, FacilityOverrides?> BuildOverrideLookup(
        List<FacilityOverrideInput>? facilities)
    {
        if (facilities == null || !facilities.Any())
        {
            return new Dictionary<string, FacilityOverrides?>(StringComparer.OrdinalIgnoreCase);
        }

        return facilities.ToDictionary(
            f => f.FacilityNumber,
            f => f.Overrides,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Processes a single facility and calculates its impairment
    /// </summary>
    private async Task<Result<FacilityImpairmentDetail>> ProcessFacilityAsync(
        CustomerFacilityDetail facility,
        FacilityOverrides? overrides,
        string customerNumber,
        bool saveToDatabase,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Processing facility {FacilityNumber} for customer {CustomerNumber}",
            facility.FacilityNumber, customerNumber);

        // Get loan details (or use overrides)
        decimal amortizedCost = overrides?.AmortizedCost ?? facility.TotalOutstanding;
        decimal interestRate = overrides?.InterestRate ?? facility.InterestRate / 100; // Convert from percentage

        // Build scenario cash flows from saved configurations
        Result<List<ScenarioCashFlowInput>> scenariosResult = await orchestrationService
            .BuildScenarioCashFlowsAsync(
                facility.FacilityNumber,
                interestRate,
                cancellationToken);

        if (scenariosResult.IsFailure)
        {
            logger.LogWarning(
                "Failed to build scenarios for facility {FacilityNumber}: {Error}",
                facility.FacilityNumber, scenariosResult.Error.Description);
            return Result.Failure<FacilityImpairmentDetail>(scenariosResult.Error);
        }

        List<ScenarioCashFlowInput> scenarios = scenariosResult.Value;

        // Filter to specific scenario if override provided
        if (overrides?.ScenarioId.HasValue == true)
        {
            scenarios = scenarios
                .Where(s => s.ScenarioId == overrides.ScenarioId.Value)
                .ToList();

            if (!scenarios.Any())
            {
                return Result.Failure<FacilityImpairmentDetail>(
                    Error.NotFound(
                        "Scenario.NotFound",
                        $"Scenario {overrides.ScenarioId} not found for facility {facility.FacilityNumber}"));
            }

            // Adjust probability to 100% for single scenario
            scenarios = scenarios
                .Select(s => s with { Probability = 1.0m })
                .ToList();
        }

        if (!scenarios.Any())
        {
            return Result.Failure<FacilityImpairmentDetail>(
                Error.NotFound(
                    "Scenarios.NotConfigured",
                    $"No cash flow configurations found for facility {facility.FacilityNumber}"));
        }

        // Calculate scenario results
        var scenarioResults = new List<ScenarioResult>();
        foreach (ScenarioCashFlowInput scenario in scenarios)
        {
            ScenarioResult result = discountingService.CalculateScenarioResult(
                scenario,
                interestRate);
            scenarioResults.Add(result);
        }

        // Calculate weighted sum of PV
        decimal sumOfPV = discountingService.CalculateWeightedSumOfPV(scenarioResults);
        decimal impairmentAmount = amortizedCost - sumOfPV;
        decimal impairmentPercentage = amortizedCost > 0
            ? impairmentAmount / amortizedCost * 100
            : 0;

        // Save to database if requested
        Guid calculationId = Guid.Empty;
        if (saveToDatabase)
        {
            calculationId = await SaveFacilityCalculationAsync(
                facility.FacilityNumber,
                customerNumber,
                interestRate,
                amortizedCost,
                sumOfPV,
                impairmentAmount,
                scenarioResults,
                cancellationToken);
        }

        logger.LogInformation(
            "Calculated impairment for facility {FacilityNumber}: " +
            "AmortizedCost={AmortizedCost}, SumOfPV={SumOfPV}, Impairment={Impairment}",
            facility.FacilityNumber, amortizedCost, sumOfPV, impairmentAmount);

        return Result.Success(new FacilityImpairmentDetail
        {
            CalculationId = calculationId,
            FacilityNumber = facility.FacilityNumber,
            InterestRate = interestRate,
            AmortizedCost = Math.Round(amortizedCost, 2),
            SumOfPVOfCashFlows = Math.Round(sumOfPV, 2),
            ImpairmentAmount = Math.Round(impairmentAmount, 2),
            ImpairmentPercentage = Math.Round(impairmentPercentage, 2)
        });
    }

    /// <summary>
    /// Saves facility calculation to database
    /// </summary>
    private async Task<Guid> SaveFacilityCalculationAsync(
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
}
