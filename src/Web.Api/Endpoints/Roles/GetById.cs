using Application.Abstractions.Messaging;
using Application.Roles.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("roles/{roleId}", async (
            Guid roleId,
            IQueryHandler<GetRoleByIdQuery, RoleResponse> handler,
            CancellationToken cancellationToken) =>
        {
            GetRoleByIdQuery query = new(roleId);

            Result<RoleResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionRead)
        .WithTags(Tags.Roles)
        .WithName("GetRoleById");
    }
}
