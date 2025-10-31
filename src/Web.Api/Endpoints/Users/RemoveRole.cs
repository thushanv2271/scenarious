using Application.Abstractions.Messaging;
using Application.Users.RemoveRole;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class RemoveRole : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("users/{userId}/roles/{roleId}", async (
            Guid userId,
            Guid roleId,
            ICommandHandler<RemoveRoleFromUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            RemoveRoleFromUserCommand command = new(userId, roleId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok("Role un assigned successfully"),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminUserManagementRead)
        .WithTags(Tags.Users);
    }
}
