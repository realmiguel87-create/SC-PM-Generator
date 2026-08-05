using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.NEC4.Dtos;

namespace SCPM.Application.NEC4.Queries.GetCompensationEvents;

public record GetCompensationEventsQuery(Guid ProjectId) : IRequest<List<CompensationEventDto>>;

public class GetCompensationEventsQueryHandler : IRequestHandler<GetCompensationEventsQuery, List<CompensationEventDto>>
{
    private readonly IAppDbContext _db;

    public GetCompensationEventsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<CompensationEventDto>> Handle(GetCompensationEventsQuery request, CancellationToken cancellationToken)
    {
        return await _db.CompensationEvents
            .Where(c => c.ProjectId == request.ProjectId)
            .OrderByDescending(c => c.NotifiedDate)
            .Select(c => new CompensationEventDto
            {
                Id = c.Id,
                Reference = c.Reference,
                Title = c.Title,
                ClauseReference = c.ClauseReference,
                EstimatedValue = c.EstimatedValue,
                Status = c.Status.ToString(),
                NotifiedDate = c.NotifiedDate
            })
            .ToListAsync(cancellationToken);
    }
}
