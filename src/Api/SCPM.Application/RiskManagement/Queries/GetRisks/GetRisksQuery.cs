using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.RiskManagement.Dtos;

namespace SCPM.Application.RiskManagement.Queries.GetRisks;

public record GetRisksQuery(Guid ProjectId) : IRequest<List<RiskDto>>;

public class GetRisksQueryHandler : IRequestHandler<GetRisksQuery, List<RiskDto>>
{
    private readonly IAppDbContext _db;

    public GetRisksQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<RiskDto>> Handle(GetRisksQuery request, CancellationToken cancellationToken)
    {
        var risks = await _db.Risks
            .Where(r => r.ProjectId == request.ProjectId)
            .ToListAsync(cancellationToken);

        return risks
            .OrderByDescending(r => r.Score)
            .Select(r => new RiskDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Category = r.Category,
                Probability = r.Probability,
                Impact = r.Impact,
                Score = r.Score,
                Status = r.Status.ToString(),
                MitigationPlan = r.MitigationPlan
            })
            .ToList();
    }
}
