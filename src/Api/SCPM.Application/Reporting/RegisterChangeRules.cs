using SCPM.Domain.Entities;

namespace SCPM.Application.Reporting;

/// <summary>
/// What counts as a reportable change, per register — identity, the fields that constitute a
/// movement, and how to name the item in a report.
/// </summary>
public sealed record RegisterRule<T>(
    string Register,
    Func<T, Guid> Identify,
    Func<T, T, bool> HasMoved,
    Func<T, string> Describe);

/// <summary>
/// The one definition of "this item moved" for every register that gets diffed.
///
/// These were previously inline in CompareSnapshotItemsQuery, which was fine while one query used
/// them. It stopped being fine the moment a second query needed the same judgements: two
/// definitions of what counts as a change, in two files, is how the endpoint comparison and the
/// interval-activity query end up disagreeing about whether a compensation event moved — with no
/// way for a reader to tell which is right. Same reasoning as SnapshotMetrics for the aggregates.
///
/// Every predicate below is a decision with a rationale, not a default. The rationale is recorded
/// beside it, because the wrong choice here still produces plausible-looking output.
/// </summary>
public static class RegisterChangeRules
{
    /// <summary>
    /// Score and status. A title-only edit is deliberately not a movement: renaming a risk is
    /// housekeeping, and listing it beside real movements dilutes the ones that matter.
    /// </summary>
    public static readonly RegisterRule<Risk> Risks = new(
        "Risk",
        r => r.Id,
        (before, after) =>
            before.Status != after.Status ||
            before.Probability != after.Probability ||
            before.Impact != after.Impact,
        r => r.Title);

    /// <summary>
    /// Dates and status. Baseline changes are excluded on purpose — re-baselining is a governance
    /// event in its own right, and folding it in would report a slip the dates no longer show, or
    /// hide one that they do.
    /// </summary>
    public static readonly RegisterRule<Milestone> Milestones = new(
        "Milestone",
        m => m.Id,
        (before, after) =>
            before.Status != after.Status ||
            before.ForecastDate != after.ForecastDate ||
            before.ActualDate != after.ActualDate,
        m => m.Name);

    /// <summary>
    /// Status only. Mitigation-action text changes every time the team works the problem, and
    /// reporting every wording change would drown the two transitions — raised, closed — that
    /// actually matter.
    /// </summary>
    public static readonly RegisterRule<EarlyWarning> EarlyWarnings = new(
        "Early warning",
        e => e.Id,
        (before, after) => before.Status != after.Status,
        e => e.Title);

    /// <summary>
    /// Status and estimated value. Either alone tells half the story: a compensation event can be
    /// accepted without its value moving, or re-estimated without changing status.
    /// </summary>
    public static readonly RegisterRule<CompensationEvent> CompensationEvents = new(
        "Compensation event",
        c => c.Id,
        (before, after) =>
            before.Status != after.Status ||
            before.EstimatedValue != after.EstimatedValue,
        c => $"{c.Reference} — {c.Title}");

    /// <summary>Status and value impact, for the same reason as compensation events.</summary>
    public static readonly RegisterRule<Variation> Variations = new(
        "Variation",
        v => v.Id,
        (before, after) =>
            before.Status != after.Status ||
            before.ValueImpact != after.ValueImpact,
        v => $"{v.Reference} — {v.Description}");

    /// <summary>
    /// Status, claimed days and awarded days. Claimed and awarded are separate fields and stay
    /// separate: a claim rising is the contractor's position, an award rising is the programme
    /// actually moving, and treating them as one figure would conflate an argument with a fact.
    /// </summary>
    public static readonly RegisterRule<ExtensionOfTime> ExtensionsOfTime = new(
        "Extension of time",
        x => x.Id,
        (before, after) =>
            before.Status != after.Status ||
            before.DaysClaimed != after.DaysClaimed ||
            before.DaysAwarded != after.DaysAwarded,
        x => $"{x.Reference} — {x.Reason}");
}
