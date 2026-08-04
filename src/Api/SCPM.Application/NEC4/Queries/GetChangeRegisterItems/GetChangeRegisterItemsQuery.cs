using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.NEC4.Dtos;

namespace SCPM.Application.NEC4.Queries.GetChangeRegisterItems;

public record GetChangeRegisterItemsQuery(Guid ProjectId) : IRequest<List<ChangeRegisterItemDto>>;

public class GetChangeRegisterItemsQueryHandler : IRequestHandler<GetChangeRegisterItemsQuery, List<ChangeRegisterItemDto>>
{
    private readonly IAppDbContext _db;

    public GetChangeRegisterItemsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<ChangeRegisterItemDto>> Handle(GetChangeRegisterItemsQuery request, CancellationToken cancellationToken)
    {
        return await _db.ChangeRegisterItems
            .Where(c => c.ProjectId == request.ProjectId)
            .Select(c => new ChangeRegisterItemDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                ValueImpact = c.ValueImpact,
                TimeImpactDays = c.TimeImpactDays,
                Status = c.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
