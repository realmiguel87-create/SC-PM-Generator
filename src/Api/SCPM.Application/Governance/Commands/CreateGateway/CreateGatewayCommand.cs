using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.Governance.Commands.CreateGateway;

/// <summary>
/// Opens a stage-gate approval request for a project's current (or specified) RIBA stage.
/// This is the missing link that makes AdvanceRibaStageCommand reachable: a stage can only
/// advance once its gateway has been decided via DecideGatewayCommand.
/// </summary>
public record CreateGatewayCommand(Guid ProjectId, byte StageNumber, string GatewayType, DateOnly? DueDate) : IRequest<Guid>;

public class CreateGatewayCommandHandler : IRequestHandler<CreateGatewayCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateGatewayCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateGatewayCommand request, CancellationToken cancellationToken)
    {
        var stageInstance = await _db.RibaStageInstances
            .FirstOrDefaultAsync(s => s.ProjectId == request.ProjectId && s.StageNumber == request.StageNumber, cancellationToken)
            ?? throw new KeyNotFoundException($"RIBA stage {request.StageNumber} not found for project {request.ProjectId}.");

        var hasPendingGateway = await _db.Gateways.AnyAsync(
            g => g.RibaStageInstanceId == stageInstance.Id && g.Status == GatewayStatus.Pending, cancellationToken);
        if (hasPendingGateway)
            throw new InvalidOperationException("This stage already has a pending gateway approval request.");

        var actorId = _currentUser.UserId ?? Guid.Empty;

        var gateway = new Gateway
        {
            ProjectId = request.ProjectId,
            RibaStageInstanceId = stageInstance.Id,
            GatewayType = request.GatewayType,
            Status = GatewayStatus.Pending,
            DueDate = request.DueDate,
            CreatedBy = actorId
        };

        _db.Gateways.Add(gateway);
        await _db.SaveChangesAsync(cancellationToken);

        return gateway.Id;
    }
}
