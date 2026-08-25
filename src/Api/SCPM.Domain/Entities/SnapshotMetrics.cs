using System.Linq.Expressions;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

/// <summary>
/// The definitions behind every register aggregate a <see cref="Snapshot"/> captures.
///
/// These live in one place, as expressions rather than prose, because the alternative is that
/// "open risk" comes to mean one thing at capture time and a subtly different thing wherever the
/// figure is next used — a dashboard tile, a committee report, a Power BI dataset. When that
/// happens the numbers disagree and nobody can say which is right, because each definition is
/// buried in a different query. Anything that needs to count the same thing should use these.
///
/// They are <see cref="Expression"/>s, not compiled predicates, so EF Core translates them into
/// SQL rather than pulling whole registers into memory to filter them.
/// </summary>
public static class SnapshotMetrics
{
    /// <summary>
    /// Score at or above which an open risk counts as high — red on the 1-25 probability x
    /// impact heatmap, and the level at which a risk is normally individually reportable.
    /// </summary>
    public const int HighRiskScoreThreshold = 15;

    /// <summary>
    /// A risk the project is still carrying. Escalated counts as open: escalation raises who
    /// decides, it does not retire the risk. Mitigated and Closed do not.
    /// </summary>
    public static Expression<Func<Risk, bool>> IsOpenRisk =>
        r => r.Status == RiskStatus.Open || r.Status == RiskStatus.Escalated;

    /// <summary>An issue not yet Resolved or Closed.</summary>
    public static Expression<Func<Issue, bool>> IsOpenIssue =>
        i => i.Status == IssueStatus.Open || i.Status == IssueStatus.InProgress;

    /// <summary>High or Critical severity — the issues that reach a committee paper.</summary>
    public static Expression<Func<Issue, bool>> IsSevereIssue =>
        i => i.Severity == IssueSeverity.High || i.Severity == IssueSeverity.Critical;

    /// <summary>An early warning still Open.</summary>
    public static Expression<Func<EarlyWarning, bool>> IsOpenEarlyWarning =>
        e => e.Status == Nec4RegisterStatus.Open;

    /// <summary>
    /// A compensation event still in play — notified, quoted, or accepted but not yet
    /// implemented. Implemented and Rejected are both concluded, in opposite directions.
    /// </summary>
    public static Expression<Func<CompensationEvent, bool>> IsOpenCompensationEvent =>
        c => c.Status == CompensationEventStatus.Notified
             || c.Status == CompensationEventStatus.Quoted
             || c.Status == CompensationEventStatus.Accepted;

    /// <summary>
    /// A compensation event whose value the project is still exposed to. Broader than
    /// <see cref="IsOpenCompensationEvent"/> — an implemented CE has been paid for and still
    /// counts against the budget; only a rejected one costs nothing.
    /// </summary>
    public static Expression<Func<CompensationEvent, bool>> CarriesCompensationEventValue =>
        c => c.Status != CompensationEventStatus.Rejected;

    /// <summary>A variation instructed or priced but not yet Agreed.</summary>
    public static Expression<Func<Variation, bool>> IsOpenVariation =>
        v => v.Status != VariationStatus.Agreed;

    /// <summary>
    /// Whether a milestone has slipped against its baseline, judged on dates rather than on
    /// MilestoneStatus: a milestone can be late without anyone having set its status to
    /// Delayed, and the slip is a fact about the dates either way. Actual date wins once it
    /// exists, since a completed milestone's forecast is no longer meaningful.
    /// </summary>
    public static bool IsDelayed(Milestone milestone) => DelayDays(milestone) > 0;

    /// <summary>
    /// Days late against baseline; negative when ahead. Mirrors <see cref="Milestone.DelayDays"/>,
    /// which is computed rather than mapped and so cannot be used inside a database query.
    /// </summary>
    public static int DelayDays(Milestone milestone) =>
        (milestone.ActualDate ?? milestone.ForecastDate).DayNumber - milestone.BaselineDate.DayNumber;
}
