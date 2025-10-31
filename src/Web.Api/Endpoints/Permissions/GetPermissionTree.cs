using Application.Abstractions.Messaging;
using Application.Permissions.GetPermissionTemplate;
using Application.Permissions.GetPermissionTree;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Permissions;

internal sealed class GetPermissionTree : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("permissions/tree/{userId}", async (
            Guid userId,
            IQueryHandler<GetPermissionTreeQuery, PermissionTreeResponse> handler,
            CancellationToken cancellationToken) =>
        {
            GetPermissionTreeQuery query = new(userId);

            Result<PermissionTreeResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionRead)
        .WithTags(Tags.Permissions);


        app.MapGet("permissions/template", async (
            IQueryHandler<GetPermissionTemplateQuery, IReadOnlyList<PermissionDefinition>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<PermissionDefinition>> result = await handler.Handle(new GetPermissionTemplateQuery(), cancellationToken);
            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionRead)
        .WithTags(Tags.Permissions);
    }
}
