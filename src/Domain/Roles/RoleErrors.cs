using SharedKernel;

namespace Domain.Roles;

public static class RoleErrors
{
    public static Error NotFound(Guid id) => new(
        "Role.NotFound",
        $"The role with ID '{id}' was not found.",
        ErrorType.NotFound);

    public static Error NotFoundMultiple(IEnumerable<Guid> roleIds) => new(
        "Role.NotFoundMultiple",
        $"The roles with the following IDs were not found: [{string.Join(", ", roleIds)}]",
        ErrorType.NotFound);

    public static Error NotFoundByName(string name) => new(
        "Role.NotFoundByName",
        $"The role with name '{name}' was not found.",
        ErrorType.NotFound);

    public static Error AlreadyExists(string name) => new(
        "Role.AlreadyExists",
        $"A role with name '{name}' already exists.",
        ErrorType.Conflict);

    public static readonly Error NameAlreadyExists = new(
        "Role.NameAlreadyExists",
        "A role with this name already exists.",
        ErrorType.Conflict);

    public static Error CannotDeleteSystemRole() => new(
        "Role.CannotDeleteSystemRole",
        "System roles cannot be deleted.",
        ErrorType.Validation);

    public static Error CannotDeactivateSystemRole() => new(
        "Role.CannotDeactivateSystemRole",
        "System roles cannot be deactivated.",
        ErrorType.Validation);

    public static Error HasAssignedUsers(int userCount) => new(
        "Role.HasAssignedUsers",
        $"Cannot delete role as it has {userCount} users assigned to it.",
        ErrorType.Conflict);




}
