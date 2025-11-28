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
            int page,
            int pageSize,
            IQueryHandler<GetAgeBucketConfigurationsQuery, PaginatedResult<AgeBucketConfigurationDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAgeBucketConfigurationsQuery(page, pageSize);
            Result<PaginatedResult<AgeBucketConfigurationDto>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("GetAgeBucketConfigurations")
        .WithSummary("Get age bucket configurations with pagination")
        .WithDescription("Returns a paginated list of age bucket configurations")
        .WithTags("Configurations")
        .Produces<PaginatedResult<AgeBucketConfigurationDto>>(200)
        .ProducesProblem(400)
        .ProducesProblem(401);
    }
}