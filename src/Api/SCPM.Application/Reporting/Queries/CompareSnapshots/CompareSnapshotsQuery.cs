using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;

namespace SCPM.Application.Reporting.Queries.CompareSnapshots;

/// <summary>
/// The Snapshot Comparison Engine (docs/roadmap.md Phase 6). Compares two snapshots of the same
/// project across everything a Snapshot captures: the project header (RIBA stage, budget,
/// forecast) and the register aggregates (risk, issues, programme, NEC4, SBCC).
///
/// Comparison is a pure read of two already-captured rows and never recomputes anything from
/// today's registers. That is what makes it a comparison of two points in time rather than of
/// one point against now — and it is why a metric that did not exist when a snapshot was taken
/// cannot be back-filled into it. Snapshots captured before the register aggregates were added
/// read 0 for them, which is the honest answer rather than a defect in this query.
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
            ToForecastCost = to.ForecastCostAtCapture,

            FromOpenRiskCount = from.OpenRiskCount,
            ToOpenRiskCount = to.OpenRiskCount,
            FromHighRiskCount = from.HighRiskCount,
            ToHighRiskCount = to.HighRiskCount,
            FromTotalOpenRiskScore = from.TotalOpenRiskScore,
            ToTotalOpenRiskScore = to.TotalOpenRiskScore,

            FromOpenIssueCount = from.OpenIssueCount,
            ToOpenIssueCount = to.OpenIssueCount,
            FromSevereOpenIssueCount = from.SevereOpenIssueCount,
            ToSevereOpenIssueCount = to.SevereOpenIssueCount,

            FromMilestoneCount = from.MilestoneCount,
            ToMilestoneCount = to.MilestoneCount,
            FromMilestonesCompleteCount = from.MilestonesCompleteCount,
            ToMilestonesCompleteCount = to.MilestonesCompleteCount,
            FromMilestonesDelayedCount = from.MilestonesDelayedCount,
            ToMilestonesDelayedCount = to.MilestonesDelayedCount,
            FromWorstMilestoneDelayDays = from.WorstMilestoneDelayDays,
            ToWorstMilestoneDelayDays = to.WorstMilestoneDelayDays,

            FromOpenEarlyWarningCount = from.OpenEarlyWarningCount,
            ToOpenEarlyWarningCount = to.OpenEarlyWarningCount,
            FromOpenCompensationEventCount = from.OpenCompensationEventCount,
            ToOpenCompensationEventCount = to.OpenCompensationEventCount,
            FromCompensationEventValue = from.CompensationEventValue,
            ToCompensationEventValue = to.CompensationEventValue,

            FromOpenVariationCount = from.OpenVariationCount,
            ToOpenVariationCount = to.OpenVariationCount,
            FromVariationValue = from.VariationValue,
            ToVariationValue = to.VariationValue,
            FromExtensionOfTimeDaysAwarded = from.ExtensionOfTimeDaysAwarded,
            ToExtensionOfTimeDaysAwarded = to.ExtensionOfTimeDaysAwarded
        };
    }
}
