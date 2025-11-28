using Application.Abstractions.Messaging;
using Application.RiskEvaluations.CreateEvaluation;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.RiskEvaluations;

/// <summary>
/// Endpoint for creating a new customer risk evaluation.
/// </summary>
internal sealed class CreateEvaluation : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("risk-evaluations", async (
            CreateEvaluationRequest request,
            ICommandHandler<CreateRiskEvaluationCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            // Convert request into command
            var command = new CreateRiskEvaluationCommand(
                request.CustomerNumber,
                request.EvaluationDate,
                request.IndicatorEvaluations
                    .Select(i => new IndicatorEvaluationItem(
                        i.IndicatorId,
                        i.Value))
                    .ToList()
            );

            // Execute command
            Result<Guid> result = await handler.Handle(command, cancellationToken);

            // Return success or problem response
            return result.Match(
                evaluationId => Results.Ok(new { evaluationId }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Risk Evaluations");
    }
}
