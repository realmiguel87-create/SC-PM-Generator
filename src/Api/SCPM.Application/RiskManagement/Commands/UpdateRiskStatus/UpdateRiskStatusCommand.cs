using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.RiskManagement.Commands.UpdateRiskStatus;

public record UpdateRiskStatusCommand(Guid RiskId, RiskStatus Status, string? MitigationPlan) : IRequest<Unit>;

public class UpdateRiskStatusCommandHandler : IRequestHandler<UpdateRiskStatusCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateRiskStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateRiskStatusCommand request, CancellationToken cancellationToken)
    {
        var risk = await _db.Risks.FirstOrDefaultAsync(r => r.Id == request.RiskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Risk {request.RiskId} not found.");

        risk.Status = request.Status;
        if (request.MitigationPlan is not null)
            risk.MitigationPlan = request.MitigationPlan;

        risk.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        risk.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
