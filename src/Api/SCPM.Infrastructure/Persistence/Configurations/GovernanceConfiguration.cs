using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class GatewayConfiguration : IEntityTypeConfiguration<Gateway>
{
    public void Configure(EntityTypeBuilder<Gateway> builder)
    {
        builder.ToTable("Gateway", "Governance", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Gateway_History", "Governance");
        }));

        builder.Property(g => g.GatewayType).HasMaxLength(50).IsRequired();
        builder.Property(g => g.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(g => g.Project).WithMany().HasForeignKey(g => g.ProjectId);
        // RibaStageInstance also cascades from Project, so Project -> Gateway direct and
        // Project -> RibaStageInstance -> Gateway would be two cascade paths to the same
        // row — SQL Server rejects that at CREATE TABLE time (error 1785). Restrict here;
        // the app never hard-deletes anyway (soft delete throughout), so this only affects
        // constraint validity, not real behaviour.
        builder.HasOne(g => g.RibaStageInstance).WithMany().HasForeignKey(g => g.RibaStageInstanceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(g => g.Approvals).WithOne(a => a.Gateway).HasForeignKey(a => a.GatewayId);
    }
}

public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        builder.ToTable("Approval", "Governance", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Approval_History", "Governance");
        }));

        builder.Property(a => a.Decision).HasConversion<string>().HasMaxLength(20);
    }
}
