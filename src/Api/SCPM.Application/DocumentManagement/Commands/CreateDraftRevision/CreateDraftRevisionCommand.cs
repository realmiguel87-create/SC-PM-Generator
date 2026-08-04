using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.DocumentManagement.Commands.CreateDraftRevision;

/// <summary>Adds a new Draft revision within the current major version (e.g. 1.1 -> 1.2).
/// To move to the next major/Approved version use ApproveVersionCommand instead.</summary>
public record CreateDraftRevisionCommand(Guid DocumentId) : IRequest<Guid>;

public class CreateDraftRevisionCommandHandler : IRequestHandler<CreateDraftRevisionCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateDraftRevisionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateDraftRevisionCommand request, CancellationToken cancellationToken)
    {
        var latest = await _db.DocumentVersions
            .Where(v => v.DocumentId == request.DocumentId)
            .OrderByDescending(v => v.MajorVersion).ThenByDescending(v => v.MinorVersion)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Document {request.DocumentId} has no versions.");

        var revision = new DocumentVersion
        {
            DocumentId = request.DocumentId,
            MajorVersion = latest.MajorVersion,
            MinorVersion = latest.MinorVersion + 1,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.DocumentVersions.Add(revision);
        await _db.SaveChangesAsync(cancellationToken);

        return revision.Id;
    }
}
