using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;

namespace SCPM.Application.Reporting.Queries.CompareSnapshots;

/// <summary>
/// The Snapshot Comparison Engine (docs/roadmap.md Phase 6). Compares the fields Snapshot
/// actually captures — RIBA stage, budget, forecast. Comparing risk/programme/NEC4/SBCC
/// registers between two points in time would need those registers to be captured into the
/// snapshot too, which they aren't yet (Snapshot only stores project-header figures — see
/// docs/roadmap.md Phase 2) — that's the natural next extension, not a gap in this query.
/// </summary>
public record CompareSnapshotsQuery(Guid FromSnapshotId, Guid ToSnapshotId) : IRequest<SnapshotComparisonDto>;

public class CompareSnapshotsQueryHandler : IRequestHandler<CompareSnapshotsQuery, SnapshotComparisonDto>
{
    private readonly IAppDbContext _db;

    public CompareSnapshotsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<SnapshotComparisonDto> Handle(CompareSnapshotsQuery request, CancellationToken cancellationToken)
    {
        var from = await _db.Snapshots.FirstOrDefaultAsync(s => s.Id == request.FromSnapshotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Snapshot {request.FromSnapshotId} not found.");
        var to = await _db.Snapshots.FirstOrDefaultAsync(s => s.Id == request.ToSnapshotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Snapshot {request.ToSnapshotId} not found.");

        if (from.ProjectId != to.ProjectId)
            throw new InvalidOperationException("Cannot compare snapshots from different projects.");

        return new SnapshotComparisonDto
        {
            FromSnapshotId = from.Id,
            FromLabel = from.Label,
            FromCapturedAt = from.CapturedAt,
            ToSnapshotId = to.Id,
            ToLabel = to.Label,
            ToCapturedAt = to.CapturedAt,
            FromRibaStage = from.RibaStageAtCapture,
            ToRibaStage = to.RibaStageAtCapture,
            FromApprovedBudget = from.ApprovedBudgetAtCapture,
            ToApprovedBudget = to.ApprovedBudgetAtCapture,
            FromForecastCost = from.ForecastCostAtCapture,
            ToForecastCost = to.ForecastCostAtCapture
        };
    }
}
