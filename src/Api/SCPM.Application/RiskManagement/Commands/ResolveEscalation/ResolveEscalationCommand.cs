using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.RiskManagement.Commands.ResolveEscalation;

public record ResolveEscalationCommand(Guid EscalationId, string ResolutionNotes) : IRequest<Unit>;

public class ResolveEscalationCommandHandler : IRequestHandler<ResolveEscalationCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ResolveEscalationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ResolveEscalationCommand request, CancellationToken cancellationToken)
    {
        var escalation = await _db.Escalations.FirstOrDefaultAsync(e => e.Id == request.EscalationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Escalation {request.EscalationId} not found.");

        var actorId = _currentUser.UserId ?? Guid.Empty;

        escalation.Status = EscalationStatus.Resolved;
        escalation.ResolutionNotes = request.ResolutionNotes;
        escalation.ResolvedByUserId = actorId;
        escalation.ResolvedDate = DateTime.UtcNow;
        escalation.ModifiedBy = actorId;
        escalation.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
