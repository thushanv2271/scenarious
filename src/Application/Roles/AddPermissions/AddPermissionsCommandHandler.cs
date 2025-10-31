using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Permissions;
using Domain.RolePermissions;
using Domain.Roles;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.AddPermissions;

internal sealed class AddPermissionsCommandHandler(IApplicationDbContext dbContext)
	: ICommandHandler<AddPermissionsCommand, Guid>
{
	public async Task<Result<Guid>> Handle(AddPermissionsCommand request, CancellationToken cancellationToken)
	{
		Role? role = await dbContext.Roles
			.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

		if (role is null)
		{
			return Result.Failure<Guid>(RoleErrors.NotFound(request.RoleId));
		}

		var validPermissionKeys = PermissionRegistry.GetAllPermissions()
			.Select(p => p.Key)
			.ToHashSet();

		var invalidKeys = request.PermissionKeys
			.Where(key => !validPermissionKeys.Contains(key))
			.ToList();

		if (invalidKeys.Any())
		{
			return Result.Failure<Guid>(PermissionErrors.InvalidPermissionKeys(invalidKeys));
		}

		List<Guid> existingPermissionIds = await dbContext.RolePermissions
			.Where(rp => rp.RoleId == request.RoleId)
			.Select(rp => rp.PermissionId)
			.ToListAsync(cancellationToken);

		List<Permission> newPermissions = await dbContext.Permissions
			.Where(p => request.PermissionKeys.Contains(p.Key))
			.Where(p => !existingPermissionIds.Contains(p.Id))
			.ToListAsync(cancellationToken);

		var rolePermissions = newPermissions
			.Select(p => new RolePermission(request.RoleId, p.Id))
			.ToList();

		dbContext.RolePermissions.AddRange(rolePermissions);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Result.Success(request.RoleId);
	}
}
