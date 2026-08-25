using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Domain.Entities;

namespace SCPM.Application.Reporting.Queries.CompareSnapshotItems;

/// <summary>
/// Item-level snapshot comparison: which risks, milestones, early warnings, compensation events,
/// variations and extensions of time changed between two snapshots, as opposed to how many
/// (CompareSnapshotsQuery).
///
/// The registers are read out of SQL Server's temporal history at each snapshot's CapturedAt
/// rather than from anything the snapshot stored. Two consequences worth knowing:
///
///   - Nothing had to be captured for this to work, and nothing grows as a result. The history is
///     a by-product of ordinary writes to already-temporal tables.
///   - Unlike the aggregate columns on Snapshot — which read 0 for any snapshot taken before they
///     were added — this works for snapshots from any point in the project's life, because the
///     history was being kept whether or not anyone was going to ask for it.
///
/// The trade is that this reflects the register as it actually was, which is not necessarily what
/// the snapshot's own aggregates say. They should agree; if they ever disagree, this is the more
/// trustworthy of the two, because it is reading the source rather than a copy of a count.
///
/// The registers covered here are exactly those the aggregate comparison counts, so the two views
/// always answer the same question at two levels of detail.
/// </summary>
public record CompareSnapshotItemsQuery(Guid FromSnapshotId, Guid ToSnapshotId)
    : IRequest<SnapshotItemComparisonDto>;

