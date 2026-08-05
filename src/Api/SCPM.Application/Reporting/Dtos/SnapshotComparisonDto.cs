namespace SCPM.Application.Reporting.Dtos;

public class SnapshotComparisonDto
{
    public Guid FromSnapshotId { get; set; }
    public string FromLabel { get; set; } = default!;
    public DateTime FromCapturedAt { get; set; }

    public Guid ToSnapshotId { get; set; }
    public string ToLabel { get; set; } = default!;
    public DateTime ToCapturedAt { get; set; }

    public byte FromRibaStage { get; set; }
    public byte ToRibaStage { get; set; }

    public decimal FromApprovedBudget { get; set; }
    public decimal ToApprovedBudget { get; set; }
    public decimal BudgetDelta => ToApprovedBudget - FromApprovedBudget;

    public decimal FromForecastCost { get; set; }
    public decimal ToForecastCost { get; set; }
    public decimal ForecastDelta => ToForecastCost - FromForecastCost;
}
