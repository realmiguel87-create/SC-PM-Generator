using System.Globalization;
using SCPM.Application.Reporting.Dtos;

namespace SCPM.Application.Reporting.Export;

/// <summary>
/// Turns the three snapshot-comparison queries into one exportable document.
///
/// All three go in together because they answer one question at three depths, and separating
/// them in an export would let a reader take away the reassuring half. The aggregate table says
/// the risk count went from 12 to 14; the item table names which two were raised; the interval
/// section says a third was raised and closed inside the period and appears in neither of the
/// first two. A pack containing only the first would be true and misleading.
///
/// Building the document here rather than in the exporter keeps the interesting decisions — what
/// each column says, how a null reads, what an empty section says — in the Application layer,
/// where they can be tested without generating a PDF to inspect.
/// </summary>
public static class ComparisonExportBuilder
{
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    public static ExportDocument Build(
        SnapshotComparisonDto summary,
        SnapshotItemComparisonDto items,
        SnapshotIntervalActivityDto interval)
    {
        var tables = new List<ExportTable> { SummaryTable(summary) };

        tables.AddRange(ItemTables(items));
        tables.Add(IntervalTable(interval));

        return new ExportDocument(
            Title: "Snapshot Comparison",
            Subtitle: $"{summary.FromLabel} → {summary.ToLabel}",
            MetaLines:
            [
                $"From: {summary.FromLabel} ({summary.FromCapturedAt.ToString("d MMMM yyyy HH:mm", Uk)} UTC)",
                $"To: {summary.ToLabel} ({summary.ToCapturedAt.ToString("d MMMM yyyy HH:mm", Uk)} UTC)",
                // Said once, at the top, because every delta below depends on it and a reader who
                // assumes "positive is good" will misread the risk and compensation-event rows.
                "Every movement is To minus From. A positive number means the figure increased, "
                    + "which is not the same as improved.",
            ],
            Tables: tables);
    }

    private static ExportTable SummaryTable(SnapshotComparisonDto s) => new(
        "Headline movements",
        ["Measure", s.FromLabel, s.ToLabel, "Movement"],
        [
            Row("RIBA stage", s.FromRibaStage.ToString(), s.ToRibaStage.ToString(),
                s.ToRibaStage == s.FromRibaStage ? "No change" : $"Stage {s.FromRibaStage} → {s.ToRibaStage}"),
            Money("Approved budget", s.FromApprovedBudget, s.ToApprovedBudget, s.BudgetDelta),
            Money("Forecast cost", s.FromForecastCost, s.ToForecastCost, s.ForecastDelta),
            Count("Open risks", s.FromOpenRiskCount, s.ToOpenRiskCount, s.OpenRiskCountDelta),
            Count("High risks (15+)", s.FromHighRiskCount, s.ToHighRiskCount, s.HighRiskCountDelta),
            Count("Total open risk score", s.FromTotalOpenRiskScore, s.ToTotalOpenRiskScore, s.TotalOpenRiskScoreDelta),
            Count("Open issues", s.FromOpenIssueCount, s.ToOpenIssueCount, s.OpenIssueCountDelta),
            Count("Severe open issues", s.FromSevereOpenIssueCount, s.ToSevereOpenIssueCount, s.SevereOpenIssueCountDelta),
            Count("Milestones delayed", s.FromMilestonesDelayedCount, s.ToMilestonesDelayedCount, s.MilestonesDelayedCountDelta),
            Count("Worst milestone slip (days)", s.FromWorstMilestoneDelayDays, s.ToWorstMilestoneDelayDays, s.WorstMilestoneDelayDaysDelta),
            Count("Open early warnings", s.FromOpenEarlyWarningCount, s.ToOpenEarlyWarningCount, s.OpenEarlyWarningCountDelta),
            Count("Open compensation events", s.FromOpenCompensationEventCount, s.ToOpenCompensationEventCount, s.OpenCompensationEventCountDelta),
            Money("Compensation event value", s.FromCompensationEventValue, s.ToCompensationEventValue, s.CompensationEventValueDelta),
            Count("Open variations", s.FromOpenVariationCount, s.ToOpenVariationCount, s.OpenVariationCountDelta),
            Money("Variation value", s.FromVariationValue, s.ToVariationValue, s.VariationValueDelta),
            Count("EOT days awarded", s.FromExtensionOfTimeDaysAwarded, s.ToExtensionOfTimeDaysAwarded, s.ExtensionOfTimeDaysAwardedDelta),
        ]);

