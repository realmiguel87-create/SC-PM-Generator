using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;

namespace SCPM.Application.Reporting.Queries.GetCommitteeReports;

/// <summary>Lists committee reports. ProjectId narrows to one project (used by the workspace
/// Reports tab); omitted, it returns every report across the portfolio (the Reporting Centre).</summary>
public record GetCommitteeReportsQuery(Guid? ProjectId) : IRequest<List<CommitteeReportListItemDto>>;

public class GetCommitteeReportsQueryHandler : IRequestHandler<GetCommitteeReportsQuery, List<CommitteeReportListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetCommitteeReportsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<CommitteeReportListItemDto>> Handle(GetCommitteeReportsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.CommitteeReports.Include(r => r.Project).AsQueryable();

        if (request.ProjectId.HasValue)
            query = query.Where(r => r.ProjectId == request.ProjectId);

        return await query
            .OrderByDescending(r => r.CreatedDate)
            .Select(r => new CommitteeReportListItemDto
            {
                Id = r.Id,
                ProjectId = r.ProjectId,
                ProjectName = r.Project.Name,
                ProjectRef = r.Project.ProjectRef,
                ReportType = r.ReportType.ToString(),
                Title = r.Title,
                MeetingDate = r.MeetingDate,
                Status = r.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
