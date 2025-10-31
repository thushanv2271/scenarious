using Application.Abstractions.Messaging;
using System;

namespace Application.Users.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
) : ICommand<string>;
