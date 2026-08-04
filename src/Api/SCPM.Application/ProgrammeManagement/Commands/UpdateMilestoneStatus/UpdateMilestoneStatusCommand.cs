using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.ProgrammeManagement.Commands.UpdateMilestoneStatus;

public record UpdateMilestoneStatusCommand(Guid MilestoneId, MilestoneStatus Status, DateOnly? ActualDate) : IRequest<Unit>;

public class UpdateMilestoneStatusCommandHandler : IRequestHandler<UpdateMilestoneStatusCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateMilestoneStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateMilestoneStatusCommand request, CancellationToken cancellationToken)
    {
        var milestone = await _db.Milestones.FirstOrDefaultAsync(m => m.Id == request.MilestoneId, cancellationToken)
            ?? throw new KeyNotFoundException($"Milestone {request.MilestoneId} not found.");

        milestone.Status = request.Status;
        if (request.Status == MilestoneStatus.Complete)
            milestone.ActualDate = request.ActualDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        milestone.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        milestone.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
