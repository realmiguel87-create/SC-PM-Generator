using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;

namespace SCPM.Application.Reporting.Queries.GetSnapshots;

public record GetSnapshotsQuery(Guid ProjectId) : IRequest<List<SnapshotDto>>;

public class GetSnapshotsQueryHandler : IRequestHandler<GetSnapshotsQuery, List<SnapshotDto>>
{
    private readonly IAppDbContext _db;

    public GetSnapshotsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<SnapshotDto>> Handle(GetSnapshotsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Snapshots
            .Where(s => s.ProjectId == request.ProjectId)
            .OrderByDescending(s => s.CapturedAt)
            .Select(s => new SnapshotDto
            {
                Id = s.Id,
                Type = s.Type.ToString(),
                Label = s.Label,
                CapturedAt = s.CapturedAt,
                RibaStageAtCapture = s.RibaStageAtCapture,
                ApprovedBudgetAtCapture = s.ApprovedBudgetAtCapture,
                ForecastCostAtCapture = s.ForecastCostAtCapture,

                OpenRiskCount = s.OpenRiskCount,
                HighRiskCount = s.HighRiskCount,
                TotalOpenRiskScore = s.TotalOpenRiskScore,

                OpenIssueCount = s.OpenIssueCount,
                SevereOpenIssueCount = s.SevereOpenIssueCount,

                MilestoneCount = s.MilestoneCount,
                MilestonesCompleteCount = s.MilestonesCompleteCount,
                MilestonesDelayedCount = s.MilestonesDelayedCount,
                WorstMilestoneDelayDays = s.WorstMilestoneDelayDays,

                OpenEarlyWarningCount = s.OpenEarlyWarningCount,
                OpenCompensationEventCount = s.OpenCompensationEventCount,
                CompensationEventValue = s.CompensationEventValue,

                OpenVariationCount = s.OpenVariationCount,
                VariationValue = s.VariationValue,
                ExtensionOfTimeDaysAwarded = s.ExtensionOfTimeDaysAwarded
            })
            .ToListAsync(cancellationToken);
    }
}
