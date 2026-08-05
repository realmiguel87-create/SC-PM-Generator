using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SCPM.IntegrationTests;

/// <summary>
/// Replaces real Entra ID JWT bearer validation in tests. Deliberately does the *minimum* real
/// auth would do — set the object-identifier claim from a request header — and leaves everything
/// downstream (EntraClaimsTransformation resolving the user + their roles from Security.User/
/// UserRole, then ASP.NET Core's RequireRole() policies) to run for real. That's what makes
/// RbacTests an actual test of the RBAC wiring end to end, not a test of a mock.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string EntraOidHeader = "X-Test-Entra-Oid";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(EntraOidHeader, out var entraOid) || string.IsNullOrWhiteSpace(entraOid))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, entraOid.ToString())], SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
