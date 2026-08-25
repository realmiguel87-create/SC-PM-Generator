namespace SCPM.Application.Reporting.Dtos;

/// <summary>
/// Two snapshots of the same project side by side, with the movement between them.
///
/// Every delta is To minus From, without exception — including the ones where "up" is bad
/// (risk, delay, compensation events). A positive number therefore always means the figure
/// increased, never "improved", so a reader never has to remember which way round a particular
/// field was defined. Interpretation belongs to whatever presents it.
/// </summary>
public class SnapshotComparisonDto
{
    public Guid FromSnapshotId { get; set; }
    public string FromLabel { get; set; } = default!;
    public DateTime FromCapturedAt { get; set; }

    public Guid ToSnapshotId { get; set; }
    public string ToLabel { get; set; } = default!;
    public DateTime ToCapturedAt { get; set; }

    // --- Project header ---
    public byte FromRibaStage { get; set; }
    public byte ToRibaStage { get; set; }

    public decimal FromApprovedBudget { get; set; }
    public decimal ToApprovedBudget { get; set; }
    public decimal BudgetDelta => ToApprovedBudget - FromApprovedBudget;

    public decimal FromForecastCost { get; set; }
    public decimal ToForecastCost { get; set; }
    public decimal ForecastDelta => ToForecastCost - FromForecastCost;

    // --- Risk ---
    public int FromOpenRiskCount { get; set; }
    public int ToOpenRiskCount { get; set; }
    public int OpenRiskCountDelta => ToOpenRiskCount - FromOpenRiskCount;

    public int FromHighRiskCount { get; set; }
    public int ToHighRiskCount { get; set; }
    public int HighRiskCountDelta => ToHighRiskCount - FromHighRiskCount;

    public int FromTotalOpenRiskScore { get; set; }
    public int ToTotalOpenRiskScore { get; set; }
    public int TotalOpenRiskScoreDelta => ToTotalOpenRiskScore - FromTotalOpenRiskScore;

    // --- Issues ---
    public int FromOpenIssueCount { get; set; }
    public int ToOpenIssueCount { get; set; }
    public int OpenIssueCountDelta => ToOpenIssueCount - FromOpenIssueCount;

    public int FromSevereOpenIssueCount { get; set; }
    public int ToSevereOpenIssueCount { get; set; }
    public int SevereOpenIssueCountDelta => ToSevereOpenIssueCount - FromSevereOpenIssueCount;

    // --- Programme ---
    public int FromMilestoneCount { get; set; }
    public int ToMilestoneCount { get; set; }
    public int MilestoneCountDelta => ToMilestoneCount - FromMilestoneCount;

    public int FromMilestonesCompleteCount { get; set; }
    public int ToMilestonesCompleteCount { get; set; }
    public int MilestonesCompleteCountDelta => ToMilestonesCompleteCount - FromMilestonesCompleteCount;

    public int FromMilestonesDelayedCount { get; set; }
    public int ToMilestonesDelayedCount { get; set; }
    public int MilestonesDelayedCountDelta => ToMilestonesDelayedCount - FromMilestonesDelayedCount;

    public int FromWorstMilestoneDelayDays { get; set; }
    public int ToWorstMilestoneDelayDays { get; set; }
    public int WorstMilestoneDelayDaysDelta => ToWorstMilestoneDelayDays - FromWorstMilestoneDelayDays;

    // --- NEC4 ---
    public int FromOpenEarlyWarningCount { get; set; }
    public int ToOpenEarlyWarningCount { get; set; }
    public int OpenEarlyWarningCountDelta => ToOpenEarlyWarningCount - FromOpenEarlyWarningCount;

    public int FromOpenCompensationEventCount { get; set; }
    public int ToOpenCompensationEventCount { get; set; }
    public int OpenCompensationEventCountDelta => ToOpenCompensationEventCount - FromOpenCompensationEventCount;

    public decimal FromCompensationEventValue { get; set; }
    public decimal ToCompensationEventValue { get; set; }
    public decimal CompensationEventValueDelta => ToCompensationEventValue - FromCompensationEventValue;

    // --- SBCC ---
    public int FromOpenVariationCount { get; set; }
    public int ToOpenVariationCount { get; set; }
    public int OpenVariationCountDelta => ToOpenVariationCount - FromOpenVariationCount;

    public decimal FromVariationValue { get; set; }
    public decimal ToVariationValue { get; set; }
    public decimal VariationValueDelta => ToVariationValue - FromVariationValue;

    public int FromExtensionOfTimeDaysAwarded { get; set; }
    public int ToExtensionOfTimeDaysAwarded { get; set; }
    public int ExtensionOfTimeDaysAwardedDelta => ToExtensionOfTimeDaysAwarded - FromExtensionOfTimeDaysAwarded;
}
