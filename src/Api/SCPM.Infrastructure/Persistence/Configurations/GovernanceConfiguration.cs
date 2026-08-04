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
        builder.HasOne(g => g.RibaStageInstance).WithMany().HasForeignKey(g => g.RibaStageInstanceId);
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
