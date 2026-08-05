using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

/// <summary>A delivery-schedule milestone for a project (distinct from Projects.Programme, the
/// portfolio grouping — see docs/erd.md naming note).</summary>
public class Milestone : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public MilestoneStatus Status { get; set; } = MilestoneStatus.NotStarted;

    public DateOnly BaselineDate { get; set; }
    public DateOnly ForecastDate { get; set; }
    public DateOnly? ActualDate { get; set; }

    public bool IsKeyMilestone { get; set; }

    public int DelayDays => ActualDate.HasValue
        ? ActualDate.Value.DayNumber - BaselineDate.DayNumber
        : ForecastDate.DayNumber - BaselineDate.DayNumber;
}
