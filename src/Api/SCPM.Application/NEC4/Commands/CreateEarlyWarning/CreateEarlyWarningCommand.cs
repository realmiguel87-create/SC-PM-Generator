using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.NEC4.Commands.CreateEarlyWarning;

public record CreateEarlyWarningCommand(Guid ProjectId, string Title, string? Description, DateOnly RaisedDate, string? MitigationAction) : IRequest<Guid>;

public class CreateEarlyWarningCommandHandler : IRequestHandler<CreateEarlyWarningCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateEarlyWarningCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateEarlyWarningCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var earlyWarning = new EarlyWarning
        {
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            RaisedDate = request.RaisedDate,
            MitigationAction = request.MitigationAction,
            RaisedByUserId = actorId,
            CreatedBy = actorId
        };

        _db.EarlyWarnings.Add(earlyWarning);
        await _db.SaveChangesAsync(cancellationToken);

        return earlyWarning.Id;
    }
}
