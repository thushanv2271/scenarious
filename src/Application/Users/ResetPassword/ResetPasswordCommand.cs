using Application.Abstractions.Messaging;

namespace Application.Users.ResetPassword;

public sealed record ResetPasswordCommand(
	string Token,
	string Password,
	string ConfirmPassword
) : ICommand<string>;
