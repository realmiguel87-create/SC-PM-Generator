using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.Programme.Commands.CreateMilestone;

public record CreateMilestoneCommand(
    Guid ProjectId,
    string Name,
    string? Description,
    DateOnly BaselineDate,
    DateOnly ForecastDate,
    bool IsKeyMilestone) : IRequest<Guid>;

public class CreateMilestoneCommandHandler : IRequestHandler<CreateMilestoneCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateMilestoneCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateMilestoneCommand request, CancellationToken cancellationToken)
    {
        var milestone = new Milestone
        {
            ProjectId = request.ProjectId,
            Name = request.Name,
            Description = request.Description,
            BaselineDate = request.BaselineDate,
            ForecastDate = request.ForecastDate,
            IsKeyMilestone = request.IsKeyMilestone,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.Milestones.Add(milestone);
        await _db.SaveChangesAsync(cancellationToken);

        return milestone.Id;
    }
}
