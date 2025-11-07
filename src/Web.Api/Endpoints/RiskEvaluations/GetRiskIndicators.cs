using Application.Abstractions.Messaging;
using Application.RiskEvaluations.GetRiskIndicators;
using Domain.RiskEvaluations;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.RiskEvaluations;

internal sealed class GetRiskIndicators : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("risk-indicators", async (
            string? category,
            IQueryHandler<GetRiskIndicatorsQuery, List<RiskIndicatorResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            RiskIndicatorCategory? categoryEnum = null;
            if (!string.IsNullOrEmpty(category) &&
                Enum.TryParse(category, true, out RiskIndicatorCategory parsed))
            {
                categoryEnum = parsed;
            }

            var query = new GetRiskIndicatorsQuery(categoryEnum);
            Result<List<RiskIndicatorResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Risk Evaluations");
    }
}
