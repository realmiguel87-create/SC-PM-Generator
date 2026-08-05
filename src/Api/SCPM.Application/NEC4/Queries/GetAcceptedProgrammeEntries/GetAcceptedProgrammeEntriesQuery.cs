using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.NEC4.Dtos;

namespace SCPM.Application.NEC4.Queries.GetAcceptedProgrammeEntries;

public record GetAcceptedProgrammeEntriesQuery(Guid ProjectId) : IRequest<List<AcceptedProgrammeEntryDto>>;

public class GetAcceptedProgrammeEntriesQueryHandler : IRequestHandler<GetAcceptedProgrammeEntriesQuery, List<AcceptedProgrammeEntryDto>>
{
    private readonly IAppDbContext _db;

    public GetAcceptedProgrammeEntriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<AcceptedProgrammeEntryDto>> Handle(GetAcceptedProgrammeEntriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.AcceptedProgrammeEntries
            .Where(a => a.ProjectId == request.ProjectId)
            .OrderByDescending(a => a.RevisionNumber)
            .Select(a => new AcceptedProgrammeEntryDto
            {
                Id = a.Id,
                RevisionNumber = a.RevisionNumber,
                AcceptedDate = a.AcceptedDate,
                Notes = a.Notes
            })
            .ToListAsync(cancellationToken);
    }
}
