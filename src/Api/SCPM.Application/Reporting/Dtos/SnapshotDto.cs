namespace SCPM.Application.Reporting.Dtos;

public class SnapshotDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Label { get; set; } = default!;
    public DateTime CapturedAt { get; set; }

    public byte RibaStageAtCapture { get; set; }
    public decimal ApprovedBudgetAtCapture { get; set; }
    public decimal ForecastCostAtCapture { get; set; }

    // Register aggregates — see Snapshot and SnapshotMetrics for exactly what each one counts.
    public int OpenRiskCount { get; set; }
    public int HighRiskCount { get; set; }
    public int TotalOpenRiskScore { get; set; }

    public int OpenIssueCount { get; set; }
    public int SevereOpenIssueCount { get; set; }

    public int MilestoneCount { get; set; }
    public int MilestonesCompleteCount { get; set; }
    public int MilestonesDelayedCount { get; set; }
    public int WorstMilestoneDelayDays { get; set; }

    public int OpenEarlyWarningCount { get; set; }
    public int OpenCompensationEventCount { get; set; }
    public decimal CompensationEventValue { get; set; }

    public int OpenVariationCount { get; set; }
    public decimal VariationValue { get; set; }
    public int ExtensionOfTimeDaysAwarded { get; set; }
}
