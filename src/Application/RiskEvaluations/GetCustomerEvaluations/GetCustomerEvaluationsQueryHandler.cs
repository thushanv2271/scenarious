using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.RiskEvaluations.GetCustomerEvaluations;

internal sealed class GetCustomerEvaluationsQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetCustomerEvaluationsQuery, List<CustomerEvaluationResponse>>
{
    public async Task<Result<List<CustomerEvaluationResponse>>> Handle(
        GetCustomerEvaluationsQuery query,
        CancellationToken cancellationToken)
    {
        List<CustomerEvaluationResponse> evaluations = await context.CustomerRiskEvaluations
            .Where(e => e.CustomerNumber == query.CustomerNumber)
            .OrderByDescending(e => e.EvaluationDate)
            .Select(e => new CustomerEvaluationResponse
            {
                EvaluationId = e.EvaluationId,
                CustomerNumber = e.CustomerNumber,
                EvaluationDate = e.EvaluationDate,
                OverallStatus = e.OverallStatus,
                EvaluatedBy = e.EvaluatedBy,
                CreatedAt = e.CreatedAt,
                IndicatorEvaluations = e.IndicatorEvaluations
                    .Select(ie => new IndicatorEvaluationDetail
                    {
                        EvalDetailId = ie.EvalDetailId,
                        IndicatorId = ie.IndicatorId,
                        IndicatorDescription = ie.Indicator.Description,
                        Category = ie.Indicator.Category.ToString(),
                        Value = ie.Value,
                        Notes = ie.Notes
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Result.Success(evaluations);
    }
}
