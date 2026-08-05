using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.NEC4.Commands.CreateChangeRegisterItem;

public record CreateChangeRegisterItemCommand(Guid ProjectId, string Title, string? Description, decimal ValueImpact, int TimeImpactDays) : IRequest<Guid>;

public class CreateChangeRegisterItemCommandHandler : IRequestHandler<CreateChangeRegisterItemCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateChangeRegisterItemCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateChangeRegisterItemCommand request, CancellationToken cancellationToken)
    {
        var item = new ChangeRegisterItem
        {
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            ValueImpact = request.ValueImpact,
            TimeImpactDays = request.TimeImpactDays,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.ChangeRegisterItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}
