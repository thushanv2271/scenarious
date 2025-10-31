using Application.Abstractions.Messaging;

namespace Application.Permissions.GetPermissionTree;

public sealed record GetPermissionTreeQuery(Guid UserId) : IQuery<PermissionTreeResponse>;
