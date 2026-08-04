using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class RiskConfiguration : IEntityTypeConfiguration<Risk>
{
    public void Configure(EntityTypeBuilder<Risk> builder)
    {
        builder.ToTable("Risk", "Risk", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Risk_History", "Risk");
        }));

        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Category).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(r => r.Score);

        builder.HasOne(r => r.Project).WithMany().HasForeignKey(r => r.ProjectId);
        builder.ToTable(t => t.HasCheckConstraint("CK_Risk_Probability", "[Probability] BETWEEN 1 AND 5"));
        builder.ToTable(t => t.HasCheckConstraint("CK_Risk_Impact", "[Impact] BETWEEN 1 AND 5"));
    }
}

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("Issue", "Risk", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Issue_History", "Risk");
        }));

        builder.Property(i => i.Title).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(i => i.Project).WithMany().HasForeignKey(i => i.ProjectId);
    }
}

public class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> builder)
    {
        builder.ToTable("Opportunity", "Risk", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Opportunity_History", "Risk");
        }));

        builder.Property(o => o.Title).HasMaxLength(200).IsRequired();
        builder.Property(o => o.PotentialValue).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(o => o.Project).WithMany().HasForeignKey(o => o.ProjectId);
        builder.ToTable(t => t.HasCheckConstraint("CK_Opportunity_Probability", "[Probability] BETWEEN 1 AND 5"));
    }
}

public class EscalationConfiguration : IEntityTypeConfiguration<Escalation>
{
    public void Configure(EntityTypeBuilder<Escalation> builder)
    {
        builder.ToTable("Escalation", "Risk", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Escalation_History", "Risk");
        }));

        builder.Property(e => e.Reason).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(e => e.Project).WithMany().HasForeignKey(e => e.ProjectId);
        // Risk/Issue also cascade from Project, so these need Restrict — same multiple-cascade-
        // paths issue as Gateway.RibaStageInstance (see GovernanceConfiguration).
        builder.HasOne(e => e.Risk).WithMany().HasForeignKey(e => e.RiskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Issue).WithMany().HasForeignKey(e => e.IssueId).OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Escalation_ExactlyOneSource",
            "([RiskId] IS NOT NULL AND [IssueId] IS NULL) OR ([RiskId] IS NULL AND [IssueId] IS NOT NULL)"));
    }
}
