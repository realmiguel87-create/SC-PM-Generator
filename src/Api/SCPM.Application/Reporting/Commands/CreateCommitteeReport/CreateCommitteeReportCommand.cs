using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Common;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.Reporting.Commands.CreateCommitteeReport;

/// <summary>
/// Creates a report and drafts the sections that can be drafted from data the platform already
/// holds, so an author starts from a populated draft rather than a blank page.
///
/// What gets auto-drafted is chosen carefully. Figures — budget against forecast, how many
/// milestones are behind, how many risks are open — are facts the platform already knows, and
/// retyping them into a document is how a report comes to disagree with the register it reports
/// on. Judgement — an executive summary, which issues matter this month, what should happen next —
/// is never drafted: generated prose there would produce something nobody should trust unread, and
/// which reads plausibly enough that nobody would.
/// </summary>
public record CreateCommitteeReportCommand(
    Guid ProjectId,
    CommitteeReportType ReportType,
    string Title,
    DateOnly? MeetingDate,
    DateOnly? ReportDate,
    Guid? SnapshotId) : IRequest<Guid>;

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

        var openRiskCount = await _db.Risks.CountAsync(
            r => r.ProjectId == request.ProjectId && r.Status == RiskStatus.Open, cancellationToken);

        var highestRiskScore = await _db.Risks
            .Where(r => r.ProjectId == request.ProjectId && r.Status == RiskStatus.Open)
            .Select(r => (int?)(r.Probability * r.Impact))
            .OrderByDescending(s => s)
            .FirstOrDefaultAsync(cancellationToken);

        var outstandingMilestones = await _db.Milestones
            .Where(m => m.ProjectId == request.ProjectId && m.Status != MilestoneStatus.Complete)
            .ToListAsync(cancellationToken);
        var behindBaseline = outstandingMilestones.Count(m => m.DelayDays > 0);

        var stakeholderCount = await _db.Stakeholders.CountAsync(
            s => s.ProjectId == request.ProjectId, cancellationToken);

        var variance = project.ForecastCost - project.ApprovedBudget;
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var finance =
            $"Approved budget: £{project.ApprovedBudget:N0}. Current forecast: £{project.ForecastCost:N0}. " +
            $"Variance: £{Math.Abs(variance):N0} ({(variance > 0 ? "over" : variance < 0 ? "under" : "on")} budget).";

        var programme = outstandingMilestones.Count == 0
            ? "No outstanding milestones recorded."
            : $"{outstandingMilestones.Count} milestone(s) outstanding, of which {behindBaseline} are behind baseline.";

        var risk = openRiskCount == 0
            ? "No open risks recorded."
            : $"{openRiskCount} open risk(s) on the register, highest current score {highestRiskScore ?? 0}/25.";

        // Keyed by section rather than assigned to named columns, so a report type without a
        // finance section simply never asks for this text rather than storing it invisibly.
        var drafts = request.ReportType == CommitteeReportType.StatusReport
            ? new Dictionary<string, string>
            {
                // Only the two sections that are wholly figures. Key activities, planned
                // activities and issues are the author's account of the period and are left empty
                // deliberately — a plausible-looking generated paragraph there is worse than an
                // obviously empty heading.
                ["schedule-update"] = programme,
                ["cost-position"] = finance,
            }
            : new Dictionary<string, string>
            {
                ["executive-summary"] =
                    $"This report provides an update on {project.Name} ({project.ProjectRef}), " +
                    $"currently at RIBA Stage {project.CurrentRibaStage}.",
                ["finance-commentary"] = finance,
                ["programme-commentary"] = programme,
                ["risk-commentary"] = risk,
                ["stakeholder-commentary"] = $"{stakeholderCount} stakeholder(s) recorded on the register.",
            };

        var report = new CommitteeReport
        {
            ProjectId = request.ProjectId,
            SnapshotId = request.SnapshotId,
            ReportType = request.ReportType,
            Title = request.Title,
            MeetingDate = request.MeetingDate,
            ReportDate = request.ReportDate,
            CreatedBy = actorId,
            Sections = ReportSections.For(request.ReportType)
                .Where(s => drafts.ContainsKey(s.Key))
                .Select(s => new CommitteeReportSectionContent
                {
                    SectionKey = s.Key,
                    Content = drafts[s.Key],
                    CreatedBy = actorId,
                })
                .ToList(),
        };

        _db.CommitteeReports.Add(report);
        await _db.SaveChangesAsync(cancellationToken);

        return report.Id;
    }
}
