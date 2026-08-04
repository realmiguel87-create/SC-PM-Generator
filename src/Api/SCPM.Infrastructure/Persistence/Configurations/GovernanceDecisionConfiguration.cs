using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class DecisionRegisterEntryConfiguration : IEntityTypeConfiguration<DecisionRegisterEntry>
{
    public void Configure(EntityTypeBuilder<DecisionRegisterEntry> builder)
    {
        builder.ToTable("DecisionRegisterEntry", "Governance", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("DecisionRegisterEntry_History", "Governance");
        }));

        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).IsRequired();

        builder.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId);
        builder.HasIndex(d => new { d.ProjectId, d.DecisionDate });
    }
}
