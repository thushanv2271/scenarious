using Application.Abstractions.Messaging;
using Application.Stages.GetStageMappingOptions;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Stages;

/// <summary>
/// Endpoint for getting stage mapping options
/// </summary>
internal sealed class GetStageMappingOptions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/stages/mapping-options", async (
            IQueryHandler<GetStageMappingOptionsQuery, GetStageMappingOptionsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetStageMappingOptionsQuery();
            Result<GetStageMappingOptionsResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("GetStageMappingOptions")
        .WithSummary("Get all available stage mapping options")
        .WithDescription("Returns a list of all available stage mapping options with their values and labels")
        .WithTags("Stages")
        .Produces<GetStageMappingOptionsResponse>(200)
        .ProducesProblem(400)
        .ProducesProblem(401);
    }
}