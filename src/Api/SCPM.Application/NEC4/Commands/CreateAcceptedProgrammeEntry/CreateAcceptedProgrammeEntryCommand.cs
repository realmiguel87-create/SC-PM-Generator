using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.NEC4.Commands.CreateAcceptedProgrammeEntry;

public record CreateAcceptedProgrammeEntryCommand(Guid ProjectId, int RevisionNumber, DateOnly AcceptedDate, string? Notes) : IRequest<Guid>;

public class CreateAcceptedProgrammeEntryCommandHandler : IRequestHandler<CreateAcceptedProgrammeEntryCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateAcceptedProgrammeEntryCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateAcceptedProgrammeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = new AcceptedProgrammeEntry
        {
            ProjectId = request.ProjectId,
            RevisionNumber = request.RevisionNumber,
            AcceptedDate = request.AcceptedDate,
            Notes = request.Notes,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.AcceptedProgrammeEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return entry.Id;
    }
}
