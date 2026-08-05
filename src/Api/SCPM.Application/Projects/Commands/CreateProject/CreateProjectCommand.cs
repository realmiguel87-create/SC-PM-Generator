using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.Projects.Commands.CreateProject;

public record CreateProjectCommand(
    string ProjectRef,
    string Name,
    string? Description,
    Guid? ProgrammeId,
    decimal ApprovedBudget,
    DateOnly? StartDate,
    DateOnly? TargetCompletionDate,
    Guid? SponsorUserId,
    Guid? ProjectManagerUserId) : IRequest<Guid>;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateProjectCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var project = new Project
        {
            ProjectRef = request.ProjectRef,
            Name = request.Name,
            Description = request.Description,
            ProgrammeId = request.ProgrammeId,
            ApprovedBudget = request.ApprovedBudget,
            ForecastCost = request.ApprovedBudget,
            StartDate = request.StartDate,
            TargetCompletionDate = request.TargetCompletionDate,
            SponsorUserId = request.SponsorUserId,
            ProjectManagerUserId = request.ProjectManagerUserId,
            CurrentRibaStage = 0,
            CreatedBy = actorId
        };

        // RIBA Stage 0 begins immediately; stages 1-7 are created NotStarted so the
        // project workspace can show the full lifecycle from day one.
        for (byte stage = 0; stage <= 7; stage++)
        {
            project.RibaStageInstances.Add(new RibaStageInstance
            {
                StageNumber = stage,
                Status = stage == 0
                    ? Domain.Enums.RibaStageInstanceStatus.InProgress
                    : Domain.Enums.RibaStageInstanceStatus.NotStarted,
                ActualStartDate = stage == 0 ? DateOnly.FromDateTime(DateTime.UtcNow) : null,
                CreatedBy = actorId
            });
        }

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}
