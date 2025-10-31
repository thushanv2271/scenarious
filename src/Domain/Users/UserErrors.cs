using SharedKernel;

namespace Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"The user with the Id = '{userId}' was not found");

    public static Error Unauthorized() => Error.Failure(
        "Users.Unauthorized",
        "You are not authorized to perform this action.");

    public static readonly Error NotFoundByEmail = Error.NotFound(
        "Users.NotFoundByEmail",
        "Submitted e-mail address doesn't exist in the system");

    public static readonly Error EmailNotUnique = Error.Conflict(
        "Users.EmailNotUnique",
        "The provided email is not unique");

    public static readonly Error InvalidOrExpiredResetToken = Error.Conflict(
        "Users.InvalidOrExpiredResetToken",
        "The provided token is invalid");

    public static readonly Error PasswordsDoNotMatch = Error.Conflict(
        "Users.PasswordsDoNotMatch",
        "Password and Confirm Password do not match");

    public static readonly Error RoleAlreadyAssigned = Error.Conflict(
        "Users.RoleAlreadyAssigned",
        "The user is already assigned to this role");

    public static readonly Error RoleNotAssigned = Error.NotFound(
        "Users.RoleNotAssigned",
        "The user is not assigned to this role");

    public static readonly Error InvalidCurrentPassword = Error.NotFound(
    "Users.InvalidCurrentPassword",
    "The current password is invalid. Please try again.");

    public static readonly Error InvalidPassword = Error.NotFound(
    "Users.InvalidPassword",
    "Thepassword is invalid. Please try again.");
}
