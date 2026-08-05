using SCPM.Domain.Common;

namespace SCPM.Domain.Entities;

public class User : BaseEntity
{
    public string EntraObjectId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? JobTitle { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class Role : BaseEntity
{
    /// <summary>Machine-readable identifier — matches SCPM.Domain.Enums.RoleName and every
    /// RequireRole()/[Authorize(Policy=...)] check in SCPM.Api/Program.cs verbatim (PascalCase,
    /// no spaces). Authorization matches on this, never on DisplayName.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Human-readable label for UI display (e.g. "Project Sponsor" for Name
    /// "ProjectSponsor") — kept separate from Name specifically so a friendlier display string
    /// can never silently break a RequireRole() match the way a single shared field once did.</summary>
    public string DisplayName { get; set; } = default!;

    public string? Description { get; set; }
}

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;
    public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
}
