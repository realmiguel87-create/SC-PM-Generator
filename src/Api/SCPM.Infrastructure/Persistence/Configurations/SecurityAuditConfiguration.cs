using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User", "Security");
        builder.Property(u => u.EntraObjectId).HasMaxLength(100).IsRequired();
        builder.HasIndex(u => u.EntraObjectId).IsUnique();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    // Fixed GUIDs (not Guid.NewGuid()) so the seed is stable across migrations —
    // a random value here would produce a spurious pending migration on every build.
    public static readonly Guid AdministratorId    = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DirectorId         = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid ProjectSponsorId   = new("00000000-0000-0000-0000-000000000003");
    public static readonly Guid ProgrammeManagerId = new("00000000-0000-0000-0000-000000000004");
    public static readonly Guid ProjectManagerId   = new("00000000-0000-0000-0000-000000000005");
    public static readonly Guid CommercialManagerId = new("00000000-0000-0000-0000-000000000006");
    public static readonly Guid QuantitySurveyorId = new("00000000-0000-0000-0000-000000000007");
    public static readonly Guid GovernanceOfficerId = new("00000000-0000-0000-0000-000000000008");
    public static readonly Guid CommitteeOfficerId = new("00000000-0000-0000-0000-000000000009");
    public static readonly Guid ReadOnlyUserId     = new("00000000-0000-0000-0000-000000000010");

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Role", "Security");
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.DisplayName).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();

        // CreatedDate is pinned (not DateTime.UtcNow) so re-running `dotnet ef migrations add`
        // reproduces byte-identical seed data instead of baking in "now" as a migration constant.
        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Name MUST match SCPM.Domain.Enums.RoleName and every RequireRole()/[Authorize] check in
        // SCPM.Api/Program.cs exactly (PascalCase, no spaces) — these are what RBAC actually
        // matches on. DisplayName carries the human-readable label for UI use. A prior version of
        // this seed used the display text as Name itself ("Project Sponsor" etc.), which silently
        // never matched any RequireRole("ProjectSponsor") policy for every multi-word role —
        // caught only by an integration test that exercised the real ASP.NET Core authorization
        // pipeline end to end (SCPM.IntegrationTests/RbacTests.cs), not by unit tests that mock
        // ICurrentUserService and never touch role-string matching at all.
        builder.HasData(
            new Role { Id = AdministratorId, Name = "Administrator", DisplayName = "Administrator", Description = "Full platform administration", CreatedBy = Guid.Empty, CreatedDate = seededAt },
            new Role { Id = DirectorId, Name = "Director", DisplayName = "Director", Description = "Portfolio-wide oversight and approval", CreatedBy = Guid.Empty, CreatedDate = seededAt },
            new Role { Id = ProjectSponsorId, Name = "ProjectSponsor", DisplayName = "Project Sponsor", Description = "Accountable owner for assigned project(s)", CreatedBy = Guid.Empty, CreatedDate = seededAt },
            new Role { Id = ProgrammeManagerId, Name = "ProgrammeManager", DisplayName = "Programme Manager", Description = "Manages a capital programme (group of projects)", CreatedBy = Guid.Empty, CreatedDate = seededAt },
            new Role { Id = ProjectManagerId, Name = "ProjectManager", DisplayName = "Project Manager", Description = "Day-to-day delivery of assigned project(s)", CreatedBy = Guid.Empty, CreatedDate = seededAt },
            new Role { Id = CommercialManagerId, Name = "CommercialManager", DisplayName = "Commercial Manager", Description = "NEC4/SBCC contract administration", CreatedBy = Guid.Empty, CreatedDate = seededAt },
            new Role { Id = QuantitySurveyorId, Name = "QuantitySurveyor", DisplayName = "Quantity Surveyor", Description = "Cost management and valuations", CreatedBy = Guid.Empty, CreatedDate = seededAt },
            new Role { Id = GovernanceOfficerId, Name = "GovernanceOfficer", DisplayName = "Governance Officer", Description = "Gateway/approval process administration", CreatedBy = Guid.Empty, CreatedDate = seededAt },
            new Role { Id = CommitteeOfficerId, Name = "CommitteeOfficer", DisplayName = "Committee Officer", Description = "Committee and cabinet reporting", CreatedBy = Guid.Empty, CreatedDate = seededAt },
            new Role { Id = ReadOnlyUserId, Name = "ReadOnlyUser", DisplayName = "Read Only User", Description = "View-only access", CreatedBy = Guid.Empty, CreatedDate = seededAt }
        );
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRole", "Security");
        builder.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
        builder.HasOne(ur => ur.Role).WithMany().HasForeignKey(ur => ur.RoleId);
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
    }
}

public class ActivityLogEntryConfiguration : IEntityTypeConfiguration<ActivityLogEntry>
{
    public void Configure(EntityTypeBuilder<ActivityLogEntry> builder)
    {
        builder.ToTable("ActivityLog", "Audit");
        builder.Property(a => a.Action).HasMaxLength(30).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.HasMany(a => a.FieldChanges).WithOne().HasForeignKey(f => f.ActivityLogId);
    }
}

public class FieldAuditEntryConfiguration : IEntityTypeConfiguration<FieldAuditEntry>
{
    public void Configure(EntityTypeBuilder<FieldAuditEntry> builder)
    {
        builder.ToTable("FieldAudit", "Audit");
        builder.Property(f => f.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(f => f.FieldName).HasMaxLength(100).IsRequired();
    }
}
