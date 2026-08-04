using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Configurations;

public class ProgrammeConfiguration : IEntityTypeConfiguration<Programme>
{
    public void Configure(EntityTypeBuilder<Programme> builder)
    {
        builder.ToTable("Programme", "Projects", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Programme_History", "Projects");
        }));

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.CapitalValue).HasColumnType("decimal(18,2)");
        builder.HasMany(p => p.Projects).WithOne(pr => pr.Programme).HasForeignKey(pr => pr.ProgrammeId);
    }
}

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Project", "Projects", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("Project_History", "Projects");
        }));

        builder.Property(p => p.ProjectRef).HasMaxLength(20).IsRequired();
        builder.HasIndex(p => p.ProjectRef).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.ApprovedBudget).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ForecastCost).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(p => p.Programme).WithMany(pr => pr.Projects).HasForeignKey(p => p.ProgrammeId);
        builder.HasMany(p => p.RibaStageInstances).WithOne(s => s.Project).HasForeignKey(s => s.ProjectId);
    }
}

public class RibaStageDefinitionConfiguration : IEntityTypeConfiguration<RibaStageDefinition>
{
    public void Configure(EntityTypeBuilder<RibaStageDefinition> builder)
    {
        builder.ToTable("RibaStageDefinition", "Projects");
        builder.HasKey(s => s.StageNumber);
        builder.Property(s => s.StageName).HasMaxLength(100).IsRequired();

        builder.HasData(
            new RibaStageDefinition { StageNumber = 0, StageName = "Strategic Definition", Description = "Identify the need, establish the business case and project brief." },
            new RibaStageDefinition { StageNumber = 1, StageName = "Preparation and Brief", Description = "Develop project objectives, quality objectives and project outcomes." },
            new RibaStageDefinition { StageNumber = 2, StageName = "Concept Design", Description = "Prepare concept design aligned to project brief." },
            new RibaStageDefinition { StageNumber = 3, StageName = "Spatial Coordination", Description = "Undertake technical design coordination and spatial coordination." },
            new RibaStageDefinition { StageNumber = 4, StageName = "Technical Design", Description = "Develop technical design in accordance with design responsibility matrix." },
            new RibaStageDefinition { StageNumber = 5, StageName = "Manufacturing and Construction", Description = "Manufacture building systems and construct on site." },
            new RibaStageDefinition { StageNumber = 6, StageName = "Handover", Description = "Conclude building contract, handover of building and asset information." },
            new RibaStageDefinition { StageNumber = 7, StageName = "Use", Description = "Undertake in-use services in accordance with schedule of services." }
        );
    }
}

public class RibaStageInstanceConfiguration : IEntityTypeConfiguration<RibaStageInstance>
{
    public void Configure(EntityTypeBuilder<RibaStageInstance> builder)
    {
        builder.ToTable("RibaStageInstance", "Projects", b => b.IsTemporal(t =>
        {
            t.HasPeriodStart("SysStartTime");
            t.HasPeriodEnd("SysEndTime");
            t.UseHistoryTable("RibaStageInstance_History", "Projects");
        }));

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(s => new { s.ProjectId, s.StageNumber }).IsUnique();
    }
}

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMember", "Projects");
        builder.HasOne(m => m.Project).WithMany(p => p.Members).HasForeignKey(m => m.ProjectId);
    }
}
