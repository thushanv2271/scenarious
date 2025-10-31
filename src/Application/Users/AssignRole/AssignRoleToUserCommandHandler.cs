using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Roles;
using Domain.UserRoles;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.AssignRole;

internal sealed class AssignRoleToUserCommandHandler(
    IApplicationDbContext dbContext,
    IPermissionCacheService permissionCache)
    : ICommandHandler<AssignRoleToUserCommand>
{
    public async Task<Result> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        // Check if the user exists
        bool userExists = await dbContext.Users
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure(UserErrors.NotFound(request.UserId));
        }

        // Get all existing roles from the request
        List<Guid> existingRoles = await dbContext.Roles
            .Where(r => request.RoleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (existingRoles.Count == 0)
        {
            return Result.Failure(RoleErrors.NotFoundMultiple(request.RoleIds));
        }

        // Get roles already assigned to the user
        List<Guid> assignedRoles = await dbContext.UserRoles
            .Where(ur => ur.UserId == request.UserId && request.RoleIds.Contains(ur.RoleId))
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        // Determine which roles are new (not yet assigned)
        var newRoleIds = existingRoles.Except(assignedRoles).ToList();

        if (newRoleIds.Count == 0)
        {
            return Result.Failure(UserErrors.RoleAlreadyAssigned);
        }

        // Assign new roles
        IEnumerable<UserRole> userRoles = newRoleIds.Select(roleId => new UserRole(request.UserId, roleId));
        dbContext.UserRoles.AddRange(userRoles);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate permission cache
        permissionCache.InvalidateUserPermissions(request.UserId);

        return Result.Success();
    }
}
