using Application.Abstractions.Messaging;
using Application.Users.AssignRole;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class AssignRole : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/{userId}/roles", async (
            Guid userId,
            AssignRoleRequest request,
            ICommandHandler<AssignRoleToUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            AssignRoleToUserCommand command = new(userId, request.RoleIds);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok("Role assigned successfully"),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(
        [
            PermissionRegistry.AdminUserManagementRead,
            PermissionRegistry.AdminSettingsRolePermissionEdit
        ])
        .WithTags(Tags.Users);
    }
}

public sealed record AssignRoleRequest(List<Guid> RoleIds);
