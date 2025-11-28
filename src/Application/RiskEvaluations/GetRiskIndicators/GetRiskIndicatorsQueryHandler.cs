using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.RiskEvaluations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.RiskEvaluations.GetRiskIndicators;

internal sealed class GetRiskIndicatorsQueryHandler(
    IApplicationDbContext context,
    ILogger<GetRiskIndicatorsQueryHandler> logger)
    : IQueryHandler<GetRiskIndicatorsQuery, List<RiskIndicatorResponse>>
{
    public async Task<Result<List<RiskIndicatorResponse>>> Handle(
        GetRiskIndicatorsQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting GetRiskIndicators with category: {Category}", query.Category);

            // Base query: only active indicators
            IQueryable<RiskIndicator> queryable = context.RiskIndicators
                .Where(r => r.IsActive);

            logger.LogInformation("Created base query");

            // Filter by category if provided
            if (query.Category.HasValue)
            {
                logger.LogInformation("Filtering by category: {Category}", query.Category.Value);
                queryable = queryable.Where(r => r.Category == query.Category.Value);
            }

            // Log the SQL query being executed
            string sql = queryable.ToQueryString();
            logger.LogInformation("Executing SQL: {Sql}", sql);

            // Execute the query
            List<RiskIndicator> indicators = await queryable
                .OrderBy(r => r.Category)
                .ThenBy(r => r.DisplayOrder)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Retrieved {Count} indicators from database", indicators.Count);

            // Map to response
            var response = indicators
                .Select(r => new RiskIndicatorResponse
                {
                    IndicatorId = r.IndicatorId,
                    Category = r.Category.ToString(),
                    Description = r.Description,
                    PossibleValues = r.PossibleValues
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList(),
                    DisplayOrder = r.DisplayOrder
                })
                .ToList();

            logger.LogInformation("Mapped to {Count} response objects", response.Count);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetRiskIndicators. Category: {Category}, Message: {Message}, StackTrace: {StackTrace}",
                query.Category, ex.Message, ex.StackTrace);

            return Result.Failure<List<RiskIndicatorResponse>>(
                new Error(
                    "RiskIndicators.QueryFailed",
                    ex.Message,
                    ErrorType.Failure));
        }
    }
}
