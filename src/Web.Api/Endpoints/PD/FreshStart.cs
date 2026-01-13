using Application.Abstractions.Messaging;
using Application.PD.FreshStart;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PD;

/// <summary>
/// Endpoint for performing a fresh start operation that truncates specific database tables and clears file storage
/// </summary>
internal sealed class FreshStart : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("pd/fresh-start", async (
            ICommandHandler<FreshStartCommand> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            // Use a default user for the fresh start operation
            var command = new FreshStartCommand("System");

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { Message = "Fresh start operation completed successfully" }),
                CustomResults.Problem
            );
        })
        .WithTags("PD Fresh Start")
        .WithDescription("Performs a fresh start by truncating specific database tables (uploaded_files, file_validation_results, collective_impairment_configs, pd_algorithm_results, pd_progress_tracking) and clearing file storage")
        .WithSummary("Fresh Start Operation");
    }
}