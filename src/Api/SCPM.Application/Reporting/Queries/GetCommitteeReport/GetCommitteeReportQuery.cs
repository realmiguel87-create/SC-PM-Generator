using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Application.Reporting.Queries.GetCommitteeReport;

public record GetCommitteeReportQuery(Guid CommitteeReportId) : IRequest<CommitteeReportDto?>;

public class GetCommitteeReportQueryHandler : IRequestHandler<GetCommitteeReportQuery, CommitteeReportDto?>
{
    private readonly IAppDbContext _db;

    public GetCommitteeReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CommitteeReportDto?> Handle(
        GetCommitteeReportQuery request, CancellationToken cancellationToken)
    {
        var report = await _db.CommitteeReports
            .AsNoTracking()
            .Include(r => r.Project)
            .Include(r => r.Sections)
            .Include(r => r.Gateway)
            .FirstOrDefaultAsync(r => r.Id == request.CommitteeReportId, cancellationToken);

        if (report is null) return null;

        (string Stage, List<ReportPanelFieldDto> Panel)? gate = report.Gateway is null
            ? null
            : await ReadGateAsync(report.Gateway, cancellationToken);

        // Sponsor and manager are held on the project as user ids, and resolved to names here.
        // Note this reports whoever holds the role *now*: a report reopened next year will name
        // the current post-holder rather than the person who held it when the report was written.
        // Nothing records the latter, and printing a raw identifier would be worse. Flagged in the
        // roadmap rather than papered over.
        var userIds = new[] { report.Project.SponsorUserId, report.Project.ProjectManagerUserId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        var names = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var content = report.Sections.ToDictionary(s => s.SectionKey, s => s.Content);

        string? Lookup(Guid? id) => id.HasValue ? names.GetValueOrDefault(id.Value) : null;

        return new CommitteeReportDto
        {
            Id = report.Id,
            ProjectId = report.ProjectId,
            ProjectName = report.Project.Name,
            ProjectRef = report.Project.ProjectRef,
            ReportType = report.ReportType.ToString(),
            Title = report.Title,
            MeetingDate = report.MeetingDate,
            ReportDate = report.ReportDate,
            Status = report.Status.ToString(),
            CreatedDate = report.CreatedDate,
            SponsorName = Lookup(report.Project.SponsorUserId),
            ProjectManagerName = Lookup(report.Project.ProjectManagerUserId),
            ApprovedBudget = report.Project.ApprovedBudget,

            RibaStage = gate?.Stage,
            ClosingPanel = gate?.Panel ?? [],

            // Driven by the report type's definition, not by what happens to be stored. An author
            // needs to see the headings they have not written yet; listing only the stored rows
            // would hide precisely the sections that still need work.
            Sections = ReportSections.For(report.ReportType)
                .Select(s => new ReportSectionDto
                {
                    Key = s.Key,
                    Heading = s.Heading,
                    Content = content.GetValueOrDefault(s.Key),
                    Prompt = s.Prompt,
                })
                .ToList(),
        };
    }

    /// <summary>
    /// The gate's stage and its decision, read from the register.
    ///
    /// This is the point of linking a report to a gateway at all. Everything here could have been
    /// typed into the document instead, and then a report could say "Approved" while
    /// <see cref="Domain.Entities.Approval.Decision"/> said otherwise — a governance file that
    /// contradicts its own register, with nothing to say which is right.
    /// </summary>
    private async Task<(string Stage, List<ReportPanelFieldDto> Panel)> ReadGateAsync(
        Domain.Entities.Gateway gateway, CancellationToken cancellationToken)
    {
        var instance = await _db.RibaStageInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == gateway.RibaStageInstanceId, cancellationToken);

        var stage = string.Empty;

        if (instance is not null)
        {
            var definition = await _db.RibaStageDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.StageNumber == instance.StageNumber, cancellationToken);

            // "Stage 2 — Concept Design", matching how the five Stage 0–1 documents name theirs,
            // so the whole pack reads consistently.
            stage = definition is null
                ? $"Stage {instance.StageNumber}"
                : $"Stage {instance.StageNumber} — {definition.StageName}";
        }

        // The latest decision, not the first. A gateway rejected and later approved should report
        // the approval; ordering by date and taking the last is what makes that true.
        var approval = await _db.Approvals
            .AsNoTracking()
            .Where(a => a.GatewayId == gateway.Id)
            .OrderByDescending(a => a.DecisionDate)
            .FirstOrDefaultAsync(cancellationToken);

        var approver = approval is null
            ? null
            : await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == approval.ApproverUserId)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync(cancellationToken);

        // Blank rather than "Pending" where no decision has been taken. A gate report is normally
        // written to seek a decision, so blank is the honest state and leaves the approver space
        // to record one; the word "Pending" printed in a decision field reads like an outcome.
        var panel = new List<ReportPanelFieldDto>
        {
            new() { Label = "Decision", Value = approval is null ? string.Empty : Describe(approval.Decision) },
            new() { Label = "Approved By", Value = approver ?? string.Empty },
            new()
            {
                Label = "Decision Date",
                Value = approval is null
                    ? string.Empty
                    : approval.DecisionDate.ToString("d MMMM yyyy", GbCulture),
            },
        };

        return (stage, panel);
    }

    private static readonly System.Globalization.CultureInfo GbCulture = new("en-GB");

    /// <summary>
    /// The decision in the words a reader expects, rather than the enum's spelling.
    /// "ApprovedWithConditions" on a council document is a leaked identifier.
    /// </summary>
    private static string Describe(ApprovalDecision decision) => decision switch
    {
        ApprovalDecision.Approved => "Approved",
        ApprovalDecision.Rejected => "Rejected",
        ApprovalDecision.ApprovedWithConditions => "Approved with conditions",
        _ => decision.ToString(),
    };
}
