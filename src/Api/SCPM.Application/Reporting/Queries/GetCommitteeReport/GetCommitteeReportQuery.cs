using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;

namespace SCPM.Application.Reporting.Queries.GetCommitteeReport;

public record GetCommitteeReportQuery(Guid CommitteeReportId) : IRequest<CommitteeReportDto?>;

public class GetCommitteeReportQueryHandler : IRequestHandler<GetCommitteeReportQuery, CommitteeReportDto?>
{
    private readonly IAppDbContext _db;

    public GetCommitteeReportQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CommitteeReportDto?> Handle(GetCommitteeReportQuery request, CancellationToken cancellationToken)
    {
        return await _db.CommitteeReports
            .Include(r => r.Project)
            .Where(r => r.Id == request.CommitteeReportId)
            .Select(r => new CommitteeReportDto
            {
                Id = r.Id,
                ProjectId = r.ProjectId,
                ProjectName = r.Project.Name,
                ProjectRef = r.Project.ProjectRef,
                ReportType = r.ReportType.ToString(),
                Title = r.Title,
                MeetingDate = r.MeetingDate,
                Status = r.Status.ToString(),
                CreatedDate = r.CreatedDate,
                ExecutiveSummary = r.ExecutiveSummary,
                Background = r.Background,
                CurrentPosition = r.CurrentPosition,
                FinanceCommentary = r.FinanceCommentary,
                ProgrammeCommentary = r.ProgrammeCommentary,
                RiskCommentary = r.RiskCommentary,
                StakeholderCommentary = r.StakeholderCommentary,
                SustainabilityCommentary = r.SustainabilityCommentary,
                EqualityImpactCommentary = r.EqualityImpactCommentary,
                Recommendations = r.Recommendations
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
