namespace SCPM.Application.Cost.Dtos;

public class CostPlanLineDto
{
    public string CostCategory { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
}

public class CostPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public int VersionNumber { get; set; }
    public bool IsBaseline { get; set; }
    public decimal TotalAmount { get; set; }
    public List<CostPlanLineDto> Lines { get; set; } = new();
}

public class ForecastDto
{
    public Guid Id { get; set; }
    public DateOnly ForecastDate { get; set; }
    public decimal ForecastCost { get; set; }
    public decimal ApprovedBudgetAtForecast { get; set; }
    public decimal Variance { get; set; }
    public string? CommentaryNotes { get; set; }
}

public class CostSummaryDto
{
    public Guid ProjectId { get; set; }
    public decimal ApprovedBudget { get; set; }
    public decimal CurrentForecastCost { get; set; }
    public decimal CurrentVariance { get; set; }
    public CostPlanDto? BaselineCostPlan { get; set; }
    public List<ForecastDto> ForecastHistory { get; set; } = new();
}
