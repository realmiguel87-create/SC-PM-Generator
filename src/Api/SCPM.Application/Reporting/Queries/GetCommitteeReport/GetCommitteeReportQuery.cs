using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Domain.Common;

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
            .FirstOrDefaultAsync(r => r.Id == request.CommitteeReportId, cancellationToken);

        if (report is null) return null;

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

            // Driven by the report type's definition, not by what happens to be stored. An author
            // needs to see the headings they have not written yet; listing only the stored rows
            // would hide precisely the sections that still need work.
            Sections = ReportSections.For(report.ReportType)
                .Select(s => new ReportSectionDto
                {
                    Key = s.Key,
                    Heading = s.Heading,
                    Content = content.GetValueOrDefault(s.Key),
                })
                .ToList(),
        };
    }
}
