using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Roles;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.GetById;

internal sealed class GetRoleByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetRoleByIdQuery, RoleResponse>
{
    public async Task<Result<RoleResponse>> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Get role with permissions
        var roleWithPermissions = await dbContext.Roles
            .Where(r => r.Id == request.RoleId)
            .Select(r => new
            {
                Role = r,
                Permissions = dbContext.RolePermissions
                    .Where(rp => rp.RoleId == r.Id)
                    .Join(dbContext.Permissions,
                        rp => rp.PermissionId,
                        p => p.Id,
                        (rp, p) => p.Key)
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (roleWithPermissions == null)
        {
            return Result.Failure<RoleResponse>(RoleErrors.NotFound(request.RoleId));
        }

        RoleResponse response = new(
            roleWithPermissions.Role.Id,
            roleWithPermissions.Role.Name,
            roleWithPermissions.Role.Description,
            roleWithPermissions.Role.IsSystemRole,
            roleWithPermissions.Role.CreatedAt,
            roleWithPermissions.Permissions);

        return Result.Success(response);
    }
}
