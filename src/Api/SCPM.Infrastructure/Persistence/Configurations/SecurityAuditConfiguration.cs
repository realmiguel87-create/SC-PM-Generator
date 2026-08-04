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
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Role", "Security");
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();
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
