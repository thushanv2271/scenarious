using Application.Abstractions.Messaging;
using Application.Users.GetUserEffectivePermissions;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class GetUserEffectivePermissions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/{userId}/permissions", async (
            Guid userId,
            IQueryHandler<GetUserEffectivePermissionsQuery, HashSet<string>> handler,
            CancellationToken cancellationToken) =>
        {
            GetUserEffectivePermissionsQuery query = new(userId);

            Result<HashSet<string>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminUserManagementRead)
        .WithTags(Tags.Users);
    }
}
