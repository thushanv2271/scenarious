using SharedKernel;

namespace Domain.Permissions;

public static class PermissionErrors
{
    public static Error NotFound(Guid id) => new(
        "Permission.NotFound",
        $"The permission with ID '{id}' was not found.",
        ErrorType.NotFound);

    public static Error NotFoundByKey(string key) => new(
        "Permission.NotFoundByKey",
        $"The permission with key '{key}' was not found.",
        ErrorType.NotFound);

    public static Error InvalidKey(string key) => new(
        "Permission.InvalidKey",
        $"The permission key '{key}' is not valid.",
        ErrorType.Validation);

    public static Error AlreadyExists(string key) => new(
        "Permission.AlreadyExists",
        $"A permission with key '{key}' already exists.",
        ErrorType.Conflict);

    public static Error InvalidPermissionKeys(IEnumerable<string> invalidKeys) => new(
        "Permission.InvalidKeys",
        $"The following permission keys are invalid: {string.Join(", ", invalidKeys)}",
        ErrorType.Validation);
}
