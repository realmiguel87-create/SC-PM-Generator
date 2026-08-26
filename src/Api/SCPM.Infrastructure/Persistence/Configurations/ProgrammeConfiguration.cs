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

public class ProgrammeBaselineConfiguration : IEntityTypeConfiguration<ProgrammeBaseline>
{
    public void Configure(EntityTypeBuilder<ProgrammeBaseline> builder)
    {
        // Temporal like the rest of the governance record. A baseline is not expected to change
        // after it is set — that is rather the point of one — but "not expected to" and "cannot"
        // are different claims, and an edit to a sanctioned programme is exactly what an auditor
        // asks to see.
        builder.ToTable("ProgrammeBaseline", "Programme", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("ProgrammeBaseline_History", "Programme");
        }));

        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Reason).HasMaxLength(2000).IsRequired();

        builder.HasOne(b => b.Project).WithMany().HasForeignKey(b => b.ProjectId);

        // Revision is unique per project. Two baselines both claiming revision 3 makes "measured
        // against revision 3" ambiguous, and two concurrent rebaselines is the obvious way to get
        // there — the index turns that race into a failed insert rather than a corrupt record.
        builder.HasIndex(b => new { b.ProjectId, b.Revision }).IsUnique();

        // At most one current baseline per project, enforced by the database rather than by the
        // command that happens to maintain it. "Which programme are we measured against?" has to
        // have exactly one answer; two rows claiming it makes every slip figure in the app depend
        // on which one a query happened to pick first. A filtered unique index turns that from a
        // silent wrong number into a failed write.
        //
        // Filtered on IsDeleted too: a soft-deleted baseline still holds IsCurrent = 1 in the row,
        // and without the second clause superseding it would collide with its own replacement.
        builder.HasIndex(b => b.ProjectId)
            .HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0")
            .IsUnique()
            .HasDatabaseName("UX_ProgrammeBaseline_CurrentPerProject");
    }
}

public class ProgrammeBaselineEntryConfiguration : IEntityTypeConfiguration<ProgrammeBaselineEntry>
{
    public void Configure(EntityTypeBuilder<ProgrammeBaselineEntry> builder)
    {
        // Not temporal, unlike its parent. An entry is immutable by construction — the command
        // writes it once and nothing updates it — so a history table would record nothing. The
        // baseline itself carries the temporal record for the case where that assumption breaks.
        builder.ToTable("ProgrammeBaselineEntry", "Programme");

        builder.Property(e => e.MilestoneName).HasMaxLength(200).IsRequired();

        builder.HasOne(e => e.ProgrammeBaseline)
            .WithMany(b => b.Entries)
            .HasForeignKey(e => e.ProgrammeBaselineId)
            // Deleting a baseline takes its entries with it: an entry without its baseline is not
            // a partial record, it is a date with nothing left to say what sanctioned it.
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Milestone)
            .WithMany()
            .HasForeignKey(e => e.MilestoneId)
            // Restrict, not cascade. The entry holds its own copy of the name and date, so it
            // survives the milestone being soft-deleted — the normal case. A hard delete should
            // fail loudly rather than quietly removing a milestone from a programme a committee
            // approved. It also avoids a second cascade path reaching Milestone alongside the one
            // through Project, which SQL Server rejects outright (error 1785) — the same class of
            // failure Phase 5 hit, and which only a real database catches.
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ProgrammeBaselineId, e.MilestoneId }).IsUnique();
    }
}
