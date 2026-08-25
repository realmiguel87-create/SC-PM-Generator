using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;

namespace SCPM.Application.Reporting.Queries.GetSnapshotIntervalActivity;

/// <summary>
/// Register activity that occurred between two snapshots and left no trace at either endpoint.
///
/// CompareSnapshotItemsQuery reads each register at two instants and diffs them, which is the
/// right way to answer "what is different now" and structurally blind to anything that both
/// began and ended inside the window. This closes that blind spot using `FOR SYSTEM_TIME BETWEEN`
/// instead of two `AS OF` reads.
///
/// Two kinds of thing are invisible to an endpoint comparison, and both are found here:
///
///   - Raised and removed: created after the earlier snapshot and gone by the later one. Absent
///     from both endpoints.
///   - Changed and reverted: identical at both endpoints, but not identical throughout. The
///     comparison correctly reports no change; the activity still happened.
///
/// What counts as "changed" is RegisterChangeRules — the same definition the comparison uses, so
/// the two queries cannot come to disagree about whether an item moved.
/// </summary>
public record GetSnapshotIntervalActivityQuery(Guid FromSnapshotId, Guid ToSnapshotId)
    : IRequest<SnapshotIntervalActivityDto>;

public class GetSnapshotIntervalActivityQueryHandler
    : IRequestHandler<GetSnapshotIntervalActivityQuery, SnapshotIntervalActivityDto>
{
    private readonly IAppDbContext _db;
    private readonly IRegisterHistory _history;

    public GetSnapshotIntervalActivityQueryHandler(IAppDbContext db, IRegisterHistory history)
    {
        _db = db;
        _history = history;
    }

    public async Task<SnapshotIntervalActivityDto> Handle(
        GetSnapshotIntervalActivityQuery request, CancellationToken cancellationToken)
    {
        var from = await _db.Snapshots.FirstOrDefaultAsync(s => s.Id == request.FromSnapshotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Snapshot {request.FromSnapshotId} not found.");
        var to = await _db.Snapshots.FirstOrDefaultAsync(s => s.Id == request.ToSnapshotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Snapshot {request.ToSnapshotId} not found.");

        if (from.ProjectId != to.ProjectId)
            throw new InvalidOperationException("Cannot compare snapshots from different projects.");

        var projectId = from.ProjectId;
        var at = from.CapturedAt;
        var then = to.CapturedAt;

        var items = new List<IntervalActivityItemDto>();

        items.AddRange(Find(RegisterChangeRules.Risks,
            await _history.RisksAsOfAsync(projectId, at, cancellationToken),
            await _history.RisksAsOfAsync(projectId, then, cancellationToken),
            await _history.RiskVersionsBetweenAsync(projectId, at, then, cancellationToken)));

        items.AddRange(Find(RegisterChangeRules.Milestones,
            await _history.MilestonesAsOfAsync(projectId, at, cancellationToken),
            await _history.MilestonesAsOfAsync(projectId, then, cancellationToken),
            await _history.MilestoneVersionsBetweenAsync(projectId, at, then, cancellationToken)));

        items.AddRange(Find(RegisterChangeRules.EarlyWarnings,
            await _history.EarlyWarningsAsOfAsync(projectId, at, cancellationToken),
            await _history.EarlyWarningsAsOfAsync(projectId, then, cancellationToken),
            await _history.EarlyWarningVersionsBetweenAsync(projectId, at, then, cancellationToken)));

        items.AddRange(Find(RegisterChangeRules.CompensationEvents,
            await _history.CompensationEventsAsOfAsync(projectId, at, cancellationToken),
            await _history.CompensationEventsAsOfAsync(projectId, then, cancellationToken),
            await _history.CompensationEventVersionsBetweenAsync(projectId, at, then, cancellationToken)));

        items.AddRange(Find(RegisterChangeRules.Variations,
            await _history.VariationsAsOfAsync(projectId, at, cancellationToken),
            await _history.VariationsAsOfAsync(projectId, then, cancellationToken),
            await _history.VariationVersionsBetweenAsync(projectId, at, then, cancellationToken)));

        items.AddRange(Find(RegisterChangeRules.ExtensionsOfTime,
            await _history.ExtensionsOfTimeAsOfAsync(projectId, at, cancellationToken),
            await _history.ExtensionsOfTimeAsOfAsync(projectId, then, cancellationToken),
            await _history.ExtensionOfTimeVersionsBetweenAsync(projectId, at, then, cancellationToken)));

        return new SnapshotIntervalActivityDto
        {
            FromSnapshotId = from.Id,
            FromLabel = from.Label,
            FromCapturedAt = from.CapturedAt,
            ToSnapshotId = to.Id,
            ToLabel = to.Label,
            ToCapturedAt = to.CapturedAt,
            // Most churn first, then a stable secondary order so two runs over identical data
            // produce identical output.
            Items = [.. items
                .OrderByDescending(i => i.VersionCount)
                .ThenBy(i => i.Register)
                .ThenBy(i => i.Name)],
        };
    }

    /// <summary>
    /// Compares each item's window versions against its state at the two endpoints, and keeps only
    /// the items an endpoint comparison could not have reported.
    /// </summary>
    private static IEnumerable<IntervalActivityItemDto> Find<T>(
        RegisterRule<T> rule,
        IReadOnlyList<T> atStart,
        IReadOnlyList<T> atEnd,
        IReadOnlyList<T> versionsInWindow) where T : class
    {
        var startById = atStart.ToDictionary(rule.Identify);
        var endById = atEnd.ToDictionary(rule.Identify);

        foreach (var group in versionsInWindow.GroupBy(rule.Identify))
        {
            var id = group.Key;
            var versions = group.ToList();

            var presentAtStart = startById.TryGetValue(id, out var start);
            var presentAtEnd = endById.TryGetValue(id, out var end);

            if (!presentAtStart && !presentAtEnd)
            {
                yield return new IntervalActivityItemDto
                {
                    Register = rule.Register,
                    ItemId = id,
                    // Last version in the window, because a removed item has no current row and
                    // its final state is the most informative thing left of it.
                    Name = rule.Describe(versions[^1]),
                    ActivityType = IntervalActivityType.RaisedAndRemoved,
                    VersionCount = versions.Count,
                };
                continue;
            }

            // Anything the endpoint comparison would already report is skipped here rather than
            // duplicated: this list exists to add what that one cannot see, and repeating its
            // findings would make it look longer and mean less.
            if (!presentAtStart || !presentAtEnd) continue;
            if (rule.HasMoved(start!, end!)) continue;

            if (versions.Any(version => rule.HasMoved(start!, version)))
            {
                yield return new IntervalActivityItemDto
                {
                    Register = rule.Register,
                    ItemId = id,
                    Name = rule.Describe(end!),
                    ActivityType = IntervalActivityType.ChangedAndReverted,
                    VersionCount = versions.Count,
                };
            }
        }
    }
}
