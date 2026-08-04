using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Programme.Dtos;

namespace SCPM.Application.Programme.Queries.GetMilestones;

public record GetMilestonesQuery(Guid ProjectId) : IRequest<List<MilestoneDto>>;

public class GetMilestonesQueryHandler : IRequestHandler<GetMilestonesQuery, List<MilestoneDto>>
{
    private readonly IAppDbContext _db;

    public GetMilestonesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<MilestoneDto>> Handle(GetMilestonesQuery request, CancellationToken cancellationToken)
    {
        var milestones = await _db.Milestones
            .Where(m => m.ProjectId == request.ProjectId)
            .OrderBy(m => m.ForecastDate)
            .ToListAsync(cancellationToken);

        return milestones.Select(m => new MilestoneDto
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            Status = m.Status.ToString(),
            BaselineDate = m.BaselineDate,
            ForecastDate = m.ForecastDate,
            ActualDate = m.ActualDate,
            IsKeyMilestone = m.IsKeyMilestone,
            DelayDays = m.DelayDays
        }).ToList();
    }
}
