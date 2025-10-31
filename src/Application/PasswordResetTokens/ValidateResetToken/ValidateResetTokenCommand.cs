using Application.Abstractions.Messaging;

namespace Application.PasswordResetTokens.ValidateResetToken;

public sealed record ValidateResetTokenCommand(string Token) : ICommand<string>;
