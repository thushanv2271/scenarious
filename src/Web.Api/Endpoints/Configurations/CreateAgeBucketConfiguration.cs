using Application.Abstractions.Messaging;
using Application.Configurations.AgeBucket.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Configurations;

internal sealed class CreateAgeBucketConfiguration : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/configurations/age-bucket", async (
            CreateAgeBucketConfigurationCommand command,
            ICommandHandler<CreateAgeBucketConfigurationCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/configurations/age-bucket/{id}", new { Id = id }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("CreateAgeBucketConfiguration")
        .WithSummary("Create a new age bucket configuration")
        .WithDescription("Creates a new age bucket configuration with JSON stored collections")
        .WithTags("Configurations")
        .Produces<object>(201)
        .ProducesProblem(400)
        .ProducesProblem(401);
    }
}