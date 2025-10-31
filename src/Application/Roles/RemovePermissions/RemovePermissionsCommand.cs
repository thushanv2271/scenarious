using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Roles.RemovePermissions;

public sealed record RemovePermissionsCommand(Guid RoleId, IReadOnlyList<string> PermissionKeys)
	: ICommand;
