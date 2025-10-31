using Application.Abstractions.Messaging;

namespace Application.Users.RefreshTokens;


public sealed record RefreshTokensCommand(string RefreshToken)
	: ICommand<(string AccessToken, string RefreshToken)>;
