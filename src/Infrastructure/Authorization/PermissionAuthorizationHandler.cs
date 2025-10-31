using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Authorization;

internal sealed class PermissionAuthorizationHandler(IServiceScopeFactory serviceScopeFactory)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Reject unauthenticated users
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // First try to get permissions from token claims (faster)
        HashSet<string> permissions = context.User.GetPermissions();

        // If no permissions found in token claims, fallback to database lookup
        // This handles cases where tokens were issued before permissions were added or changed yet new token was not created for the user
        if (permissions.Count == 0)
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            PermissionProvider permissionProvider = scope.ServiceProvider.GetRequiredService<PermissionProvider>();
            Guid userId = context.User.GetUserId();
            permissions = await permissionProvider.GetForUserIdAsync(userId);
        }

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
