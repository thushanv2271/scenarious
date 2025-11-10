using System;
using System.Collections.Generic;
using System.Data;
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

            // Check cache before processing
            string cacheKey = $"ECL_Threshold_{branchCode}_{command.IndividualSignificantThreshold}";

            if (cacheService.TryGetValue(cacheKey, out EclThresholdSummaryResponse? cachedResponse) && cachedResponse != null)
            {
                logger.LogInformation(
                    "Retrieved threshold summary from cache for user {UserId}, branch {BranchName}, key: {CacheKey}",
                    command.UserId,
                    branchName,
                    cacheKey);
                return Result.Success(cachedResponse);
            }

            // Validate threshold value
            if (command.IndividualSignificantThreshold <= 0)
            {
                return Result.Failure<EclThresholdSummaryResponse>(EclAnalysisErrors.InvalidThreshold);
            }

            // Get database connection string
            var dbContext = context as DbContext;
            string? connectionString = dbContext?.Database.GetConnectionString();

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database connection string not found");
            }

            // Query loan data using raw SQL for performance
            var customerExposures = new List<LoanDetailRawDto>();

            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                // Get total outstanding balance per customer in the branch
                string sql = @"
                    SELECT 
                        customer_number,
                        SUM(total_os) as total_os
                    FROM loan_details
                    WHERE branch = @branchName
                    GROUP BY customer_number";

                await using var sqlCommand = new NpgsqlCommand(sql, connection);
                sqlCommand.Parameters.AddWithValue("@branchName", branchName);

                await using NpgsqlDataReader reader = await sqlCommand.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    customerExposures.Add(new LoanDetailRawDto
                    {
                        CustomerNumber = reader.GetString(0),
                        TotalOs = reader.GetDecimal(1)
                    });
                }
            }

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

            // Split customers into individual (above threshold) and collective (below threshold) categories
            var individualCustomers = customerExposures
                .Where(c => c.TotalOs >= command.IndividualSignificantThreshold)
                .ToList();

            var collectiveCustomers = customerExposures
                .Where(c => c.TotalOs < command.IndividualSignificantThreshold)
                .ToList();

            // Calculate totals for each category
            int individualCount = individualCustomers.Count;
            decimal individualCost = individualCustomers.Sum(c => c.TotalOs);
            int collectiveCount = collectiveCustomers.Count;
            decimal collectiveCost = collectiveCustomers.Sum(c => c.TotalOs);

            // Build response with all calculated values
            var response = new EclThresholdSummaryResponse
            {
                BranchCode = branchCode,
                BranchName = branchName,
                Individual = new ImpairmentCategoryResponse
                {
                    CustomerCount = individualCount,
                    AmortizedCost = individualCost
                },
                Collective = new ImpairmentCategoryResponse
                {
                    CustomerCount = collectiveCount,
                    AmortizedCost = collectiveCost
                },
                GrandTotal = new ImpairmentCategoryResponse
                {
                    CustomerCount = individualCount + collectiveCount,
                    AmortizedCost = individualCost + collectiveCost
                }
            };

            // Store result in cache for 1 hour
            cacheService.Set(cacheKey, response, TimeSpan.FromHours(1));

            logger.LogInformation(
                "Calculated and cached threshold summary for user {UserId}, branch {BranchName} ({BranchCode}) with threshold {Threshold}. Found {TotalCustomers} customers.",
                command.UserId,
                branchName,
                branchCode,
                command.IndividualSignificantThreshold,
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
