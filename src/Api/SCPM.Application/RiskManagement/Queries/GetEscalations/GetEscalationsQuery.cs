using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.RiskManagement.Dtos;

namespace SCPM.Application.RiskManagement.Queries.GetEscalations;

public record GetEscalationsQuery(Guid ProjectId) : IRequest<List<EscalationDto>>;

public class GetEscalationsQueryHandler : IRequestHandler<GetEscalationsQuery, List<EscalationDto>>
{
    private readonly IAppDbContext _db;

    public GetEscalationsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<EscalationDto>> Handle(GetEscalationsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Escalations
            .Where(e => e.ProjectId == request.ProjectId)
            .OrderByDescending(e => e.RaisedDate)
            .Select(e => new EscalationDto
            {
                Id = e.Id,
                RiskId = e.RiskId,
                RiskTitle = e.Risk != null ? e.Risk.Title : null,
                IssueId = e.IssueId,
                IssueTitle = e.Issue != null ? e.Issue.Title : null,
                Reason = e.Reason,
                Status = e.Status.ToString(),
                RaisedDate = e.RaisedDate,
                ResolvedDate = e.ResolvedDate,
                ResolutionNotes = e.ResolutionNotes
            })
            .ToListAsync(cancellationToken);
    }
}
