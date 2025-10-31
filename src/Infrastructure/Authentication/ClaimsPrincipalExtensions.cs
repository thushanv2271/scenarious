using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Authentication;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal? principal)
    {
        string? userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userId, out Guid parsedUserId) ?
            parsedUserId :
            throw new ApplicationException("User id is unavailable");
    }

    public static HashSet<string> GetPermissions(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return new HashSet<string>();
        }

        return principal
            .FindAll("permission")
            .Select(claim => claim.Value)
            .ToHashSet();
    }
}
