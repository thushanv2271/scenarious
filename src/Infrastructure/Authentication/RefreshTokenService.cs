using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Domain.Users;
using Domain.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
namespace Infrastructure.Authentication;

public class RefreshTokenService(
	IApplicationDbContext context) : IRefreshTokenService
{
	public async Task<string> CreateRefreshTokenAsync(User user, CancellationToken cancellationToken)
	{
		string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

		var refreshToken = new RefreshToken
		{
			Token = token,
			UserId = user.Id,
			CreatedAt = DateTime.UtcNow,
			ExpiresAt = DateTime.UtcNow.AddDays(7),
			IsRevoked = false
		};

		context.RefreshTokens.Add(refreshToken);
		await context.SaveChangesAsync(cancellationToken);

		return token;
	}

	public async Task<(User user, RefreshToken token)?> GetUserFromRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
	{
		RefreshToken? token = await context.RefreshTokens
			.Include(r => r.User)
			.FirstOrDefaultAsync(r =>
				r.Token == refreshToken &&
				!r.IsRevoked &&
				r.ExpiresAt > DateTime.UtcNow,
				cancellationToken);

		return token is null ? null : (token.User, token);
	}

	public async Task<string> RotateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
	{
		refreshToken.IsRevoked = true;

		return await CreateRefreshTokenAsync(refreshToken.User, cancellationToken);
	}
}
