using Application.Abstractions.Messaging;
using Application.RiskEvaluations.CreateEvaluation;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.RiskEvaluations;

// Endpoint for creating a new customer risk evaluation
internal sealed class CreateEvaluation : IEndpoint
{
    // Request model for API input
    public sealed record CreateEvaluationRequest(
        string CustomerNumber,                        // Customer identifier
        DateTime EvaluationDate,                      // Evaluation date
        string OverallStatus,                         // Overall risk status
        List<IndicatorEvaluationRequest> IndicatorEvaluations // Indicator details
    );

    // Request model for each indicator entry
    public sealed record IndicatorEvaluationRequest(
        Guid IndicatorId,     // Master indicator ID
        string Value,         // Yes / No / N/A
        string? Notes = null  // Optional notes
    );

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
                request.OverallStatus,
                request.IndicatorEvaluations
                    .Select(i => new IndicatorEvaluationItem(
                        i.IndicatorId,
                        i.Value,
                        i.Notes))
                    .ToList()
            );

            // Execute command
            Result<Guid> result = await handler.Handle(command, cancellationToken);

            // Return success or problem response
            return result.Match(
                evaluationId => Results.Ok(new { evaluationId }),
                CustomResults.Problem);
        })
        .RequireAuthorization()                         // Requires auth
        .HasPermission(PermissionRegistry.PDSetupAccess) // Requires permission
        .WithTags("Risk Evaluations");                   // OpenAPI grouping
    }
}
