using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Drafting;
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
    Guid? SnapshotId,
    Guid? GatewayId = null) : IRequest<Guid>;

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

        // Checked here rather than only in the validator: a gateway on another project would make
        // the report quote a decision about something else entirely, and no amount of shape
        // validation catches that.
        if (request.GatewayId is { } gatewayId)
        {
            var belongs = await _db.Gateways.AnyAsync(
                g => g.Id == gatewayId && g.ProjectId == request.ProjectId, cancellationToken);

            if (!belongs)
            {
                throw new KeyNotFoundException(
                    $"Gateway {gatewayId} does not exist on project {request.ProjectId}.");
            }
        }

        var actorId = _currentUser.UserId ?? Guid.Empty;

        // Read once from the registers, then handed to the one place that decides which sections
        // of which documents are figures and which are judgement. See ReportDrafts.
        var facts = await ProjectFacts.ReadAsync(_db, project, cancellationToken);
        var drafts = ReportDrafts.For(request.ReportType, facts);

        var report = new CommitteeReport
        {
            ProjectId = request.ProjectId,
            SnapshotId = request.SnapshotId,
            GatewayId = request.GatewayId,
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
