using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.ProgrammeManagement.Dtos;

namespace SCPM.Application.ProgrammeManagement.Queries.GetProgrammeAgainstBaseline;

/// <summary>
/// Measures where the programme now sits against a chosen baseline.
///
/// This is the question a rebaseline otherwise destroys the ability to ask. Once
/// <c>Milestone.BaselineDate</c> has been moved to the new programme, every slip figure in the app
/// measures against the new one, and "how far are we from the programme sanctioned in March?"
/// stops having an answer — even though nothing was deleted. Passing a baseline id gives it back.
/// </summary>
/// <param name="BaselineId">The baseline to measure against; the current one when null.</param>
public record GetProgrammeAgainstBaselineQuery(Guid ProjectId, Guid? BaselineId)
    : IRequest<ProgrammeAgainstBaselineDto?>;

public class GetProgrammeAgainstBaselineQueryHandler
    : IRequestHandler<GetProgrammeAgainstBaselineQuery, ProgrammeAgainstBaselineDto?>
{
    private readonly IAppDbContext _db;

    public GetProgrammeAgainstBaselineQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ProgrammeAgainstBaselineDto?> Handle(
        GetProgrammeAgainstBaselineQuery request, CancellationToken cancellationToken)
    {
        var baseline = await _db.ProgrammeBaselines
            .AsNoTracking()
            .Include(b => b.Entries)
            .Where(b => b.ProjectId == request.ProjectId)
            .Where(b => request.BaselineId == null
                ? b.IsCurrent
                : b.Id == request.BaselineId)
            .FirstOrDefaultAsync(cancellationToken);

        // Null rather than an empty comparison. A project that has never been rebaselined has no
        // baseline record at all, and returning an empty result would present "no baseline exists"
        // and "nothing has slipped" as the same answer.
        if (baseline is null) return null;

        var milestones = await _db.Milestones
            .AsNoTracking()
            .Where(m => m.ProjectId == request.ProjectId)
            .ToListAsync(cancellationToken);

        var entriesByMilestone = baseline.Entries.ToDictionary(e => e.MilestoneId);

        var rows = milestones
            .Select(m =>
            {
                // Same date precedence as Milestone.DelayDays and the timeline: an actual, once it
                // exists, supersedes a forecast that has been overtaken by events.
                var currentDate = m.ActualDate ?? m.ForecastDate;
                var entry = entriesByMilestone.GetValueOrDefault(m.Id);

                return new MilestoneAgainstBaselineDto(
                    m.Id,
                    m.Name,
                    // Falls back to the live name for a milestone the baseline never held: there
                    // is no historical name to report, and leaving it blank would read as one
                    // that had been erased.
                    entry?.MilestoneName ?? m.Name,
                    entry?.BaselineDate,
                    currentDate,
                    m.ActualDate.HasValue,
                    // A milestone added after the baseline was sanctioned carries no slip. It is
                    // not early and it is not late; it was not in the programme being measured,
                    // and scoring it against a date nobody set would be inventing a figure.
                    entry is null ? 0 : currentDate.DayNumber - entry.BaselineDate.DayNumber,
                    m.IsKeyMilestone,
                    AddedSinceBaseline: entry is null);
            })
            .OrderBy(r => r.BaselineDate ?? r.CurrentDate)
            .ToList();

        var worst = rows
            .Where(r => !r.AddedSinceBaseline && r.SlipDays > 0)
            .OrderByDescending(r => r.SlipDays)
            .FirstOrDefault();

        var liveMilestoneIds = milestones.Select(m => m.Id).ToHashSet();
        var removed = baseline.Entries
            .Where(e => !liveMilestoneIds.Contains(e.MilestoneId))
            .OrderBy(e => e.BaselineDate)
            .Select(e => e.MilestoneName)
            .ToList();

        return new ProgrammeAgainstBaselineDto(
            new ProgrammeBaselineDto(
                baseline.Id,
                baseline.Revision,
                baseline.Name,
                baseline.Reason,
                baseline.ApprovedBy,
                baseline.ApprovedDate,
                baseline.IsCurrent,
                baseline.CreatedDate,
                baseline.Entries.Count),
            rows,
            worst?.SlipDays ?? 0,
            worst?.Name,
            removed);
    }
}
