using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Roles;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.RemoveRole;

internal sealed class RemoveRoleFromUserCommandHandler(
    IApplicationDbContext dbContext,
    IPermissionCacheService permissionCache)
    : ICommandHandler<RemoveRoleFromUserCommand>
{
    public async Task<Result> Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        // Verify user exists
        bool userExists = await dbContext.Users
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure(UserErrors.NotFound(request.UserId));
        }

        // Verify role exists
        bool roleExists = await dbContext.Roles
            .AnyAsync(r => r.Id == request.RoleId, cancellationToken);

        if (!roleExists)
        {
            return Result.Failure(RoleErrors.NotFound(request.RoleId));
        }

        // Find and remove the user role assignment
        Domain.UserRoles.UserRole? userRole = await dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId, cancellationToken);

        if (userRole == null)
        {
            return Result.Failure(UserErrors.RoleNotAssigned);
        }

        dbContext.UserRoles.Remove(userRole);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate permission cache for the user
        permissionCache.InvalidateUserPermissions(request.UserId);

        return Result.Success();
    }
}
