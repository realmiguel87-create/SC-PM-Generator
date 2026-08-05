using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.SBCC.Commands.CreateLossAndExpenseClaim;

public record CreateLossAndExpenseClaimCommand(Guid ProjectId, string Reference, string Description, decimal ClaimedAmount) : IRequest<Guid>;

public class CreateLossAndExpenseClaimCommandHandler : IRequestHandler<CreateLossAndExpenseClaimCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateLossAndExpenseClaimCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateLossAndExpenseClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = new LossAndExpenseClaim
        {
            ProjectId = request.ProjectId,
            Reference = request.Reference,
            Description = request.Description,
            ClaimedAmount = request.ClaimedAmount,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.LossAndExpenseClaims.Add(claim);
        await _db.SaveChangesAsync(cancellationToken);

        return claim.Id;
    }
}
