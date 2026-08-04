using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.SBCC.Commands.CreateInterimValuation;

public record CreateInterimValuationCommand(Guid ProjectId, int ValuationNumber, DateOnly ValuationDate, decimal GrossValuation, decimal NetPayment) : IRequest<Guid>;

public class CreateInterimValuationCommandHandler : IRequestHandler<CreateInterimValuationCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateInterimValuationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateInterimValuationCommand request, CancellationToken cancellationToken)
    {
        var valuation = new InterimValuation
        {
            ProjectId = request.ProjectId,
            ValuationNumber = request.ValuationNumber,
            ValuationDate = request.ValuationDate,
            GrossValuation = request.GrossValuation,
            NetPayment = request.NetPayment,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.InterimValuations.Add(valuation);
        await _db.SaveChangesAsync(cancellationToken);

        return valuation.Id;
    }
}
