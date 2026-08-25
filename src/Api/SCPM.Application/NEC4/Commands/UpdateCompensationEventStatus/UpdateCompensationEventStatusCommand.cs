using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Application.NEC4.Commands.UpdateCompensationEventStatus;

public record UpdateCompensationEventStatusCommand(Guid CompensationEventId, CompensationEventStatus Status) : IRequest<Unit>;

public class UpdateCompensationEventStatusCommandHandler : IRequestHandler<UpdateCompensationEventStatusCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCompensationEventStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateCompensationEventStatusCommand request, CancellationToken cancellationToken)
    {
        var ce = await _db.CompensationEvents.FirstOrDefaultAsync(c => c.Id == request.CompensationEventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Compensation event {request.CompensationEventId} not found.");

        StatusTransitions.EnsureAllowed(
            StatusTransitions.CompensationEvent, ce.Status, request.Status, $"compensation event {ce.Reference}");

        ce.Status = request.Status;
        ce.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        ce.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
