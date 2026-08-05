using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.RiskManagement.Commands.CreateRisk;

public record CreateRiskCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    string Category,
    int Probability,
    int Impact,
    string? MitigationPlan) : IRequest<Guid>;

public class CreateRiskCommandHandler : IRequestHandler<CreateRiskCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateRiskCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateRiskCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var risk = new Risk
        {
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Probability = request.Probability,
            Impact = request.Impact,
            MitigationPlan = request.MitigationPlan,
            OwnerUserId = actorId,
            CreatedBy = actorId
        };

        _db.Risks.Add(risk);
        await _db.SaveChangesAsync(cancellationToken);

        return risk.Id;
    }
}
