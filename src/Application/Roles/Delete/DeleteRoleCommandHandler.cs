using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Roles;
using Domain.RolePermissions;
using Domain.UserRoles;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.Delete;

internal sealed class DeleteRoleCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<DeleteRoleCommand>
{
    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        // Check if role exists
        Role? role = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(request.RoleId));
        }

        // Check if role is assigned to any user
        int assignedUserCount = await dbContext.UserRoles
      .CountAsync(ur => ur.RoleId == request.RoleId, cancellationToken);

        if (assignedUserCount > 0)
        {
            return Result.Failure(RoleErrors.HasAssignedUsers(assignedUserCount));
        }

        // Remove role permissions
        List<RolePermission> rolePermissions = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == request.RoleId)
            .ToListAsync(cancellationToken);

        dbContext.RolePermissions.RemoveRange(rolePermissions);

        // Remove role
        dbContext.Roles.Remove(role);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
