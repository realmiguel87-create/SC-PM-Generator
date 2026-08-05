using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.NEC4.Commands.CreateRiskAllocationItem;

public record CreateRiskAllocationItemCommand(Guid ProjectId, string Description, RiskAllocationParty AllocatedTo, string? MitigationOwner) : IRequest<Guid>;

public class CreateRiskAllocationItemCommandHandler : IRequestHandler<CreateRiskAllocationItemCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateRiskAllocationItemCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateRiskAllocationItemCommand request, CancellationToken cancellationToken)
    {
        var item = new RiskAllocationItem
        {
            ProjectId = request.ProjectId,
            Description = request.Description,
            AllocatedTo = request.AllocatedTo,
            MitigationOwner = request.MitigationOwner,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.RiskAllocationItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}
