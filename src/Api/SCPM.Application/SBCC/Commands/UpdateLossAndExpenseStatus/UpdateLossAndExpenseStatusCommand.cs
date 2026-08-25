using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.SBCC.Commands.UpdateLossAndExpenseStatus;

/// <summary>
/// Determines a loss and expense claim: moves it through the SBCC process and, where an amount
/// has been agreed, records it.
///
/// Deliberately mirrors UpdateExtensionOfTimeStatusCommand, including the nullable award. A
/// claim under review has no awarded amount, and that is not the same as an award of nothing —
/// so passing null leaves the existing value untouched rather than clearing it, and only an
/// explicit figure writes one. Same distinction as EOT's DaysAwarded, and for the same reason:
/// in a contractual register, "not yet determined" and "determined at zero" are different facts.
/// </summary>
public record UpdateLossAndExpenseStatusCommand(
    Guid LossAndExpenseClaimId,
    LossAndExpenseStatus Status,
    decimal? AwardedAmount) : IRequest<Unit>;

public class UpdateLossAndExpenseStatusCommandHandler
    : IRequestHandler<UpdateLossAndExpenseStatusCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateLossAndExpenseStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateLossAndExpenseStatusCommand request, CancellationToken cancellationToken)
    {
        var claim = await _db.LossAndExpenseClaims
            .FirstOrDefaultAsync(c => c.Id == request.LossAndExpenseClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Loss and expense claim {request.LossAndExpenseClaimId} not found.");

        claim.Status = request.Status;
        if (request.AwardedAmount.HasValue)
            claim.AwardedAmount = request.AwardedAmount;

        claim.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        claim.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
