using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> builder)
    {
        builder.ToTable("Milestone", "Programme", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Milestone_History", "Programme");
        }));

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(30);
        builder.Ignore(m => m.DelayDays);

        builder.HasOne(m => m.Project).WithMany().HasForeignKey(m => m.ProjectId);
        builder.HasIndex(m => new { m.ProjectId, m.ForecastDate });
    }
}
