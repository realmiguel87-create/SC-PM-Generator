using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.NEC4.Commands.UpdatePaymentAssessmentStatus;

public record UpdatePaymentAssessmentStatusCommand(Guid PaymentAssessmentId, PaymentAssessmentStatus Status) : IRequest<Unit>;

public class UpdatePaymentAssessmentStatusCommandHandler : IRequestHandler<UpdatePaymentAssessmentStatusCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdatePaymentAssessmentStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdatePaymentAssessmentStatusCommand request, CancellationToken cancellationToken)
    {
        var assessment = await _db.PaymentAssessments.FirstOrDefaultAsync(p => p.Id == request.PaymentAssessmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment assessment {request.PaymentAssessmentId} not found.");

        assessment.Status = request.Status;
        assessment.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        assessment.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
