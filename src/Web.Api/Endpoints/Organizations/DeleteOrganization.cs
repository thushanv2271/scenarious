using Application.Abstractions.Messaging;
using Application.Organizations.DeleteOrganization;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

/// <summary>
/// Endpoint for deleting an organization
/// </summary>
internal sealed class Delete : IEndpoint
{   
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Map the DELETE endpoint for deleting an organization by ID
        app.MapDelete("organizations/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteOrganizationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteOrganizationCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("DeleteOrganization")
        .WithSummary("Delete an organization")
        .WithDescription("Deletes an organization if it has no associated branches or users")
        .WithTags("Organizations")
        .WithOpenApi();
    }
}
