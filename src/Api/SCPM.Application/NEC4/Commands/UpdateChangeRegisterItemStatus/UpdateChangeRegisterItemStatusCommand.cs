using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Application.NEC4.Commands.UpdateChangeRegisterItemStatus;

public record UpdateChangeRegisterItemStatusCommand(Guid ChangeRegisterItemId, ChangeRegisterStatus Status) : IRequest<Unit>;

public class UpdateChangeRegisterItemStatusCommandHandler : IRequestHandler<UpdateChangeRegisterItemStatusCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateChangeRegisterItemStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateChangeRegisterItemStatusCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.ChangeRegisterItems.FirstOrDefaultAsync(c => c.Id == request.ChangeRegisterItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Change register item {request.ChangeRegisterItemId} not found.");

        StatusTransitions.EnsureAllowed(
            StatusTransitions.ChangeRegisterItem, item.Status, request.Status, $"change {item.Title}");

        item.Status = request.Status;
        item.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        item.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
