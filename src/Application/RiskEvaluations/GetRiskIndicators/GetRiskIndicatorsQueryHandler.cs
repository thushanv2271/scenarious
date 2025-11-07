using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.RiskEvaluations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.RiskEvaluations.GetRiskIndicators;

// Handles query for fetching active risk indicators
internal sealed class GetRiskIndicatorsQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetRiskIndicatorsQuery, List<RiskIndicatorResponse>>
{
    public async Task<Result<List<RiskIndicatorResponse>>> Handle(
        GetRiskIndicatorsQuery query,
        CancellationToken cancellationToken)
    {
        // Base query: only active indicators
        IQueryable<RiskIndicator> queryable = context.RiskIndicators
            .Where(r => r.IsActive);

        // Filter by category if provided
        if (query.Category.HasValue)
        {
            queryable = queryable.Where(r => r.Category == query.Category.Value);
        }

        // Build response list
        List<RiskIndicatorResponse> indicators = await queryable
            .OrderBy(r => r.Category)               // Sort by category
            .ThenBy(r => r.DisplayOrder)            // Then by display order
            .Select(r => new RiskIndicatorResponse
            {
                IndicatorId = r.IndicatorId,
                Category = r.Category.ToString(),
                Description = r.Description,
                PossibleValues = r.PossibleValues
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList(),                      // Convert CSV string to list
                DisplayOrder = r.DisplayOrder
            })
            .ToListAsync(cancellationToken);

        // Return as success result
        return Result.Success(indicators);
    }
}
