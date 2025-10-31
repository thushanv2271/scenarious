using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetUserEffectivePermissions;

internal sealed class GetUserEffectivePermissionsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetUserEffectivePermissionsQuery, HashSet<string>>
{
    public async Task<Result<HashSet<string>>> Handle(
        GetUserEffectivePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        // Get all permissions for the user through their roles
        HashSet<string> rolePermissions = await dbContext.UserRoles
            .Where(ur => ur.UserId == request.UserId)
            .Join(dbContext.RolePermissions,
                ur => ur.RoleId,
                rp => rp.RoleId,
                (ur, rp) => rp.PermissionId)
            .Join(dbContext.Permissions,
                permissionId => permissionId,
                p => p.Id,
                (permissionId, permission) => permission.Key)
            .ToHashSetAsync(cancellationToken);
        HashSet<string> allPermissions = new(rolePermissions);


        return Result.Success(allPermissions);
    }
}
