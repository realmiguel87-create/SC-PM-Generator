using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.StakeholderManagement.Dtos;

namespace SCPM.Application.StakeholderManagement.Queries.GetStakeholders;

public record GetStakeholdersQuery(Guid ProjectId) : IRequest<List<StakeholderDto>>;

public class GetStakeholdersQueryHandler : IRequestHandler<GetStakeholdersQuery, List<StakeholderDto>>
{
    private readonly IAppDbContext _db;

    public GetStakeholdersQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<StakeholderDto>> Handle(GetStakeholdersQuery request, CancellationToken cancellationToken)
    {
        var stakeholders = await _db.Stakeholders
            .Include(s => s.Engagements)
            .Where(s => s.ProjectId == request.ProjectId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return stakeholders.Select(s => new StakeholderDto
        {
            Id = s.Id,
            Name = s.Name,
            Organisation = s.Organisation,
            RoleTitle = s.RoleTitle,
            ContactEmail = s.ContactEmail,
            Influence = s.Influence.ToString(),
            Interest = s.Interest.ToString(),
            Engagements = s.Engagements
                .OrderByDescending(e => e.EngagementDate)
                .Select(e => new StakeholderEngagementDto
                {
                    Id = e.Id,
                    EngagementDate = e.EngagementDate,
                    Method = e.Method,
                    Summary = e.Summary,
                    Outcome = e.Outcome
                })
                .ToList()
        }).ToList();
    }
}
