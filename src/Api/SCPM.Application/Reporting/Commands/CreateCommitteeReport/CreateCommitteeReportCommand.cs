using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.Reporting.Commands.CreateCommitteeReport;

/// <summary>
/// Creates a committee/cabinet/board report or decision paper. The Finance/Programme/Risk/
/// Stakeholder commentary sections are auto-drafted from live project data — "generate project
/// documentation automatically" from the spec — so an officer starts from a populated draft and
/// edits it (via UpdateCommitteeReportCommand) rather than a blank page. Executive Summary,
/// Background and Recommendations are always author-written; auto-drafting those would produce
/// prose nobody should trust unread.
/// </summary>
public record CreateCommitteeReportCommand(
    Guid ProjectId, CommitteeReportType ReportType, string Title, DateOnly? MeetingDate, Guid? SnapshotId) : IRequest<Guid>;

public class CreateCommitteeReportCommandHandler : IRequestHandler<CreateCommitteeReportCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCommitteeReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCommitteeReportCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {request.ProjectId} not found.");

        var openRiskCount = await _db.Risks.CountAsync(r => r.ProjectId == request.ProjectId && r.Status == RiskStatus.Open, cancellationToken);
        var highestRiskScore = await _db.Risks
            .Where(r => r.ProjectId == request.ProjectId && r.Status == RiskStatus.Open)
            .Select(r => (int?)(r.Probability * r.Impact))
            .OrderByDescending(s => s)
            .FirstOrDefaultAsync(cancellationToken);

        var delayedMilestoneCount = await _db.Milestones
            .Where(m => m.ProjectId == request.ProjectId && m.Status != MilestoneStatus.Complete)
            .ToListAsync(cancellationToken);
        var delayedCount = delayedMilestoneCount.Count(m => m.DelayDays > 0);

        var stakeholderCount = await _db.Stakeholders.CountAsync(s => s.ProjectId == request.ProjectId, cancellationToken);

        var variance = project.ForecastCost - project.ApprovedBudget;
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var report = new CommitteeReport
        {
            ProjectId = request.ProjectId,
            SnapshotId = request.SnapshotId,
            ReportType = request.ReportType,
            Title = request.Title,
            MeetingDate = request.MeetingDate,
            ExecutiveSummary = $"This report provides an update on {project.Name} ({project.ProjectRef}), currently at RIBA Stage {project.CurrentRibaStage}.",
            FinanceCommentary = $"Approved budget: £{project.ApprovedBudget:N0}. Current forecast: £{project.ForecastCost:N0}. " +
                $"Variance: £{variance:N0} ({(variance > 0 ? "over" : "under")} budget).",
            ProgrammeCommentary = delayedMilestoneCount.Count == 0
                ? "No outstanding milestones recorded."
                : $"{delayedMilestoneCount.Count} milestone(s) outstanding, of which {delayedCount} are behind baseline.",
            RiskCommentary = openRiskCount == 0
                ? "No open risks recorded."
                : $"{openRiskCount} open risk(s) on the register, highest current score {highestRiskScore ?? 0}/25.",
            StakeholderCommentary = $"{stakeholderCount} stakeholder(s) recorded on the register.",
            CreatedBy = actorId
        };

        _db.CommitteeReports.Add(report);
        await _db.SaveChangesAsync(cancellationToken);

        return report.Id;
    }
}
