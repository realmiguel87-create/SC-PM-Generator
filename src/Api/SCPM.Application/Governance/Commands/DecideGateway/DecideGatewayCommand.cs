using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.Governance.Commands.DecideGateway;

/// <summary>
/// Records an approver's decision on a gateway. On Approved/ApprovedWithConditions this moves
/// the underlying RibaStageInstance to Gated, which is the state AdvanceRibaStageCommand requires
/// before a project can move to its next RIBA stage. Mirrors the atomicity of
/// database/procedures/010_Governance_ApproveGateway.sql (approval + gateway + stage updated in
/// one SaveChanges), but goes through EF Core rather than the raw stored procedure so the
/// Audit.ActivityLog / Audit.FieldAudit interceptor captures every change automatically.
/// </summary>
public record DecideGatewayCommand(Guid GatewayId, ApprovalDecision Decision, string? Comments) : IRequest<Unit>;

public class DecideGatewayCommandHandler : IRequestHandler<DecideGatewayCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DecideGatewayCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DecideGatewayCommand request, CancellationToken cancellationToken)
    {
        var gateway = await _db.Gateways
            .Include(g => g.RibaStageInstance)
            .FirstOrDefaultAsync(g => g.Id == request.GatewayId, cancellationToken)
            ?? throw new KeyNotFoundException($"Gateway {request.GatewayId} not found.");

        if (gateway.Status != GatewayStatus.Pending)
            throw new InvalidOperationException($"Gateway {gateway.Id} has already been decided ({gateway.Status}).");

        var actorId = _currentUser.UserId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        _db.Approvals.Add(new Approval
        {
            GatewayId = gateway.Id,
            ApproverUserId = actorId,
            Decision = request.Decision,
            Comments = request.Comments,
            DecisionDate = now,
            CreatedBy = actorId
        });

        gateway.Status = request.Decision == ApprovalDecision.Rejected ? GatewayStatus.Rejected : GatewayStatus.Approved;
        gateway.ModifiedBy = actorId;
        gateway.ModifiedDate = now;

        if (request.Decision is ApprovalDecision.Approved or ApprovalDecision.ApprovedWithConditions)
        {
            gateway.RibaStageInstance.Status = RibaStageInstanceStatus.Gated;
            gateway.RibaStageInstance.ActualEndDate ??= DateOnly.FromDateTime(now);
            gateway.RibaStageInstance.ModifiedBy = actorId;
            gateway.RibaStageInstance.ModifiedDate = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
