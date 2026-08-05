using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.SBCC.Dtos;

namespace SCPM.Application.SBCC.Queries.GetExtensionsOfTime;

public record GetExtensionsOfTimeQuery(Guid ProjectId) : IRequest<List<ExtensionOfTimeDto>>;

public class GetExtensionsOfTimeQueryHandler : IRequestHandler<GetExtensionsOfTimeQuery, List<ExtensionOfTimeDto>>
{
    private readonly IAppDbContext _db;

    public GetExtensionsOfTimeQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<ExtensionOfTimeDto>> Handle(GetExtensionsOfTimeQuery request, CancellationToken cancellationToken)
    {
        return await _db.ExtensionsOfTime
            .Where(e => e.ProjectId == request.ProjectId)
            .Select(e => new ExtensionOfTimeDto
            {
                Id = e.Id,
                Reference = e.Reference,
                Reason = e.Reason,
                DaysClaimed = e.DaysClaimed,
                DaysAwarded = e.DaysAwarded,
                Status = e.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
