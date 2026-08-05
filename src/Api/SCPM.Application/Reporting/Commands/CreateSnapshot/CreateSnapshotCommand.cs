using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.Reporting.Commands.CreateSnapshot;

/// <summary>
/// Captures a named, point-in-time snapshot of a project's key figures. Used both for
/// user-initiated manual snapshots (ReportingController) and scheduled ones
/// (SCPM.Infrastructure.BackgroundJobs.SnapshotJobs, via Hangfire).
/// </summary>
public record CreateSnapshotCommand(Guid ProjectId, SnapshotType Type, string Label) : IRequest<Guid>;

public class CreateSnapshotCommandHandler : IRequestHandler<CreateSnapshotCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateSnapshotCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateSnapshotCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {request.ProjectId} not found.");

        var snapshot = new Snapshot
        {
            ProjectId = project.Id,
            Type = request.Type,
            Label = request.Label,
            CapturedAt = DateTime.UtcNow,
            RibaStageAtCapture = project.CurrentRibaStage,
            ApprovedBudgetAtCapture = project.ApprovedBudget,
            ForecastCostAtCapture = project.ForecastCost,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.Snapshots.Add(snapshot);
        await _db.SaveChangesAsync(cancellationToken);

        return snapshot.Id;
    }
}
