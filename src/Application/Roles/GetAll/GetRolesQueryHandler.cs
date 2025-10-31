using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.GetAll;

internal sealed class GetRolesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetRolesQuery, List<RoleListResponse>>
{
    public async Task<Result<List<RoleListResponse>>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        List<RoleListResponse> roles = await dbContext.Roles
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.IsSystemRole,
                r.CreatedAt,
                PermissionCount = dbContext.RolePermissions.Count(rp => rp.RoleId == r.Id),
                Permissions = dbContext.RolePermissions
                    .Where(rp => rp.RoleId == r.Id)
                    .Select(rp => rp.Permission)
                    .ToList()
            })
            .OrderBy(r => r.Name)
            .Select(r => new RoleListResponse(
                r.Id,
                r.Name,
                r.Description,
                r.IsSystemRole,
                r.CreatedAt,
                r.PermissionCount,
                r.Permissions))
            .ToListAsync(cancellationToken);

        return Result.Success(roles);
    }
}
