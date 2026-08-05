namespace SCPM.Application.Common.Interfaces;

/// <summary>Resolves the authenticated user's identity from the Entra ID token for the current request.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? DisplayName { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);
}
