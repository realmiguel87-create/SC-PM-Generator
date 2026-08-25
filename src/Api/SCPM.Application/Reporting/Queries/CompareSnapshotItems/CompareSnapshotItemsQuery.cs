using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Domain.Entities;

namespace SCPM.Application.Reporting.Queries.CompareSnapshotItems;

/// <summary>
/// Item-level snapshot comparison: which risks and milestones changed between two snapshots, as
/// opposed to how many (CompareSnapshotsQuery).
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
/// the snapshot's own aggregates say. They should agree; if they ever disagree, this is the
/// more trustworthy of the two, because it is reading the source rather than a copy of a count.
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
        var risksFrom = await _history.RisksAsOfAsync(from.ProjectId, from.CapturedAt, cancellationToken);
        var risksTo = await _history.RisksAsOfAsync(to.ProjectId, to.CapturedAt, cancellationToken);

        var milestonesFrom = await _history.MilestonesAsOfAsync(from.ProjectId, from.CapturedAt, cancellationToken);
        var milestonesTo = await _history.MilestonesAsOfAsync(to.ProjectId, to.CapturedAt, cancellationToken);

        return new SnapshotItemComparisonDto
        {
            FromSnapshotId = from.Id,
            FromLabel = from.Label,
            FromCapturedAt = from.CapturedAt,
            ToSnapshotId = to.Id,
            ToLabel = to.Label,
            ToCapturedAt = to.CapturedAt,
            RiskChanges = DiffRisks(risksFrom, risksTo),
            MilestoneChanges = DiffMilestones(milestonesFrom, milestonesTo),
        };
    }

    private static List<RiskChangeDto> DiffRisks(
        IReadOnlyList<Risk> from, IReadOnlyList<Risk> to)
    {
        var fromById = from.ToDictionary(r => r.Id);
        var toById = to.ToDictionary(r => r.Id);
        var changes = new List<RiskChangeDto>();

        foreach (var id in fromById.Keys.Union(toById.Keys))
        {
            fromById.TryGetValue(id, out var before);
            toById.TryGetValue(id, out var after);

            // Same field set the comparison reports on. A title-only edit is deliberately not a
            // change worth reporting: renaming a risk is housekeeping, and listing it alongside
            // real movements dilutes the ones that matter.
            var moved = before is not null && after is not null &&
                (before.Status != after.Status ||
                 before.Probability != after.Probability ||
                 before.Impact != after.Impact);

            if (before is not null && after is not null && !moved) continue;

            changes.Add(new RiskChangeDto
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
            });
        }

        // Biggest movement first, then additions and removals (which have no delta), then by
        // title so the order is stable between two runs over identical data.
        return changes
            .OrderByDescending(c => Math.Abs(c.ScoreDelta ?? 0))
            .ThenByDescending(c => c.ToScore ?? c.FromScore ?? 0)
            .ThenBy(c => c.Title)
            .ToList();
    }

    private static List<MilestoneChangeDto> DiffMilestones(
        IReadOnlyList<Milestone> from, IReadOnlyList<Milestone> to)
    {
        var fromById = from.ToDictionary(m => m.Id);
        var toById = to.ToDictionary(m => m.Id);
        var changes = new List<MilestoneChangeDto>();

        foreach (var id in fromById.Keys.Union(toById.Keys))
        {
            fromById.TryGetValue(id, out var before);
            toById.TryGetValue(id, out var after);

            // Baseline changes are not tracked here on purpose: re-baselining is a governance
            // event in its own right, and folding it into "this milestone slipped" would report a
            // slip that the dates no longer show — or hide one that they do.
            var moved = before is not null && after is not null &&
                (before.Status != after.Status ||
                 before.ForecastDate != after.ForecastDate ||
                 before.ActualDate != after.ActualDate);

            if (before is not null && after is not null && !moved) continue;

            changes.Add(new MilestoneChangeDto
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
            });
        }

        return changes
            .OrderByDescending(c => Math.Abs(c.DelayDaysDelta ?? 0))
            .ThenByDescending(c => c.ToDelayDays ?? c.FromDelayDays ?? 0)
            .ThenBy(c => c.Name)
            .ToList();
    }

    private static ItemChangeType ChangeTypeFor(object? before, object? after) => before switch
    {
        null => ItemChangeType.Added,
        _ when after is null => ItemChangeType.Removed,
        _ => ItemChangeType.Modified,
    };
}
