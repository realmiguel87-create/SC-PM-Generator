using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class VariationConfiguration : IEntityTypeConfiguration<Variation>
{
    public void Configure(EntityTypeBuilder<Variation> builder)
    {
        builder.ToTable("Variation", "SBCC", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Variation_History", "SBCC");
        }));

        builder.Property(v => v.Reference).HasMaxLength(30).IsRequired();
        builder.Property(v => v.Description).IsRequired();
        builder.Property(v => v.ValueImpact).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(v => v.Project).WithMany().HasForeignKey(v => v.ProjectId);
        builder.HasIndex(v => new { v.ProjectId, v.Reference }).IsUnique();
    }
}

public class ExtensionOfTimeConfiguration : IEntityTypeConfiguration<ExtensionOfTime>
{
    public void Configure(EntityTypeBuilder<ExtensionOfTime> builder)
    {
        builder.ToTable("ExtensionOfTime", "SBCC", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("ExtensionOfTime_History", "SBCC");
        }));

        builder.Property(e => e.Reference).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Reason).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(e => e.Project).WithMany().HasForeignKey(e => e.ProjectId);
        builder.HasIndex(e => new { e.ProjectId, e.Reference }).IsUnique();
    }
}

public class LossAndExpenseClaimConfiguration : IEntityTypeConfiguration<LossAndExpenseClaim>
{
    public void Configure(EntityTypeBuilder<LossAndExpenseClaim> builder)
    {
        builder.ToTable("LossAndExpenseClaim", "SBCC", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("LossAndExpenseClaim_History", "SBCC");
        }));

        builder.Property(l => l.Reference).HasMaxLength(30).IsRequired();
        builder.Property(l => l.Description).IsRequired();
        builder.Property(l => l.ClaimedAmount).HasColumnType("decimal(18,2)");
        builder.Property(l => l.AwardedAmount).HasColumnType("decimal(18,2)");
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(l => l.Project).WithMany().HasForeignKey(l => l.ProjectId);
        builder.HasIndex(l => new { l.ProjectId, l.Reference }).IsUnique();
    }
}

public class ArchitectsInstructionConfiguration : IEntityTypeConfiguration<ArchitectsInstruction>
{
    public void Configure(EntityTypeBuilder<ArchitectsInstruction> builder)
    {
        builder.ToTable("ArchitectsInstruction", "SBCC", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("ArchitectsInstruction_History", "SBCC");
        }));

        builder.Property(a => a.Description).IsRequired();
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(a => a.Project).WithMany().HasForeignKey(a => a.ProjectId);
        builder.HasIndex(a => new { a.ProjectId, a.InstructionNumber }).IsUnique();
    }
}

public class InterimValuationConfiguration : IEntityTypeConfiguration<InterimValuation>
{
    public void Configure(EntityTypeBuilder<InterimValuation> builder)
    {
        builder.ToTable("InterimValuation", "SBCC", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("InterimValuation_History", "SBCC");
        }));

        builder.Property(i => i.GrossValuation).HasColumnType("decimal(18,2)");
        builder.Property(i => i.NetPayment).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(i => i.Project).WithMany().HasForeignKey(i => i.ProjectId);
        builder.HasIndex(i => new { i.ProjectId, i.ValuationNumber }).IsUnique();
    }
}
