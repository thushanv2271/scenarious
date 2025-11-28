// src/Application/EclAnalysis/CalculateThresholdSummary/CalculateEclThresholdSummaryCommandHandler.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions.Caching;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.EclAnalysis;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SharedKernel;

namespace Application.EclAnalysis.CalculateThresholdSummary;

/// <summary>
/// Handles calculation of ECL threshold summary by categorizing loans into individual and collective groups
/// </summary>
internal sealed class CalculateEclThresholdSummaryCommandHandler(
    IApplicationDbContext context,
    IEclThresholdSummaryCache cacheService,
    ILogger<CalculateEclThresholdSummaryCommandHandler> logger)
    : ICommandHandler<CalculateEclThresholdSummaryCommand, EclThresholdSummaryResponse>
{
    public async Task<Result<EclThresholdSummaryResponse>> Handle(
        CalculateEclThresholdSummaryCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // Retrieve user with their branch details
            User? user = await context.Users
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

            if (user is null)
            {
                return Result.Failure<EclThresholdSummaryResponse>(UserErrors.NotFound(command.UserId));
            }

            // Ensure user is assigned to a branch
            if (user.BranchId is null || user.Branch is null)
            {
                return Result.Failure<EclThresholdSummaryResponse>(
                    EclAnalysisErrors.UserNotAssignedToBranch);
            }

            string branchName = user.Branch.BranchName;
            string branchCode = user.Branch.BranchCode;

            // Generate cache key based on threshold type and parameters
            string cacheKey = ThresholdCacheKeyGenerator.Generate(command, branchCode);

            if (cacheService.TryGetValue(cacheKey, out EclThresholdSummaryResponse? cachedResponse) && cachedResponse != null)
            {
                logger.LogInformation(
                    "Retrieved threshold summary from cache for user {UserId}, branch {BranchName}, key: {CacheKey}",
                    command.UserId,
                    branchName,
                    cacheKey);
                return Result.Success(cachedResponse);
            }

            // Get database connection string
            var dbContext = context as DbContext;
            string? connectionString = dbContext?.Database.GetConnectionString();

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database connection string not found");
            }

            // Query loan data
            List<LoanDetailRawDto> customerExposures = await LoanDataRetriever.RetrieveAsync(
                connectionString,
                branchName,
                cancellationToken);

            // Check if any data was found
            if (!customerExposures.Any())
            {
                logger.LogWarning(
                    "No loan data found for user {UserId}, branch {BranchName} ({BranchCode})",
                    command.UserId,
                    branchName,
                    branchCode);
                return Result.Failure<EclThresholdSummaryResponse>(EclAnalysisErrors.NoDataFound);
            }

            // Calculate threshold based on type
            Result<ThresholdCalculationResult> thresholdResult = ThresholdCalculator.Calculate(
                command.ThresholdType,
                customerExposures,
                command.IndividualSignificantThreshold,
                command.TopNCount,
                command.CumulativePercentageThreshold);

            if (thresholdResult.IsFailure)
            {
                return Result.Failure<EclThresholdSummaryResponse>(thresholdResult.Error);
            }

            ThresholdCalculationResult calculationResult = thresholdResult.Value;

            // Build response with all calculated values
            var response = new EclThresholdSummaryResponse
            {
                BranchCode = branchCode,
                BranchName = branchName,
                ThresholdType = command.ThresholdType.ToString(),
                ThresholdValue = calculationResult.ThresholdDescription,
                Individual = new ImpairmentCategoryResponse
                {
                    CustomerCount = calculationResult.IndividualCustomerCount,
                    AmortizedCost = calculationResult.IndividualCost
                },
                Collective = new ImpairmentCategoryResponse
                {
                    CustomerCount = calculationResult.CollectiveCustomerCount,
                    AmortizedCost = calculationResult.CollectiveCost
                },
                GrandTotal = new ImpairmentCategoryResponse
                {
                    CustomerCount = calculationResult.IndividualCustomerCount + calculationResult.CollectiveCustomerCount,
                    AmortizedCost = calculationResult.IndividualCost + calculationResult.CollectiveCost
                }
            };

            // Store result in cache for 1 hour
            cacheService.Set(cacheKey, response, TimeSpan.FromHours(1));

            logger.LogInformation(
                "Calculated and cached threshold summary for user {UserId}, branch {BranchName} ({BranchCode}) using {ThresholdType}. Found {TotalCustomers} customers.",
                command.UserId,
                branchName,
                branchCode,
                command.ThresholdType,
                response.GrandTotal.CustomerCount);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating ECL threshold summary for user {UserId}", command.UserId);
            return Result.Failure<EclThresholdSummaryResponse>(
                EclAnalysisErrors.ProcessingError(ex.Message));
        }
    }
}
