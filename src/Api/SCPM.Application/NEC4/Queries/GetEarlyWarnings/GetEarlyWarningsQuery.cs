using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.NEC4.Dtos;

namespace SCPM.Application.NEC4.Queries.GetEarlyWarnings;

public record GetEarlyWarningsQuery(Guid ProjectId) : IRequest<List<EarlyWarningDto>>;

public class GetEarlyWarningsQueryHandler : IRequestHandler<GetEarlyWarningsQuery, List<EarlyWarningDto>>
{
    private readonly IAppDbContext _db;

    public GetEarlyWarningsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<EarlyWarningDto>> Handle(GetEarlyWarningsQuery request, CancellationToken cancellationToken)
    {
        return await _db.EarlyWarnings
            .Where(e => e.ProjectId == request.ProjectId)
            .OrderByDescending(e => e.RaisedDate)
            .Select(e => new EarlyWarningDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                RaisedDate = e.RaisedDate,
                MitigationAction = e.MitigationAction,
                Status = e.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
