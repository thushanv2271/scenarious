using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Permissions;
using Domain.RolePermissions;
using Domain.Roles;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.Create;

internal sealed class CreateRoleCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<CreateRoleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // Validate that role name is unique
        bool nameExists = await dbContext.Roles
            .AnyAsync(r => r.Name == request.Name, cancellationToken);

        if (nameExists)
        {
            return Result.Failure<Guid>(RoleErrors.NameAlreadyExists);
        }

        // Validate that all permission keys are valid
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

        // Get permission entities for the provided keys
        List<Permission> permissions = await dbContext.Permissions
            .Where(p => request.PermissionKeys.Contains(p.Key))
            .ToListAsync(cancellationToken);

        // Create the role
        Role role = new(
            Guid.CreateVersion7(),
            request.Name,
            request.Description ?? string.Empty,
            isSystemRole: false);

        dbContext.Roles.Add(role);

        // Create role permissions
        var rolePermissions = permissions
            .Select(permission => new RolePermission(role.Id, permission.Id))
            .ToList();

        dbContext.RolePermissions.AddRange(rolePermissions);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(role.Id);
    }
}
