using Application.Abstractions.Messaging;
using Application.Configurations.AgeBucket.Update;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Configurations;

internal sealed class UpdateAgeBucketConfiguration : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/configurations/age-bucket/{id:guid}", async (
            Guid id,
            UpdateAgeBucketConfigurationCommand command,
            ICommandHandler<UpdateAgeBucketConfigurationCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            command.Id = id;
            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                success => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("UpdateAgeBucketConfiguration")
        .WithSummary("Update an existing age bucket configuration")
        .WithDescription("Updates an existing age bucket configuration with new data")
        .WithTags("Configurations")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(404);
    }
}