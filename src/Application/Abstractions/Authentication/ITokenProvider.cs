using Domain.Users;

namespace Application.Abstractions.Authentication;

public interface ITokenProvider
{
    Task<string> CreateAsync(User user);
}
