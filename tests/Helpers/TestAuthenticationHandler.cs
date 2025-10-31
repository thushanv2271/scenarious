using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace IntegrationTests.Helpers;

public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string DefaultUserId { get; set; } = Guid.CreateVersion7().ToString();
    public List<string> Permissions { get; set; } = new();
}

public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        IOptionsMonitor<TestAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string userId = Context.Request.Headers["X-Test-UserId"].FirstOrDefault()
            ?? Options.DefaultUserId;

        string[] permissions = Context.Request.Headers["X-Test-Permissions"]
            .FirstOrDefault()?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            ?? Options.Permissions.ToArray();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Email, "test@example.com")
        };

        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
