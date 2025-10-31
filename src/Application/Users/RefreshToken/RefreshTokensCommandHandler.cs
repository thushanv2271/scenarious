using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Domain.Authentication;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.RefreshTokens;


internal sealed class RefreshTokensCommandHandler(
	IRefreshTokenService refreshTokenService,
	ITokenProvider tokenProvider)
	: ICommandHandler<RefreshTokensCommand, (string AccessToken, string RefreshToken)>
{
	public async Task<Result<(string AccessToken, string RefreshToken)>> Handle(RefreshTokensCommand command, CancellationToken cancellationToken)
	{
		(User user, RefreshToken token)? result = await refreshTokenService.GetUserFromRefreshTokenAsync(command.RefreshToken, cancellationToken);

		if (result is null)
		{
			return Result.Failure<(string, string)>(
				new Error("InvalidToken", "Invalid or expired refresh token.", ErrorType.Failure)
			);
		}

		(User user, RefreshToken oldToken) = result.Value;

		string accessToken = await tokenProvider.CreateAsync(user);
		string newRefreshToken = await refreshTokenService.RotateRefreshTokenAsync(oldToken, cancellationToken);

		return Result.Success((accessToken, newRefreshToken));
	}
}
