using System.Collections.Concurrent;
using Application.Abstractions.Messaging;
using Application.IndividualImpairment.CalculateCustomerImpairment;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.IndividualImpairment.CalculatePortfolioImpairment;

/// <summary>
/// Handler to calculate impairment across multiple customers (portfolio level)
/// Processes customers in parallel for performance
/// Uses simplified approach - fetches data from database automatically
/// </summary>
internal sealed class CalculatePortfolioImpairmentCommandHandler(
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
                        // Use simplified command - data is fetched automatically from database
                        var customerCommand = new CalculateCustomerImpairmentCommand(
                            customerNumber,
                            null, // Process all facilities
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
}
