using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class SnapshotConfiguration : IEntityTypeConfiguration<Snapshot>
{
    public void Configure(EntityTypeBuilder<Snapshot> builder)
    {
        // Snapshots are themselves an immutable point-in-time record, so unlike the entities
        // they capture, this table is not temporal — there is nothing to version.
        builder.ToTable("Snapshot", "Reporting");

        builder.Property(s => s.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Label).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ApprovedBudgetAtCapture).HasColumnType("decimal(18,2)");
        builder.Property(s => s.ForecastCostAtCapture).HasColumnType("decimal(18,2)");

        // Same precision as the columns they aggregate (CompensationEvent.EstimatedValue,
        // Variation.ValueImpact). A snapshot that rounded differently from the register it
        // captured would eventually show a delta where nothing had actually changed.
        builder.Property(s => s.CompensationEventValue).HasColumnType("decimal(18,2)");
        builder.Property(s => s.VariationValue).HasColumnType("decimal(18,2)");

        builder.HasOne(s => s.Project).WithMany().HasForeignKey(s => s.ProjectId);
        builder.HasIndex(s => new { s.ProjectId, s.CapturedAt });
    }
}
