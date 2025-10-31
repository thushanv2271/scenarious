using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Users.GetUserEffectivePermissions;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Permissions.GetPermissionTree;

internal sealed class GetPermissionTreeQueryHandler(
    IQueryHandler<GetUserEffectivePermissionsQuery, HashSet<string>> permissionsHandler)
    : IQueryHandler<GetPermissionTreeQuery, PermissionTreeResponse>
{
    public async Task<Result<PermissionTreeResponse>> Handle(
        GetPermissionTreeQuery request,
        CancellationToken cancellationToken)
    {
        // Get all permissions from the registry
        var allPermissions = PermissionRegistry.GetAllPermissions().ToList();

        // Get user's effective permissions
        GetUserEffectivePermissionsQuery userPermissionsQuery = new(request.UserId);
        Result<HashSet<string>> userPermissionsResult = await permissionsHandler.Handle(userPermissionsQuery, cancellationToken);

        if (userPermissionsResult.IsFailure)
        {
            return Result.Failure<PermissionTreeResponse>(userPermissionsResult.Error);
        }

        HashSet<string> userPermissions = userPermissionsResult.Value;

        // Build the permission tree with isPresent flags
        var permissionTree = allPermissions
            .Select(permission => new PermissionTreeNode(
                permission.Key,
                permission.DisplayName,
                permission.Category,
                permission.Description,
                userPermissions.Contains(permission.Key)))
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Key)
            .ToList();

        PermissionTreeResponse response = new(permissionTree);
        return Result.Success(response);
    }
}
