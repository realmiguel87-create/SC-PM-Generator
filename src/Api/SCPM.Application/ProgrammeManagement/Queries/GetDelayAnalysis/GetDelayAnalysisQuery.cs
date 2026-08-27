using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.ProgrammeManagement.Dtos;

namespace SCPM.Application.ProgrammeManagement.Queries.GetDelayAnalysis;

/// <summary>
/// Sets a project's programme slip against what has been recorded to account for it.
///
/// The question this answers is not "how late are we" — the timeline already says that — but "how
/// much of it does anyone understand". A programme three months late with three months accounted
/// for is a managed programme; three months late with a fortnight accounted for is a programme
/// nobody has got to the bottom of. Until now both read identically.
/// </summary>
public record GetDelayAnalysisQuery(Guid ProjectId) : IRequest<ProjectDelayAnalysisDto>;

public class GetDelayAnalysisQueryHandler : IRequestHandler<GetDelayAnalysisQuery, ProjectDelayAnalysisDto>
{
    private readonly IAppDbContext _db;

    public GetDelayAnalysisQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ProjectDelayAnalysisDto> Handle(
        GetDelayAnalysisQuery request, CancellationToken cancellationToken)
    {
        var milestones = await _db.Milestones
            .AsNoTracking()
            .Where(m => m.ProjectId == request.ProjectId)
            .ToListAsync(cancellationToken);

        var milestoneIds = milestones.Select(m => m.Id).ToList();

        var causes = await _db.MilestoneDelayCauses
            .AsNoTracking()
            .Where(c => milestoneIds.Contains(c.MilestoneId))
            .Include(c => c.ExtensionOfTime)
            .Include(c => c.CompensationEvent)
            .ToListAsync(cancellationToken);

        var causesByMilestone = causes
            .GroupBy(c => c.MilestoneId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = milestones
            .Select(m =>
            {
                var mine = causesByMilestone.GetValueOrDefault(m.Id) ?? [];
                var attributed = mine.Sum(c => c.DelayDays);

                // Only positive slip can be accounted for. A milestone running early has nothing
                // to explain, and subtracting attributions from a negative number would produce a
                // large "unattributed" figure for a milestone that is not late at all.
                var slip = Math.Max(0, m.DelayDays);

                return new MilestoneDelayAnalysisDto(
                    m.Id,
                    m.Name,
                    m.IsKeyMilestone,
                    slip,
                    attributed,
                    // Floored, because unexplained days cannot be negative: over-attribution is a
                    // different condition and is reported separately rather than as a discount on
                    // this figure.
                    UnattributedDays: Math.Max(0, slip - attributed),
                    OverAttributedDays: Math.Max(0, attributed - slip),
                    mine
                        .OrderByDescending(c => c.DelayDays)
                        .Select(c => new MilestoneDelayCauseDto(
                            c.Id,
                            c.DelayDays,
                            c.Category,
                            c.Narrative,
                            c.ExtensionOfTimeId,
                            c.CompensationEventId,
                            // Whichever link exists; both cannot, and the command refuses one that
                            // tries to set them together.
                            c.ExtensionOfTime?.Reference ?? c.CompensationEvent?.Reference))
                        .ToList());
            })
            // Worst-explained first: a reader scanning this wants the milestones nobody has
            // accounted for, not the ones in date order.
            .OrderByDescending(r => r.UnattributedDays)
            .ThenByDescending(r => r.SlipDays)
            .ToList();

        var byCategory = causes
            .GroupBy(c => c.Category)
            .Select(g => new DelayCategoryTotalDto(g.Key, g.Sum(c => c.DelayDays), g.Count()))
            .OrderByDescending(t => t.Days)
            .ToList();

        return new ProjectDelayAnalysisDto(
            rows,
            byCategory,
            rows.Sum(r => r.SlipDays),
            rows.Sum(r => r.AttributedDays),
            rows.Sum(r => r.UnattributedDays));
    }
}
