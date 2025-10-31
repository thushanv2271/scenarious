using Application.Abstractions.Messaging;

namespace Application.Users.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand<string>;
