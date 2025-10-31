using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Permissions;
using Domain.RolePermissions;
using Domain.Roles;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Roles.RemovePermissions;

internal sealed class RemovePermissionsCommandHandler(IApplicationDbContext dbContext)
	: ICommandHandler<RemovePermissionsCommand>
{
	public async Task<Result> Handle(RemovePermissionsCommand request, CancellationToken cancellationToken)
	{
		Role? role = await dbContext.Roles
			.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

		if (role is null)
		{
			return Result.Failure(RoleErrors.NotFound(request.RoleId));
		}

		var validPermissionKeys = PermissionRegistry.GetAllPermissions()
			.Select(p => p.Key)
			.ToHashSet();

		var invalidKeys = request.PermissionKeys
			.Where(k => !validPermissionKeys.Contains(k))
			.ToList();

		if (invalidKeys.Any())
		{
			return Result.Failure(PermissionErrors.InvalidPermissionKeys(invalidKeys));
		}

		List<RolePermission> permissionsToRemove = await dbContext.RolePermissions
			.Where(rp => rp.RoleId == request.RoleId &&
						 request.PermissionKeys.Contains(rp.Permission.Key))
			.Include(rp => rp.Permission)
			.ToListAsync(cancellationToken);

		if (!permissionsToRemove.Any())
		{
			return Result.Success();
		}

		dbContext.RolePermissions.RemoveRange(permissionsToRemove);
		await dbContext.SaveChangesAsync(cancellationToken);

		return Result.Success();
	}
}
