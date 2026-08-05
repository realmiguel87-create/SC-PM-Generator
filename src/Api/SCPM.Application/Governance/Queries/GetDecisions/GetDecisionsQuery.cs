using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Governance.Dtos;

namespace SCPM.Application.Governance.Queries.GetDecisions;

public record GetDecisionsQuery(Guid ProjectId) : IRequest<List<DecisionDto>>;

public class GetDecisionsQueryHandler : IRequestHandler<GetDecisionsQuery, List<DecisionDto>>
{
    private readonly IAppDbContext _db;

    public GetDecisionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<DecisionDto>> Handle(GetDecisionsQuery request, CancellationToken cancellationToken)
    {
        return await _db.DecisionRegisterEntries
            .Where(d => d.ProjectId == request.ProjectId)
            .OrderByDescending(d => d.DecisionDate)
            .Select(d => new DecisionDto
            {
                Id = d.Id,
                Title = d.Title,
                Description = d.Description,
                DecisionDate = d.DecisionDate,
                Rationale = d.Rationale
            })
            .ToListAsync(cancellationToken);
    }
}
