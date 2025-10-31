using Application.Abstractions.Messaging;
using Application.Roles.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class Create : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("roles", async (
            CreateRoleRequest request,
            ICommandHandler<CreateRoleCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            CreateRoleCommand command = new(
                request.Name,
                request.Description,
                request.PermissionKeys);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                roleId => Results.Ok(new { roleId }),
                CustomResults.Problem);


        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionCreate)
        .WithTags(Tags.Roles);
    }
}

public sealed record CreateRoleRequest(
    string Name,
    string? Description,
    IReadOnlyList<string> PermissionKeys);
