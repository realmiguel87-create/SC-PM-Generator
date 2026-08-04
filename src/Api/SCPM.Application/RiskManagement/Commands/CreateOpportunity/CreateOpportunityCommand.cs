using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.RiskManagement.Commands.CreateOpportunity;

public record CreateOpportunityCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    decimal PotentialValue,
    int Probability) : IRequest<Guid>;

public class CreateOpportunityCommandHandler : IRequestHandler<CreateOpportunityCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateOpportunityCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateOpportunityCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var opportunity = new Opportunity
        {
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            PotentialValue = request.PotentialValue,
            Probability = request.Probability,
            OwnerUserId = actorId,
            CreatedBy = actorId
        };

        _db.Opportunities.Add(opportunity);
        await _db.SaveChangesAsync(cancellationToken);

        return opportunity.Id;
    }
}
