using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SCPM.Application.Common.Interfaces;

namespace SCPM.Infrastructure.Identity;

/// <summary>Resolves the current user from the validated Entra ID JWT on HttpContext.</summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("scpm_user_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? DisplayName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

    public IReadOnlyCollection<string> Roles =>
        _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

    public bool IsInRole(string role) => _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
}
