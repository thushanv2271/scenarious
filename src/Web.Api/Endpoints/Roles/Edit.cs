using Application.Abstractions.Messaging;
using Application.Roles.Create;
using Application.Roles.Edit;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class Edit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("roles", async (
            EditRoleRequest request,
            ICommandHandler<EditRoleCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            EditRoleCommand command = new(
                request.RoleId,
                request.Name,
                request.Description,
                request.PermissionKeys);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                roleId => Results.CreatedAtRoute("GetRoleById", new { roleId }, roleId),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionEdit)
        .WithTags(Tags.Roles);
    }
}

public sealed record EditRoleRequest(
    Guid RoleId,
    string Name,
    string? Description,
    IReadOnlyList<string> PermissionKeys);
