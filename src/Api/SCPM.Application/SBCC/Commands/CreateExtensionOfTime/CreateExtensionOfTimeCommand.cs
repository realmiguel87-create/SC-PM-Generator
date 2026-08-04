using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.SBCC.Commands.CreateExtensionOfTime;

public record CreateExtensionOfTimeCommand(Guid ProjectId, string Reference, string Reason, int DaysClaimed) : IRequest<Guid>;

public class CreateExtensionOfTimeCommandHandler : IRequestHandler<CreateExtensionOfTimeCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateExtensionOfTimeCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateExtensionOfTimeCommand request, CancellationToken cancellationToken)
    {
        var eot = new ExtensionOfTime
        {
            ProjectId = request.ProjectId,
            Reference = request.Reference,
            Reason = request.Reason,
            DaysClaimed = request.DaysClaimed,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.ExtensionsOfTime.Add(eot);
        await _db.SaveChangesAsync(cancellationToken);

        return eot.Id;
    }
}
