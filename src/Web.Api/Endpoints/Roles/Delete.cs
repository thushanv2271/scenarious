using Application.Abstractions.Messaging;
using Application.Roles.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("roles/{roleId}", async (
            Guid roleId,
            ICommandHandler<DeleteRoleCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteRoleCommand(roleId);
            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok("Role Deleted successfully"),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionDelete)
        .WithTags(Tags.Roles);

    }
}

public sealed record DeleteRoleRequest(Guid RoleId);
