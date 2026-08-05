using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.NEC4.Dtos;

namespace SCPM.Application.NEC4.Queries.GetRiskAllocationItems;

public record GetRiskAllocationItemsQuery(Guid ProjectId) : IRequest<List<RiskAllocationItemDto>>;

public class GetRiskAllocationItemsQueryHandler : IRequestHandler<GetRiskAllocationItemsQuery, List<RiskAllocationItemDto>>
{
    private readonly IAppDbContext _db;

    public GetRiskAllocationItemsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<RiskAllocationItemDto>> Handle(GetRiskAllocationItemsQuery request, CancellationToken cancellationToken)
    {
        return await _db.RiskAllocationItems
            .Where(r => r.ProjectId == request.ProjectId)
            .Select(r => new RiskAllocationItemDto
            {
                Id = r.Id,
                Description = r.Description,
                AllocatedTo = r.AllocatedTo.ToString(),
                MitigationOwner = r.MitigationOwner
            })
            .ToListAsync(cancellationToken);
    }
}
