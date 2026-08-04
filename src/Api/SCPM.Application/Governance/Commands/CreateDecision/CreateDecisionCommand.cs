using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.Governance.Commands.CreateDecision;

public record CreateDecisionCommand(
    Guid ProjectId,
    string Title,
    string Description,
    DateOnly DecisionDate,
    string? Rationale) : IRequest<Guid>;

public class CreateDecisionCommandHandler : IRequestHandler<CreateDecisionCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateDecisionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateDecisionCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var decision = new DecisionRegisterEntry
        {
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            DecisionDate = request.DecisionDate,
            Rationale = request.Rationale,
            DecisionOwnerUserId = actorId,
            CreatedBy = actorId
        };

        _db.DecisionRegisterEntries.Add(decision);
        await _db.SaveChangesAsync(cancellationToken);

        return decision.Id;
    }
}
