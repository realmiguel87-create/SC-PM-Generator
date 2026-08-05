using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.RiskManagement.Commands.CreateEscalation;

/// <summary>Exactly one of RiskId/IssueId must be set — enforced here and by the DB check
/// constraint CK_Escalation_ExactlyOneSource.</summary>
public record CreateEscalationCommand(Guid ProjectId, Guid? RiskId, Guid? IssueId, string Reason) : IRequest<Guid>;

public class CreateEscalationCommandHandler : IRequestHandler<CreateEscalationCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateEscalationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateEscalationCommand request, CancellationToken cancellationToken)
    {
        if ((request.RiskId is null) == (request.IssueId is null))
            throw new InvalidOperationException("An escalation must reference exactly one of Risk or Issue.");

        var actorId = _currentUser.UserId ?? Guid.Empty;

        var escalation = new Escalation
        {
            ProjectId = request.ProjectId,
            RiskId = request.RiskId,
            IssueId = request.IssueId,
            Reason = request.Reason,
            RaisedByUserId = actorId,
            RaisedDate = DateTime.UtcNow,
            CreatedBy = actorId
        };

        _db.Escalations.Add(escalation);

        if (request.RiskId is not null)
        {
            var risk = await _db.Risks.FindAsync([request.RiskId], cancellationToken);
            if (risk is not null) risk.Status = RiskStatus.Escalated;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return escalation.Id;
    }
}
