using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SCPM.Infrastructure.Persistence;

namespace SCPM.Infrastructure.Identity;

/// <summary>
/// Bridges a validated Entra ID JWT to the platform's internal RBAC model. Without this,
/// ICurrentUserService.UserId always resolves to null (the "scpm_user_id" claim it looks for
/// does not exist on a real Entra ID token — that's a claim this class adds, not one Entra ID
/// issues), so every CreatedBy/ModifiedBy would silently be Guid.Empty and RequireRole policies
/// would never match any role since Entra ID's own token carries no platform role claims.
///
/// Runs once per request after authentication, before authorization: looks up Security.User by
/// the token's object identifier claim, and if found, adds the internal user id
/// ("scpm_user_id", read by CurrentUserService) and one ClaimTypes.Role claim per role the user
/// holds (Security.UserRole). A user authenticated by Entra ID but not provisioned in
/// Security.User gets no role claims — RequireRole policies then correctly deny them (fail
/// closed), rather than needing special-cased handling here.
/// </summary>
public class EntraClaimsTransformation : IClaimsTransformation
{
    public const string UserIdClaimType = "scpm_user_id";

    private readonly AppDbContext _db;

    public EntraClaimsTransformation(AppDbContext db) => _db = db;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated)
            return principal;

        // Already transformed on an earlier middleware pass for this principal instance.
        if (identity.HasClaim(c => c.Type == UserIdClaimType))
            return principal;

        var entraObjectId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
            ?? principal.FindFirstValue("oid");

        if (string.IsNullOrEmpty(entraObjectId))
            return principal;

        var user = await _db.Users
            .Where(u => u.EntraObjectId == entraObjectId && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.DisplayName,
                RoleNames = u.UserRoles.Select(ur => ur.Role.Name)
            })
            .FirstOrDefaultAsync();

        if (user is null)
            return principal;

        identity.AddClaim(new Claim(UserIdClaimType, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
        foreach (var roleName in user.RoleNames)
            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));

        return principal;
    }
}
