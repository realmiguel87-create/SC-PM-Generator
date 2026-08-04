using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.SBCC.Commands.UpdateVariationStatus;

public record UpdateVariationStatusCommand(Guid VariationId, VariationStatus Status) : IRequest<Unit>;

public class UpdateVariationStatusCommandHandler : IRequestHandler<UpdateVariationStatusCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateVariationStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateVariationStatusCommand request, CancellationToken cancellationToken)
    {
        var variation = await _db.Variations.FirstOrDefaultAsync(v => v.Id == request.VariationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Variation {request.VariationId} not found.");

        variation.Status = request.Status;
        variation.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        variation.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
