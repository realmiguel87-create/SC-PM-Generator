using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.RiskManagement.Dtos;

namespace SCPM.Application.RiskManagement.Queries.GetIssues;

public record GetIssuesQuery(Guid ProjectId) : IRequest<List<IssueDto>>;

public class GetIssuesQueryHandler : IRequestHandler<GetIssuesQuery, List<IssueDto>>
{
    private readonly IAppDbContext _db;

    public GetIssuesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<IssueDto>> Handle(GetIssuesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Issues
            .Where(i => i.ProjectId == request.ProjectId)
            .OrderByDescending(i => i.RaisedDate)
            .Select(i => new IssueDto
            {
                Id = i.Id,
                Title = i.Title,
                Description = i.Description,
                Severity = i.Severity.ToString(),
                Status = i.Status.ToString(),
                RaisedDate = i.RaisedDate,
                ResolvedDate = i.ResolvedDate,
                ResolutionNotes = i.ResolutionNotes
            })
            .ToListAsync(cancellationToken);
    }
}
