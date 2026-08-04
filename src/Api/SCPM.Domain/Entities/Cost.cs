using SCPM.Domain.Common;

namespace SCPM.Domain.Entities;

/// <summary>A version of the cost plan for a project — superseded (not overwritten) each time it's re-baselined.</summary>
public class CostPlan : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Name { get; set; } = default!;
    public int VersionNumber { get; set; } = 1;
    public bool IsBaseline { get; set; }

    public ICollection<CostPlanLine> Lines { get; set; } = new List<CostPlanLine>();
}

public class CostPlanLine : BaseEntity
{
    public Guid CostPlanId { get; set; }
    public CostPlan CostPlan { get; set; } = default!;

    public string CostCategory { get; set; } = default!; // e.g. Construction, Fees, Contingency, FF&E
    public string? Description { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>A point-in-time forecast against the project's approved budget.</summary>
public class Forecast : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public DateOnly ForecastDate { get; set; }
    public decimal ForecastCost { get; set; }
    public decimal ApprovedBudgetAtForecast { get; set; }
    public string? CommentaryNotes { get; set; }

    public decimal Variance => ForecastCost - ApprovedBudgetAtForecast;
}
