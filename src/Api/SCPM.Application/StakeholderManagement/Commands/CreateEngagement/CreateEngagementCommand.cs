using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.StakeholderManagement.Commands.CreateEngagement;

public record CreateEngagementCommand(
    Guid StakeholderId,
    DateOnly EngagementDate,
    string Method,
    string Summary,
    string? Outcome) : IRequest<Guid>;

public class CreateEngagementCommandHandler : IRequestHandler<CreateEngagementCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateEngagementCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateEngagementCommand request, CancellationToken cancellationToken)
    {
        var engagement = new StakeholderEngagement
        {
            StakeholderId = request.StakeholderId,
            EngagementDate = request.EngagementDate,
            Method = request.Method,
            Summary = request.Summary,
            Outcome = request.Outcome,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.StakeholderEngagements.Add(engagement);
        await _db.SaveChangesAsync(cancellationToken);

        return engagement.Id;
    }
}
