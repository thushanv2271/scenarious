using Application.Abstractions.Messaging;
using Application.Configurations.AgeBucket.Get;
using Application.Configurations.AgeBucket.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Configurations;

internal sealed class GetAgeBucketConfigurationById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/configurations/age-bucket/{id:guid}", async (
            Guid id,
            IQueryHandler<GetAgeBucketConfigurationByIdQuery, AgeBucketConfigurationDto> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAgeBucketConfigurationByIdQuery(id);
            Result<AgeBucketConfigurationDto> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("GetAgeBucketConfigurationById")
        .WithSummary("Get age bucket configuration by ID")
        .WithDescription("Returns a specific age bucket configuration by its ID")
        .WithTags("Configurations")
        .Produces<AgeBucketConfigurationDto>(200)
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(404);
    }
}