using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class CommitteeReportConfiguration : IEntityTypeConfiguration<CommitteeReport>
{
    public void Configure(EntityTypeBuilder<CommitteeReport> builder)
    {
        builder.ToTable("CommitteeReport", "Reporting", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("CommitteeReport_History", "Reporting");
        }));

        builder.Property(r => r.ReportType).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.ExecutiveSummary).IsRequired();

        builder.HasOne(r => r.Project).WithMany().HasForeignKey(r => r.ProjectId);
        // Same multiple-cascade-paths issue as DocumentVersion.Snapshot (see
        // DocumentConfiguration) — Snapshot also cascades from Project.
        builder.HasOne(r => r.Snapshot).WithMany().HasForeignKey(r => r.SnapshotId).OnDelete(DeleteBehavior.Restrict);
    }
}
