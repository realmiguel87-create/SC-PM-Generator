using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Projects.Dtos;

namespace SCPM.Application.Projects.Queries.GetProjectById;

public record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectDetailDto?>;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto?>
{
    private readonly IAppDbContext _db;

    public GetProjectByIdQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ProjectDetailDto?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects
            .Include(p => p.RibaStageInstances)
            .Include(p => p.Programme)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken);

        if (project is null)
            return null;

        var stageDefinitions = await _db.RibaStageDefinitions.ToDictionaryAsync(sd => sd.StageNumber, cancellationToken);

        // Most recent gateway per stage instance, so the UI can offer "decide" once one is
        // pending and show the outcome once it's been approved/rejected.
        var stageInstanceIds = project.RibaStageInstances.Select(s => s.Id).ToList();
        var gatewaysForProject = await _db.Gateways
            .Where(g => stageInstanceIds.Contains(g.RibaStageInstanceId))
            .OrderByDescending(g => g.CreatedDate)
            .ToListAsync(cancellationToken);
        var latestGatewayByStage = gatewaysForProject
            .GroupBy(g => g.RibaStageInstanceId)
            .ToDictionary(g => g.Key, g => g.First());

        return new ProjectDetailDto
        {
            Id = project.Id,
            ProjectRef = project.ProjectRef,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status.ToString(),
            CurrentRibaStage = project.CurrentRibaStage,
            CurrentRibaStageName = stageDefinitions.GetValueOrDefault(project.CurrentRibaStage)?.StageName ?? string.Empty,
            ApprovedBudget = project.ApprovedBudget,
            ForecastCost = project.ForecastCost,
            StartDate = project.StartDate,
            TargetCompletionDate = project.TargetCompletionDate,
            ProgrammeName = project.Programme?.Name,
            RibaStages = project.RibaStageInstances
                .OrderBy(s => s.StageNumber)
                .Select(s =>
                {
                    latestGatewayByStage.TryGetValue(s.Id, out var gateway);
                    return new RibaStageInstanceDto
                    {
                        Id = s.Id,
                        StageNumber = s.StageNumber,
                        StageName = stageDefinitions.GetValueOrDefault(s.StageNumber)?.StageName ?? string.Empty,
                        Status = s.Status.ToString(),
                        PlannedStartDate = s.PlannedStartDate,
                        PlannedEndDate = s.PlannedEndDate,
                        ActualStartDate = s.ActualStartDate,
                        ActualEndDate = s.ActualEndDate,
                        PendingGatewayId = gateway?.Status == Domain.Enums.GatewayStatus.Pending ? gateway.Id : null,
                        GatewayStatus = gateway?.Status.ToString()
                    };
                })
                .ToList()
        };
    }
}
