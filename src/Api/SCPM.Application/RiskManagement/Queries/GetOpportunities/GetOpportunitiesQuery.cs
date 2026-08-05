using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.RiskManagement.Dtos;

namespace SCPM.Application.RiskManagement.Queries.GetOpportunities;

public record GetOpportunitiesQuery(Guid ProjectId) : IRequest<List<OpportunityDto>>;

public class GetOpportunitiesQueryHandler : IRequestHandler<GetOpportunitiesQuery, List<OpportunityDto>>
{
    private readonly IAppDbContext _db;

    public GetOpportunitiesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<OpportunityDto>> Handle(GetOpportunitiesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Opportunities
            .Where(o => o.ProjectId == request.ProjectId)
            .OrderByDescending(o => o.PotentialValue)
            .Select(o => new OpportunityDto
            {
                Id = o.Id,
                Title = o.Title,
                Description = o.Description,
                PotentialValue = o.PotentialValue,
                Probability = o.Probability,
                Status = o.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
