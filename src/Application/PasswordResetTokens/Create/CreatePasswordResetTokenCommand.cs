using Application.Abstractions.Messaging;

namespace Application.PasswordResetTokens.Create;

public sealed record CreatePasswordResetTokenCommand(string Email) : ICommand<string>;

