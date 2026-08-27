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

        builder.HasOne(r => r.Project).WithMany().HasForeignKey(r => r.ProjectId);
        // Same multiple-cascade-paths issue as DocumentVersion.Snapshot (see
        // DocumentConfiguration) — Snapshot also cascades from Project.
        builder.HasOne(r => r.Snapshot).WithMany().HasForeignKey(r => r.SnapshotId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommitteeReportSectionContentConfiguration
    : IEntityTypeConfiguration<CommitteeReportSectionContent>
{
    public void Configure(EntityTypeBuilder<CommitteeReportSectionContent> builder)
    {
        // Temporal like its parent. A report's wording before it went to committee, and what it
        // said afterwards, is exactly the sort of thing someone asks about a year later.
        builder.ToTable("CommitteeReportSection", "Reporting", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("CommitteeReportSection_History", "Reporting");
        }));

        builder.Property(s => s.SectionKey).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Content).IsRequired();

        builder.HasOne(s => s.CommitteeReport)
            .WithMany(r => r.Sections)
            .HasForeignKey(s => s.CommitteeReportId)
            .OnDelete(DeleteBehavior.Cascade);

        // One row per section per report. Two rows for the same heading would make the document's
        // content depend on which one a query returned first — and both would be plausible.
        builder.HasIndex(s => new { s.CommitteeReportId, s.SectionKey }).IsUnique();
    }
}
