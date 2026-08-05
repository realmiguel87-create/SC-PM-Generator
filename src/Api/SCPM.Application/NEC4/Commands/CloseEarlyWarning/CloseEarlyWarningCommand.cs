using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.NEC4.Commands.CloseEarlyWarning;

public record CloseEarlyWarningCommand(Guid EarlyWarningId) : IRequest<Unit>;

public class CloseEarlyWarningCommandHandler : IRequestHandler<CloseEarlyWarningCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CloseEarlyWarningCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(CloseEarlyWarningCommand request, CancellationToken cancellationToken)
    {
        var earlyWarning = await _db.EarlyWarnings.FirstOrDefaultAsync(e => e.Id == request.EarlyWarningId, cancellationToken)
            ?? throw new KeyNotFoundException($"Early warning {request.EarlyWarningId} not found.");

        earlyWarning.Status = Nec4RegisterStatus.Closed;
        earlyWarning.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        earlyWarning.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
