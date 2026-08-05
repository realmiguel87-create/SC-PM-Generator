using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.StakeholderManagement.Commands.CreateStakeholder;

public record CreateStakeholderCommand(
    Guid ProjectId,
    string Name,
    string? Organisation,
    string? RoleTitle,
    string? ContactEmail,
    StakeholderInfluence Influence,
    StakeholderInterest Interest) : IRequest<Guid>;

public class CreateStakeholderCommandHandler : IRequestHandler<CreateStakeholderCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateStakeholderCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateStakeholderCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var stakeholder = new Stakeholder
        {
            ProjectId = request.ProjectId,
            Name = request.Name,
            Organisation = request.Organisation,
            RoleTitle = request.RoleTitle,
            ContactEmail = request.ContactEmail,
            Influence = request.Influence,
            Interest = request.Interest,
            CreatedBy = actorId
        };

        _db.Stakeholders.Add(stakeholder);
        await _db.SaveChangesAsync(cancellationToken);

        return stakeholder.Id;
    }
}
