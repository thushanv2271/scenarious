using Application.Abstractions.Messaging;
using System;

namespace Application.Roles.AddPermissions;

public sealed record AddPermissionsCommand(Guid RoleId, IReadOnlyList<string> PermissionKeys)
	: ICommand<Guid>;
