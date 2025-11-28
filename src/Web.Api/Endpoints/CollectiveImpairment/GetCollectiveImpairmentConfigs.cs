using Application.Abstractions.Messaging;
using Application.CollectiveImpairment.GetConfigurations;
using Domain.CollectiveImpairment;
using Microsoft.Extensions.Logging;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.CollectiveImpairment;

internal sealed class GetCollectiveImpairmentConfigs : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("collective-impairment", async (
            ParameterType? parameter,
            IQueryHandler<GetCollectiveImpairmentConfigsQuery, IReadOnlyList<CollectiveImpairmentConfigResponse>> handler,
            ILogger<GetCollectiveImpairmentConfigs> logger,
            CancellationToken cancellationToken) =>
        {
            GetCollectiveImpairmentConfigsQuery query = new(parameter);

            Result<IReadOnlyList<CollectiveImpairmentConfigResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.CollectiveImpairment);
    }
}
