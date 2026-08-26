using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.ProgrammeManagement.Dtos;

namespace SCPM.Application.ProgrammeManagement.Queries.GetProgrammeBaselines;

/// <summary>Lists a project's sanctioned programmes, newest revision first.</summary>
public record GetProgrammeBaselinesQuery(Guid ProjectId) : IRequest<List<ProgrammeBaselineDto>>;

public class GetProgrammeBaselinesQueryHandler
    : IRequestHandler<GetProgrammeBaselinesQuery, List<ProgrammeBaselineDto>>
{
    private readonly IAppDbContext _db;

    public GetProgrammeBaselinesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<ProgrammeBaselineDto>> Handle(
        GetProgrammeBaselinesQuery request, CancellationToken cancellationToken) =>
        await _db.ProgrammeBaselines
            .AsNoTracking()
            .Where(b => b.ProjectId == request.ProjectId)
            .OrderByDescending(b => b.Revision)
            .Select(b => new ProgrammeBaselineDto(
                b.Id,
                b.Revision,
                b.Name,
                b.Reason,
                b.ApprovedBy,
                b.ApprovedDate,
                b.IsCurrent,
                b.CreatedDate,
                b.Entries.Count))
            .ToListAsync(cancellationToken);
}
