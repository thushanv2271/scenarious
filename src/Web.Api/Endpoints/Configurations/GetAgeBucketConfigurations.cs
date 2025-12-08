using Application.Abstractions.Messaging;
using Application.Configurations.AgeBucket.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Configurations;

internal sealed class GetAgeBucketConfigurations : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/configurations/age-bucket", async (
            IQueryHandler<GetAgeBucketConfigurationQuery, AgeBucketConfigurationDto> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAgeBucketConfigurationQuery();
            Result<AgeBucketConfigurationDto> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("GetAgeBucketConfiguration")
        .WithSummary("Get age bucket configuration")
        .WithDescription("Returns the single age bucket configuration")
        .WithTags("Configurations")
        .Produces<AgeBucketConfigurationDto>(200)
        .ProducesProblem(400)
        .ProducesProblem(401);
    }
}
