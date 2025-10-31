using Application.Abstractions.Messaging;

namespace Application.Roles.Edit;

public sealed record EditRoleCommand(
    Guid RoleId,
    string Name,
    string? Description,
    IReadOnlyList<string> PermissionKeys) : ICommand<Guid>;