public class CompareSnapshotItemsQueryHandler
    : IRequestHandler<CompareSnapshotItemsQuery, SnapshotItemComparisonDto>
{
    private readonly IAppDbContext _db;
    private readonly IRegisterHistory _history;

    public CompareSnapshotItemsQueryHandler(IAppDbContext db, IRegisterHistory history)
    {
        _db = db;
        _history = history;
    }

    public async Task<SnapshotItemComparisonDto> Handle(
        CompareSnapshotItemsQuery request, CancellationToken cancellationToken)
    {
        var from = await _db.Snapshots.FirstOrDefaultAsync(s => s.Id == request.FromSnapshotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Snapshot {request.FromSnapshotId} not found.");
        var to = await _db.Snapshots.FirstOrDefaultAsync(s => s.Id == request.ToSnapshotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Snapshot {request.ToSnapshotId} not found.");

        if (from.ProjectId != to.ProjectId)
            throw new InvalidOperationException("Cannot compare snapshots from different projects.");

        // Ordering is not enforced. Comparing a later snapshot against an earlier one is a
        // legitimate question ("what have we undone since?"), and the deltas stay coherent either
        // way because every one is To minus From. The caller decides which way round to read it.
        var projectId = from.ProjectId;
        var at = from.CapturedAt;
        var then = to.CapturedAt;

        return new SnapshotItemComparisonDto
        {
            FromSnapshotId = from.Id,
            FromLabel = from.Label,
            FromCapturedAt = from.CapturedAt,
            ToSnapshotId = to.Id,
            ToLabel = to.Label,
            ToCapturedAt = to.CapturedAt,

            RiskChanges = DiffRisks(
                await _history.RisksAsOfAsync(projectId, at, cancellationToken),
                await _history.RisksAsOfAsync(projectId, then, cancellationToken)),

            MilestoneChanges = DiffMilestones(
                await _history.MilestonesAsOfAsync(projectId, at, cancellationToken),
                await _history.MilestonesAsOfAsync(projectId, then, cancellationToken)),

            EarlyWarningChanges = DiffEarlyWarnings(
                await _history.EarlyWarningsAsOfAsync(projectId, at, cancellationToken),
                await _history.EarlyWarningsAsOfAsync(projectId, then, cancellationToken)),

            CompensationEventChanges = DiffCompensationEvents(
                await _history.CompensationEventsAsOfAsync(projectId, at, cancellationToken),
                await _history.CompensationEventsAsOfAsync(projectId, then, cancellationToken)),

            VariationChanges = DiffVariations(
                await _history.VariationsAsOfAsync(projectId, at, cancellationToken),
                await _history.VariationsAsOfAsync(projectId, then, cancellationToken)),

            ExtensionOfTimeChanges = DiffExtensionsOfTime(
                await _history.ExtensionsOfTimeAsOfAsync(projectId, at, cancellationToken),
                await _history.ExtensionsOfTimeAsOfAsync(projectId, then, cancellationToken)),
        };
    }

    /// <summary>
    /// The shape every register diff has in common: match by id, classify as added/removed/
    /// modified, and drop anything present at both points that did not move.
    ///
    /// What differs per register is only the rule's HasMoved predicate — which fields count as a
    /// reportable change — and that is the part that needs judgement rather than mechanism. Those
    /// judgements live in RegisterChangeRules so this query and the interval-activity query
    /// cannot come to disagree about whether a given item moved.
    /// </summary>
    private static List<TChange> Diff<TEntity, TChange>(
        IReadOnlyList<TEntity> from,
        IReadOnlyList<TEntity> to,
        RegisterRule<TEntity> rule,
        Func<Guid, TEntity?, TEntity?, TChange> build)
        where TEntity : class
    {
        var fromById = from.ToDictionary(rule.Identify);
        var toById = to.ToDictionary(rule.Identify);
        var changes = new List<TChange>();

        foreach (var id in fromById.Keys.Union(toById.Keys))
        {
            fromById.TryGetValue(id, out var before);
            toById.TryGetValue(id, out var after);

            if (before is not null && after is not null && !rule.HasMoved(before, after)) continue;

            changes.Add(build(id, before, after));
        }

        return changes;
    }

    // Each register's DTO below carries whichever fields its RegisterChangeRules rule treats as a
    // movement, plus enough context to name the item. What counts as a movement is defined there,
    // once, and not restated here.

    private static ItemChangeType ChangeTypeFor(object? before, object? after) => before switch
    {
        null => ItemChangeType.Added,
        _ when after is null => ItemChangeType.Removed,
        _ => ItemChangeType.Modified,
    };

    private static List<RiskChangeDto> DiffRisks(IReadOnlyList<Risk> from, IReadOnlyList<Risk> to) =>
        Diff(from, to, RegisterChangeRules.Risks,
            (id, before, after) => new RiskChangeDto
            {
                RiskId = id,
                Title = after?.Title ?? before!.Title,
                ChangeType = ChangeTypeFor(before, after),
                FromStatus = before?.Status.ToString(),
                ToStatus = after?.Status.ToString(),
                FromProbability = before?.Probability,
                ToProbability = after?.Probability,
                FromImpact = before?.Impact,
                ToImpact = after?.Impact,
                FromScore = before?.Score,
                ToScore = after?.Score,
            })
        // Biggest movement first, then additions and removals (which have no delta), then by
        // title so the order is stable between two runs over identical data.
        .OrderByDescending(c => Math.Abs(c.ScoreDelta ?? 0))
        .ThenByDescending(c => c.ToScore ?? c.FromScore ?? 0)
        .ThenBy(c => c.Title)
        .ToList();

    private static List<MilestoneChangeDto> DiffMilestones(
        IReadOnlyList<Milestone> from, IReadOnlyList<Milestone> to) =>
        Diff(from, to, RegisterChangeRules.Milestones,
            (id, before, after) => new MilestoneChangeDto
            {
                MilestoneId = id,
                Name = after?.Name ?? before!.Name,
                ChangeType = ChangeTypeFor(before, after),
                FromStatus = before?.Status.ToString(),
                ToStatus = after?.Status.ToString(),
                FromForecastDate = before?.ForecastDate,
                ToForecastDate = after?.ForecastDate,
                FromActualDate = before?.ActualDate,
                ToActualDate = after?.ActualDate,
                FromDelayDays = before is null ? null : SnapshotMetrics.DelayDays(before),
                ToDelayDays = after is null ? null : SnapshotMetrics.DelayDays(after),
            })
        .OrderByDescending(c => Math.Abs(c.DelayDaysDelta ?? 0))
        .ThenByDescending(c => c.ToDelayDays ?? c.FromDelayDays ?? 0)
        .ThenBy(c => c.Name)
        .ToList();

    private static List<EarlyWarningChangeDto> DiffEarlyWarnings(
        IReadOnlyList<EarlyWarning> from, IReadOnlyList<EarlyWarning> to) =>
        Diff(from, to, RegisterChangeRules.EarlyWarnings,
            (id, before, after) => new EarlyWarningChangeDto
            {
                EarlyWarningId = id,
                Title = after?.Title ?? before!.Title,
                ChangeType = ChangeTypeFor(before, after),
                FromStatus = before?.Status.ToString(),
                ToStatus = after?.Status.ToString(),
            })
        .OrderBy(c => c.ChangeType)
        .ThenBy(c => c.Title)
        .ToList();

    private static List<CompensationEventChangeDto> DiffCompensationEvents(
        IReadOnlyList<CompensationEvent> from, IReadOnlyList<CompensationEvent> to) =>
        Diff(from, to, RegisterChangeRules.CompensationEvents,
            (id, before, after) => new CompensationEventChangeDto
            {
                CompensationEventId = id,
                Reference = after?.Reference ?? before!.Reference,
                Title = after?.Title ?? before!.Title,
                ChangeType = ChangeTypeFor(before, after),
                FromStatus = before?.Status.ToString(),
                ToStatus = after?.Status.ToString(),
                FromEstimatedValue = before?.EstimatedValue,
                ToEstimatedValue = after?.EstimatedValue,
            })
        .OrderByDescending(c => Math.Abs(c.EstimatedValueDelta ?? 0))
        .ThenByDescending(c => c.ToEstimatedValue ?? c.FromEstimatedValue ?? 0)
        .ThenBy(c => c.Reference)
        .ToList();

    private static List<VariationChangeDto> DiffVariations(
        IReadOnlyList<Variation> from, IReadOnlyList<Variation> to) =>
        Diff(from, to, RegisterChangeRules.Variations,
            (id, before, after) => new VariationChangeDto
            {
                VariationId = id,
                Reference = after?.Reference ?? before!.Reference,
                Description = after?.Description ?? before!.Description,
                ChangeType = ChangeTypeFor(before, after),
                FromStatus = before?.Status.ToString(),
                ToStatus = after?.Status.ToString(),
                FromValueImpact = before?.ValueImpact,
                ToValueImpact = after?.ValueImpact,
            })
        .OrderByDescending(c => Math.Abs(c.ValueImpactDelta ?? 0))
        .ThenByDescending(c => c.ToValueImpact ?? c.FromValueImpact ?? 0)
        .ThenBy(c => c.Reference)
        .ToList();

    private static List<ExtensionOfTimeChangeDto> DiffExtensionsOfTime(
        IReadOnlyList<ExtensionOfTime> from, IReadOnlyList<ExtensionOfTime> to) =>
        Diff(from, to, RegisterChangeRules.ExtensionsOfTime,
            (id, before, after) => new ExtensionOfTimeChangeDto
            {
                ExtensionOfTimeId = id,
                Reference = after?.Reference ?? before!.Reference,
                Reason = after?.Reason ?? before!.Reason,
                ChangeType = ChangeTypeFor(before, after),
                FromStatus = before?.Status.ToString(),
                ToStatus = after?.Status.ToString(),
                FromDaysClaimed = before?.DaysClaimed,
                ToDaysClaimed = after?.DaysClaimed,
                FromDaysAwarded = before?.DaysAwarded,
                ToDaysAwarded = after?.DaysAwarded,
            })
        // Awarded days first — an award is a programme fact, a claim is not yet one.
        .OrderByDescending(c => Math.Abs(c.DaysAwardedDelta ?? 0))
        .ThenByDescending(c => c.ToDaysAwarded ?? 0)
        .ThenBy(c => c.Reference)
        .ToList();
}
