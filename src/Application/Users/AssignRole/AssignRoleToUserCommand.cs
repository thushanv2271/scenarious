using Application.Abstractions.Messaging;

namespace Application.Users.AssignRole;

public sealed record AssignRoleToUserCommand(Guid UserId, IReadOnlyList<Guid> RoleIds) : ICommand;
