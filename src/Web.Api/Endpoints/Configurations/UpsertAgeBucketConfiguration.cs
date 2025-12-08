using Application.Abstractions.Messaging;
using Application.Configurations.AgeBucket.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Configurations;

internal sealed class UpsertAgeBucketConfiguration : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/configurations/age-bucket", async (
            UpsertAgeBucketConfigurationCommand command,
            ICommandHandler<UpsertAgeBucketConfigurationCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Ok(new { Id = id, Message = "Configuration upserted successfully" }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("UpsertAgeBucketConfiguration")
        .WithSummary("Upsert an age bucket configuration")
        .WithDescription("Creates a new age bucket configuration or updates the existing one with JSON stored collections")
        .WithTags("Configurations")
        .Produces<object>(200)
        .ProducesProblem(400)
        .ProducesProblem(401);
    }
}
