using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.Projects.Commands.AdvanceRibaStage;

/// <summary>
/// Marks the current RIBA stage instance Complete and starts the next stage.
/// Progressing past a stage that requires a gateway is enforced by the Governance
/// module (a Gateway must be Approved before this succeeds) — see GatewayGuard below.
/// </summary>
public record AdvanceRibaStageCommand(Guid ProjectId) : IRequest<Unit>;

public class AdvanceRibaStageCommandHandler : IRequestHandler<AdvanceRibaStageCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AdvanceRibaStageCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(AdvanceRibaStageCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects
            .Include(p => p.RibaStageInstances)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {request.ProjectId} not found.");

        if (project.CurrentRibaStage >= 7)
            throw new InvalidOperationException("Project is already at the final RIBA stage (Use).");

        var actorId = _currentUser.UserId ?? Guid.Empty;

        var currentStage = project.RibaStageInstances.First(s => s.StageNumber == project.CurrentRibaStage);
        if (currentStage.Status != RibaStageInstanceStatus.Gated && currentStage.Status != RibaStageInstanceStatus.Complete)
            throw new InvalidOperationException(
                $"Stage {currentStage.StageNumber} must pass its approval gate before the project can advance.");

        var nextStageNumber = (byte)(project.CurrentRibaStage + 1);
        var nextStage = project.RibaStageInstances.First(s => s.StageNumber == nextStageNumber);

        currentStage.Status = RibaStageInstanceStatus.Complete;
        currentStage.ActualEndDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
        currentStage.ModifiedBy = actorId;
        currentStage.ModifiedDate = DateTime.UtcNow;

        nextStage.Status = RibaStageInstanceStatus.InProgress;
        nextStage.ActualStartDate = DateOnly.FromDateTime(DateTime.UtcNow);
        nextStage.ModifiedBy = actorId;
        nextStage.ModifiedDate = DateTime.UtcNow;

        project.CurrentRibaStage = nextStageNumber;
        project.ModifiedBy = actorId;
        project.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
