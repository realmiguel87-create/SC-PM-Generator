using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Application.SBCC.Commands.UpdateExtensionOfTimeStatus;

public record UpdateExtensionOfTimeStatusCommand(Guid ExtensionOfTimeId, ExtensionOfTimeStatus Status, int? DaysAwarded) : IRequest<Unit>;

public class UpdateExtensionOfTimeStatusCommandHandler : IRequestHandler<UpdateExtensionOfTimeStatusCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateExtensionOfTimeStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateExtensionOfTimeStatusCommand request, CancellationToken cancellationToken)
    {
        var eot = await _db.ExtensionsOfTime.FirstOrDefaultAsync(e => e.Id == request.ExtensionOfTimeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Extension of time {request.ExtensionOfTimeId} not found.");

        StatusTransitions.EnsureAllowed(
            StatusTransitions.ExtensionOfTime, eot.Status, request.Status, $"extension of time {eot.Reference}");

        eot.Status = request.Status;
        if (request.DaysAwarded.HasValue)
            eot.DaysAwarded = request.DaysAwarded;

        eot.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        eot.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
