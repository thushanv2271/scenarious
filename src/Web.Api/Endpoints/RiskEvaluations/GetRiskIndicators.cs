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

            // Parse category string to enum if provided
            if (!string.IsNullOrWhiteSpace(category))
            {
                if (Enum.TryParse<RiskIndicatorCategory>(category, true, out RiskIndicatorCategory parsed))
                {
                    categoryEnum = parsed;
                }
                else
                {
                    // Return bad request if invalid category provided
                    return Results.BadRequest(new
                    {
                        error = $"Invalid category '{category}'. Valid values are: SICR, OEIL"
                    });
                }
            }

            var query = new GetRiskIndicatorsQuery(categoryEnum);
            Result<List<RiskIndicatorResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Risk Evaluations")
        .Produces<List<RiskIndicatorResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
