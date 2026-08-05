using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class EarlyWarningConfiguration : IEntityTypeConfiguration<EarlyWarning>
{
    public void Configure(EntityTypeBuilder<EarlyWarning> builder)
    {
        builder.ToTable("EarlyWarning", "NEC4", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("EarlyWarning_History", "NEC4");
        }));

        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(e => e.Project).WithMany().HasForeignKey(e => e.ProjectId);
    }
}

public class CompensationEventConfiguration : IEntityTypeConfiguration<CompensationEvent>
{
    public void Configure(EntityTypeBuilder<CompensationEvent> builder)
    {
        builder.ToTable("CompensationEvent", "NEC4", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("CompensationEvent_History", "NEC4");
        }));

        builder.Property(c => c.Reference).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.EstimatedValue).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(c => c.Project).WithMany().HasForeignKey(c => c.ProjectId);
        builder.HasIndex(c => new { c.ProjectId, c.Reference }).IsUnique();
    }
}

public class ContractDataEntryConfiguration : IEntityTypeConfiguration<ContractDataEntry>
{
    public void Configure(EntityTypeBuilder<ContractDataEntry> builder)
    {
        builder.ToTable("ContractDataEntry", "NEC4", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("ContractDataEntry_History", "NEC4");
        }));

        builder.Property(c => c.Part).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.ClauseReference).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Description).IsRequired();
        builder.Property(c => c.Value).IsRequired();
        builder.HasOne(c => c.Project).WithMany().HasForeignKey(c => c.ProjectId);
    }
}

public class RiskAllocationItemConfiguration : IEntityTypeConfiguration<RiskAllocationItem>
{
    public void Configure(EntityTypeBuilder<RiskAllocationItem> builder)
    {
        builder.ToTable("RiskAllocationItem", "NEC4", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("RiskAllocationItem_History", "NEC4");
        }));

        builder.Property(r => r.Description).IsRequired();
        builder.Property(r => r.AllocatedTo).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(r => r.Project).WithMany().HasForeignKey(r => r.ProjectId);
    }
}

public class AcceptedProgrammeEntryConfiguration : IEntityTypeConfiguration<AcceptedProgrammeEntry>
{
    public void Configure(EntityTypeBuilder<AcceptedProgrammeEntry> builder)
    {
        builder.ToTable("AcceptedProgrammeEntry", "NEC4", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("AcceptedProgrammeEntry_History", "NEC4");
        }));

        builder.HasOne(a => a.Project).WithMany().HasForeignKey(a => a.ProjectId);
        builder.HasIndex(a => new { a.ProjectId, a.RevisionNumber }).IsUnique();
    }
}

public class PaymentAssessmentConfiguration : IEntityTypeConfiguration<PaymentAssessment>
{
    public void Configure(EntityTypeBuilder<PaymentAssessment> builder)
    {
        builder.ToTable("PaymentAssessment", "NEC4", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("PaymentAssessment_History", "NEC4");
        }));

        builder.Property(p => p.AmountDue).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(p => p.Project).WithMany().HasForeignKey(p => p.ProjectId);
        builder.HasIndex(p => new { p.ProjectId, p.AssessmentNumber }).IsUnique();
    }
}

public class ChangeRegisterItemConfiguration : IEntityTypeConfiguration<ChangeRegisterItem>
{
    public void Configure(EntityTypeBuilder<ChangeRegisterItem> builder)
    {
        builder.ToTable("ChangeRegisterItem", "NEC4", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("ChangeRegisterItem_History", "NEC4");
        }));

        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.ValueImpact).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(c => c.Project).WithMany().HasForeignKey(c => c.ProjectId);
    }
}
