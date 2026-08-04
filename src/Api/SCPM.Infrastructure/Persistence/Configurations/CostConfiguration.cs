using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class CostPlanConfiguration : IEntityTypeConfiguration<CostPlan>
{
    public void Configure(EntityTypeBuilder<CostPlan> builder)
    {
        builder.ToTable("CostPlan", "Cost", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("CostPlan_History", "Cost");
        }));

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.HasOne(c => c.Project).WithMany().HasForeignKey(c => c.ProjectId);
        builder.HasMany(c => c.Lines).WithOne(l => l.CostPlan).HasForeignKey(l => l.CostPlanId);
    }
}

public class CostPlanLineConfiguration : IEntityTypeConfiguration<CostPlanLine>
{
    public void Configure(EntityTypeBuilder<CostPlanLine> builder)
    {
        builder.ToTable("CostPlanLine", "Cost");
        builder.Property(l => l.CostCategory).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Amount).HasColumnType("decimal(18,2)");
    }
}

public class ForecastConfiguration : IEntityTypeConfiguration<Forecast>
{
    public void Configure(EntityTypeBuilder<Forecast> builder)
    {
        builder.ToTable("Forecast", "Cost", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Forecast_History", "Cost");
        }));

        builder.Property(f => f.ForecastCost).HasColumnType("decimal(18,2)");
        builder.Property(f => f.ApprovedBudgetAtForecast).HasColumnType("decimal(18,2)");
        builder.Ignore(f => f.Variance);

        builder.HasOne(f => f.Project).WithMany().HasForeignKey(f => f.ProjectId);
        builder.HasIndex(f => new { f.ProjectId, f.ForecastDate });
    }
}
