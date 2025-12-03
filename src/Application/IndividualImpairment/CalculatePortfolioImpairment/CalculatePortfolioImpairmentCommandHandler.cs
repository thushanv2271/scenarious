using System.Collections.Concurrent;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.IndividualImpairment.CalculateCustomerImpairment;
using Application.IndividualImpairment.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.IndividualImpairment.CalculatePortfolioImpairment;

/// <summary>
/// Handler to calculate impairment across multiple customers (portfolio level)
/// Processes customers in parallel for performance
/// </summary>
internal sealed class CalculatePortfolioImpairmentCommandHandler(
    ILoanDetailsRepository loanRepository,
    ICommandHandler<CalculateCustomerImpairmentCommand, CustomerImpairmentResponse> customerHandler,
    IDateTimeProvider dateTimeProvider,
    ILogger<CalculatePortfolioImpairmentCommandHandler> logger) 
    : ICommandHandler<CalculatePortfolioImpairmentCommand, PortfolioImpairmentResponse>
{
    private const int MaxParallelDegree = 5; // Limit concurrent processing

    public async Task<Result<PortfolioImpairmentResponse>> Handle(
        CalculatePortfolioImpairmentCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!command.CustomerNumbers.Any())
            {
                return Result.Failure<PortfolioImpairmentResponse>(
                    Error.Validation("Portfolio.NoCustomers", "At least one customer is required"));
            }

            logger.LogInformation(
                "Starting portfolio impairment calculation for {CustomerCount} customers",
                command.CustomerNumbers.Count);

            var customerSummaries = new ConcurrentBag<CustomerImpairmentSummary>();
            var failedCustomers = new ConcurrentBag<string>();
            int totalFacilities = 0;

            // Process customers in parallel with controlled degree of parallelism
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxParallelDegree,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(
                command.CustomerNumbers,
                parallelOptions,
                async (customerNumber, ct) =>
                {
                    try
                    {
                        // Get all facilities for this customer
                        List<CustomerFacilityDetail> facilities = await loanRepository
                            .GetCustomerFacilitiesAsync(customerNumber, ct);

                        if (!facilities.Any())
                        {
                            logger.LogWarning("No facilities found for customer {CustomerNumber}", customerNumber);
                            failedCustomers.Add(customerNumber);
                            return;
                        }

                        // Build facility scenario inputs
                        // Note: In production, you'd fetch actual scenario configurations
                        // For now, we'll create a simplified structure
                        var facilityInputs = facilities.Select(f => new FacilityScenarioInput
                        {
                            FacilityNumber = f.FacilityNumber,
                            AmortizedCost = f.TotalOutstanding,
                            InterestRate = f.InterestRate / 100, // Convert percentage to decimal
                            Scenarios = CreateDefaultScenarios(f) // Helper method to create scenarios
                        }).ToList();

                        // Calculate customer-level impairment
                        var customerCommand = new CalculateCustomerImpairmentCommand(
                            customerNumber,
                            facilityInputs,
                            command.SaveToDatabase);

                        Result<CustomerImpairmentResponse> customerResult =
                            await customerHandler.Handle(customerCommand, ct);

                        if (customerResult.IsSuccess)
                        {
                            CustomerImpairmentResponse response = customerResult.Value;

                            customerSummaries.Add(new CustomerImpairmentSummary
                            {
                                CustomerNumber = response.CustomerNumber,
                                FacilityCount = response.TotalFacilities,
                                CustomerAmortizedCost = response.CustomerAmortizedCost,
                                CustomerSumOfPV = response.CustomerSumOfPV,
                                CustomerImpairmentAmount = response.CustomerImpairmentAmount,
                                CustomerImpairmentPercentage = response.CustomerImpairmentPercentage
                            });

                            Interlocked.Add(ref totalFacilities, response.TotalFacilities);

                            logger.LogInformation(
                                "Successfully calculated impairment for customer {CustomerNumber}",
                                customerNumber);
                        }
                        else
                        {
                            logger.LogError(
                                "Failed to calculate impairment for customer {CustomerNumber}: {Error}",
                                customerNumber, customerResult.Error.Description);
                            failedCustomers.Add(customerNumber);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Error processing customer {CustomerNumber}", customerNumber);
                        failedCustomers.Add(customerNumber);
                    }
                });

            // Aggregate portfolio-level totals
            decimal portfolioAmortizedCost = customerSummaries.Sum(c => c.CustomerAmortizedCost);
            decimal portfolioSumOfPV = customerSummaries.Sum(c => c.CustomerSumOfPV);
            decimal portfolioImpairmentAmount = portfolioAmortizedCost - portfolioSumOfPV;
            decimal portfolioImpairmentPercentage = portfolioAmortizedCost > 0
                ? portfolioImpairmentAmount / portfolioAmortizedCost * 100
                : 0;

            var response = new PortfolioImpairmentResponse
            {
                CalculationDate = dateTimeProvider.UtcNow,
                BranchCode = command.BranchCode,
                TotalCustomers = customerSummaries.Count,
                TotalFacilities = totalFacilities,
                PortfolioAmortizedCost = Math.Round(portfolioAmortizedCost, 2),
                PortfolioSumOfPV = Math.Round(portfolioSumOfPV, 2),
                PortfolioImpairmentAmount = Math.Round(portfolioImpairmentAmount, 2),
                PortfolioImpairmentPercentage = Math.Round(portfolioImpairmentPercentage, 2),
                Customers = customerSummaries.OrderBy(c => c.CustomerNumber).ToList(),
                FailedCustomers = failedCustomers.ToList()
            };

            logger.LogInformation(
                "Completed portfolio impairment calculation: {SuccessCount} successful, {FailedCount} failed",
                customerSummaries.Count, failedCustomers.Count);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating portfolio impairment");
            return Result.Failure<PortfolioImpairmentResponse>(
                Error.Failure("Portfolio.CalculationError", $"Error calculating portfolio impairment: {ex.Message}"));
        }
    }

    /// <summary>
    /// Creates default scenarios for a facility
    /// In production, this would fetch actual configured scenarios
    /// </summary>
    private static List<ScenarioCashFlowInput> CreateDefaultScenarios(CustomerFacilityDetail facility)
    {
        // This is a simplified example - in production you'd fetch actual scenarios
        // from the scenarios table and build cash flows accordingly

        int tenureMonths = CalculateTenureMonths(facility.MaturityDate);
        decimal monthlyPayment = facility.TotalOutstanding / tenureMonths;

        return new List<ScenarioCashFlowInput>
        {
            new()
            {
                ScenarioId = Guid.NewGuid(),
                ScenarioName = "Base Scenario",
                Probability = 1.0m, // 100% for simplified example
                CashFlows = Enumerable.Range(1, tenureMonths)
                    .Select(month => new CashFlowItemInput
                    {
                        Month = month,
                        CashFlowAmount = monthlyPayment
                    })
                    .ToList()
            }
        };
    }

    /// <summary>
    /// Calculates remaining tenure in months
    /// </summary>
    private static int CalculateTenureMonths(DateTime maturityDate)
    {
        int months = (maturityDate.Year - DateTime.UtcNow.Year) * 12 +
                     maturityDate.Month - DateTime.UtcNow.Month;
        return Math.Max(months, 1); // Minimum 1 month
    }
}
