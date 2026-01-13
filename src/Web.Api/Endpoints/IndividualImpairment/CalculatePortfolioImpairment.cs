using Application.Abstractions.Messaging;
using Application.IndividualImpairment.CalculatePortfolioImpairment;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.IndividualImpairment;

/// <summary>
/// Endpoint for calculating individual impairment across multiple customers (portfolio level)
/// </summary>
internal sealed class CalculatePortfolioImpairment : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("individual-impairment/calculate-portfolio", async (
            CalculatePortfolioImpairmentCommand command,
            ICommandHandler<CalculatePortfolioImpairmentCommand, PortfolioImpairmentResponse> handler,
            CancellationToken cancellationToken) =>
        {
            Result<PortfolioImpairmentResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.EclAnalysisAccess)
        .WithTags(Tags.IndividualImpairment)
        .WithName("CalculatePortfolioImpairment")
        .WithDescription("Calculate individual impairment for multiple customers with portfolio aggregation")
        .Produces<PortfolioImpairmentResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
