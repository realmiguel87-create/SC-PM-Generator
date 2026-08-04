using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Projects.Dtos;

namespace SCPM.Application.Projects.Queries.GetProjects;

public record GetProjectsQuery(string? Status = null, Guid? ProgrammeId = null) : IRequest<List<ProjectListItemDto>>;

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, List<ProjectListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetProjectsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<ProjectListItemDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Projects.Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(p => p.Status.ToString() == request.Status);

        if (request.ProgrammeId.HasValue)
            query = query.Where(p => p.ProgrammeId == request.ProgrammeId);

        return await query
            .OrderBy(p => p.ProjectRef)
            .Select(p => new ProjectListItemDto
            {
                Id = p.Id,
                ProjectRef = p.ProjectRef,
                Name = p.Name,
                Status = p.Status.ToString(),
                CurrentRibaStage = p.CurrentRibaStage,
                CurrentRibaStageName = _db.RibaStageDefinitions
                    .Where(sd => sd.StageNumber == p.CurrentRibaStage)
                    .Select(sd => sd.StageName)
                    .FirstOrDefault() ?? string.Empty,
                ApprovedBudget = p.ApprovedBudget,
                ForecastCost = p.ForecastCost,
                ProgrammeName = p.Programme != null ? p.Programme.Name : null
            })
            .ToListAsync(cancellationToken);
    }
}
