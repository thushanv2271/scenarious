using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Permissions;
using Domain.RolePermissions;
using Domain.Roles;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.Edit;

internal sealed class EditRoleCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<EditRoleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(EditRoleCommand request, CancellationToken cancellationToken)
    {
        // Check if the role exists
        Role? role = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure<Guid>(RoleErrors.NotFound(request.RoleId));
        }

        // Ensure the name is unique among other roles
        bool nameExists = await dbContext.Roles
            .AnyAsync(r => r.Id != request.RoleId && r.Name == request.Name, cancellationToken);

        if (nameExists)
        {
            return Result.Failure<Guid>(RoleErrors.NameAlreadyExists);
        }

        // Validate permission keys
        var allValidPermissions = PermissionRegistry.GetAllPermissions()
            .Select(p => p.Key)
            .ToList();

        var invalidPermissions = request.PermissionKeys
            .Where(key => !allValidPermissions.Contains(key))
            .ToList();

        if (invalidPermissions.Count != 0)
        {
            return Result.Failure<Guid>(PermissionErrors.InvalidPermissionKeys(invalidPermissions));
        }

        // Update name and description
        role.Update(request.Name, request.Description ?? string.Empty);

        // Get permission entities
        List<Permission> permissions = await dbContext.Permissions
            .Where(p => request.PermissionKeys.Contains(p.Key))
            .ToListAsync(cancellationToken);

        // Remove old role permissions
        List<RolePermission> existingPermissions = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync(cancellationToken);
        dbContext.RolePermissions.RemoveRange(existingPermissions);

        // Add new role permissions
        var newRolePermissions = permissions
            .Select(p => new RolePermission(role.Id, p.Id))
            .ToList();

        dbContext.RolePermissions.AddRange(newRolePermissions);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(role.Id);
    }
}
