using Domain.Authentication;
using Domain.Users;

namespace Application.Abstractions.Authentication;

public interface IRefreshTokenService
{
	Task<string> CreateRefreshTokenAsync(User user, CancellationToken cancellationToken);
	Task<(User user, RefreshToken token)?> GetUserFromRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
	Task<string> RotateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
}
