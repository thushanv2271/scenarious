using Application.Abstractions.Messaging;
using Application.Roles.GetAll;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class GetAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("roles", async (
            IQueryHandler<GetRolesQuery, List<RoleListResponse>> handler,
            CancellationToken cancellationToken) => 
        { 
            GetRolesQuery query = new();

            Result<List<RoleListResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionCreate)
        .WithTags(Tags.Roles);
    }
}
