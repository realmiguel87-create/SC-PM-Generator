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
    public string Name { get; set; } = default!;
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
