using Application.Abstractions.Messaging;
using Application.Configurations.AgeBucket.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Configurations;

internal sealed class DeleteAgeBucketConfiguration : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/configurations/age-bucket/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteAgeBucketConfigurationCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteAgeBucketConfigurationCommand(id);
            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                success => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("DeleteAgeBucketConfiguration")
        .WithSummary("Delete an age bucket configuration")
        .WithDescription("Deletes an existing age bucket configuration")
        .WithTags("Configurations")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(404);
    }
}