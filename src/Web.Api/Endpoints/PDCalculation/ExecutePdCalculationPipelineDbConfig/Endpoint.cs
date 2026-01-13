using Application.Abstractions.Pipeline;
using Application.PD.Services;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PdCalculation.ExecutePdCalculationPipelineDbConfig;

/// <summary>
/// Executes the PD calculation pipeline using configuration stored in the database
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("pd-calculations/pipeline-db-config", async (
            Request request,
            IPDPipelineService pdPipelineService,
            IPDProgressPublisher progressPublisher,
            ILogger<Endpoint> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("PD Calculation pipeline execution with DB config triggered for SessionId: {SessionId}", request.SessionId);

            try
            {
                // Get user from context
                string createdBy = context.User?.Identity?.Name ?? "system";

                Result result = await pdPipelineService.RunPipelineFromDbAsync(createdBy, request.SessionId, cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogError("PD Calculation Pipeline with DB config failed for SessionId: {SessionId}. Error: {Error}",
                        request.SessionId, result.Error);

                    // Mark any in-progress tasks as failed
                    await progressPublisher.MarkInProgressAsFailedAsync(request.SessionId, result.Error.Description, cancellationToken);

                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "PD Calculation Pipeline Execution Failed",
                        Detail = result.Error.Description,
                        Status = 400,
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    });
                }

                logger.LogInformation("PD Calculation Pipeline with DB config completed successfully for SessionId: {SessionId}", request.SessionId);

                return Results.Ok(new Response
                {
                    Success = true,
                    Message = "PD Calculation pipeline executed successfully"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while executing PD Calculation pipeline with DB config for SessionId: {SessionId}", request.SessionId);

                // Mark any in-progress tasks as failed
                await progressPublisher.MarkInProgressAsFailedAsync(request.SessionId, ex.Message, cancellationToken);

                return Results.Problem(new ProblemDetails
                {
                    Title = "PD Calculation Pipeline Execution Failed",
                    Detail = $"An error occurred while executing the PD Calculation pipeline: {ex.Message}",
                    Status = 500,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                });
            }
        })
        .WithTags("PD Calculation")
        .WithName("ExecutePdCalculationPipelineDbConfig")
        .WithSummary("Execute PD Calculation Pipeline using Database Configuration")
        .WithDescription("Executes the full PD Calculation pipeline using configuration retrieved from the database based on the provided SessionId.")
        .Produces<Response>(200)
        .ProducesProblem(400)
        .ProducesProblem(500);
    }
}