    private static IEnumerable<ExportTable> ItemTables(SnapshotItemComparisonDto items)
    {
        yield return new ExportTable(
            "Risk changes",
            ["Change", "Risk", "Status", "Score", "Movement"],
            [.. items.RiskChanges.Select(c => Row(
                c.ChangeType.ToString(),
                c.Title,
                Transition(c.FromStatus, c.ToStatus),
                Transition(Text(c.FromScore), Text(c.ToScore)),
                Signed(c.ScoreDelta)))])
        { EmptyMessage = "No individual risk changed between these two points." };

        yield return new ExportTable(
            "Milestone changes",
            ["Change", "Milestone", "Status", "Days against baseline", "Movement"],
            [.. items.MilestoneChanges.Select(c => Row(
                c.ChangeType.ToString(),
                c.Name,
                Transition(c.FromStatus, c.ToStatus),
                Transition(Text(c.FromDelayDays), Text(c.ToDelayDays)),
                Signed(c.DelayDaysDelta)))])
        { EmptyMessage = "No milestone changed between these two points." };

        yield return new ExportTable(
            "Early warning changes (NEC4)",
            ["Change", "Early warning", "Status"],
            [.. items.EarlyWarningChanges.Select(c => Row(
                c.ChangeType.ToString(),
                c.Title,
                Transition(c.FromStatus, c.ToStatus)))])
        { EmptyMessage = "No early warning changed between these two points." };

        yield return new ExportTable(
            "Compensation event changes (NEC4)",
            ["Change", "Reference", "Title", "Status", "Estimated value", "Movement"],
            [.. items.CompensationEventChanges.Select(c => Row(
                c.ChangeType.ToString(),
                c.Reference,
                c.Title,
                Transition(c.FromStatus, c.ToStatus),
                Transition(Money(c.FromEstimatedValue), Money(c.ToEstimatedValue)),
                SignedMoney(c.EstimatedValueDelta)))])
        { EmptyMessage = "No compensation event changed between these two points." };

        yield return new ExportTable(
            "Variation changes (SBCC)",
            ["Change", "Reference", "Description", "Status", "Value impact", "Movement"],
            [.. items.VariationChanges.Select(c => Row(
                c.ChangeType.ToString(),
                c.Reference,
                c.Description,
                Transition(c.FromStatus, c.ToStatus),
                Transition(Money(c.FromValueImpact), Money(c.ToValueImpact)),
                SignedMoney(c.ValueImpactDelta)))])
        { EmptyMessage = "No variation changed between these two points." };

        yield return new ExportTable(
            "Extension of time changes (SBCC)",
            ["Change", "Reference", "Reason", "Status", "Days claimed", "Days awarded"],
            [.. items.ExtensionOfTimeChanges.Select(c => Row(
                c.ChangeType.ToString(),
                c.Reference,
                c.Reason,
                Transition(c.FromStatus, c.ToStatus),
                Transition(Text(c.FromDaysClaimed), Text(c.ToDaysClaimed)),
                // "Undetermined" rather than a dash or a zero: an EOT claim with no decision yet
                // is a different thing from one determined at zero days, and in a contractual
                // document that difference is the point.
                Transition(Days(c.FromDaysAwarded), Days(c.ToDaysAwarded))))])
        { EmptyMessage = "No extension of time changed between these two points." };
    }

    private static ExportTable IntervalTable(SnapshotIntervalActivityDto interval) => new(
        "Also happened in between",
        ["Register", "Item", "Activity", "Revisions"],
        [.. interval.Items.Select(i => Row(
            i.Register,
            i.Name,
            i.ActivityType switch
            {
                IntervalActivityType.RaisedAndRemoved => "Raised and removed within the period",
                IntervalActivityType.ChangedAndReverted => "Changed and changed back within the period",
                _ => i.ActivityType.ToString(),
            },
            i.VersionCount.ToString()))])
    {
        EmptyMessage = "Nothing was raised and removed, or changed and reverted, within the period.",
    };

    private static IReadOnlyList<string> Row(params string[] cells) => cells;

    private static IReadOnlyList<string> Money(string label, decimal from, decimal to, decimal delta) =>
        Row(label, Money(from), Money(to), SignedMoney(delta));

    private static IReadOnlyList<string> Count(string label, int from, int to, int delta) =>
        Row(label, from.ToString(), to.ToString(), Signed(delta));

    private static string Money(decimal value) => value.ToString("C0", Uk);

    private static string Money(decimal? value) => value.HasValue ? Money(value.Value) : "—";

    private static string Text(int? value) => value?.ToString() ?? "—";

    private static string Days(int? value) => value.HasValue ? value.Value.ToString() : "Undetermined";

    /// <summary>"Open → Closed", or the single value when both sides are the same.</summary>
    private static string Transition(string? from, string? to) =>
        (from, to) switch
        {
            (null, null) => "—",
            (null, _) => to!,
            (_, null) => from!,
            _ when from == to => from!,
            _ => $"{from} → {to}",
        };

    // "No change" rather than "0", and an em-dash rather than blank where there is no movement to
    // report at all. A zero and an absence look identical in a table and mean different things:
    // one says the figure held steady, the other that the item did not exist at both points.
    private static string Signed(int? delta) => delta switch
    {
        null => "—",
        0 => "No change",
        > 0 => $"+{delta}",
        _ => delta.Value.ToString(),
    };

    private static string SignedMoney(decimal? delta) => delta switch
    {
        null => "—",
        0 => "No change",
        > 0 => $"+{Money(delta.Value)}",
        _ => $"-{Money(Math.Abs(delta.Value))}",
    };
}
