using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.RiskManagement.Commands.UpdateOpportunityStatus;

public record UpdateOpportunityStatusCommand(Guid OpportunityId, OpportunityStatus Status) : IRequest<Unit>;

public class UpdateOpportunityStatusCommandHandler : IRequestHandler<UpdateOpportunityStatusCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateOpportunityStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateOpportunityStatusCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await _db.Opportunities.FirstOrDefaultAsync(o => o.Id == request.OpportunityId, cancellationToken)
            ?? throw new KeyNotFoundException($"Opportunity {request.OpportunityId} not found.");

        opportunity.Status = request.Status;
        opportunity.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        opportunity.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
