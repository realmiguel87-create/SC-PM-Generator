using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class StakeholderConfiguration : IEntityTypeConfiguration<Stakeholder>
{
    public void Configure(EntityTypeBuilder<Stakeholder> builder)
    {
        builder.ToTable("Stakeholder", "Stakeholder", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Stakeholder_History", "Stakeholder");
        }));

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Influence).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Interest).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(s => s.Project).WithMany().HasForeignKey(s => s.ProjectId);
        builder.HasMany(s => s.Engagements).WithOne(e => e.Stakeholder).HasForeignKey(e => e.StakeholderId);
    }
}

public class StakeholderEngagementConfiguration : IEntityTypeConfiguration<StakeholderEngagement>
{
    public void Configure(EntityTypeBuilder<StakeholderEngagement> builder)
    {
        builder.ToTable("StakeholderEngagement", "Stakeholder");
        builder.Property(e => e.Method).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Summary).IsRequired();
    }
}
