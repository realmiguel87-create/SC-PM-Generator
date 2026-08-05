using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

public class Programme : SoftDeletableEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal CapitalValue { get; set; }
    public Guid? SponsorUserId { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

public class RibaStageDefinition
{
    public byte StageNumber { get; set; }
    public string StageName { get; set; } = default!;
    public string? Description { get; set; }
}

public class Project : SoftDeletableEntity
{
    public Guid? ProgrammeId { get; set; }
    public Programme? Programme { get; set; }

    public string ProjectRef { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public byte CurrentRibaStage { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    public decimal ApprovedBudget { get; set; }
    public decimal ForecastCost { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }

    public Guid? SponsorUserId { get; set; }
    public Guid? ProjectManagerUserId { get; set; }

    public ICollection<RibaStageInstance> RibaStageInstances { get; set; } = new List<RibaStageInstance>();
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
}

public class RibaStageInstance : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public byte StageNumber { get; set; }
    public RibaStageInstanceStatus Status { get; set; } = RibaStageInstanceStatus.NotStarted;

    public DateOnly? PlannedStartDate { get; set; }
    public DateOnly? PlannedEndDate { get; set; }
    public DateOnly? ActualStartDate { get; set; }
    public DateOnly? ActualEndDate { get; set; }
}

public class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
}
