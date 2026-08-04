using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.DocumentManagement.Commands.ApproveVersion;

/// <summary>
/// Approves a draft version in place: rather than creating a new row, this version's own
/// number bumps to the next major (e.g. 1.2 -> 2.0) and its status becomes Approved — matching
/// the versioning sequence in docs/erd.md (1.0 Draft, 1.1 Draft, 1.2 Draft, 2.0 Approved, ...).
/// Any previously Approved version of the same document moves to Superseded, since a document
/// can only have one current Approved version at a time.
/// </summary>
public record ApproveVersionCommand(Guid DocumentVersionId) : IRequest<Unit>;

public class ApproveVersionCommandHandler : IRequestHandler<ApproveVersionCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ApproveVersionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ApproveVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _db.DocumentVersions.FirstOrDefaultAsync(v => v.Id == request.DocumentVersionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document version {request.DocumentVersionId} not found.");

        if (version.Status is not (DocumentVersionStatus.Draft or DocumentVersionStatus.Review))
            throw new InvalidOperationException($"Version {version.VersionLabel} is {version.Status} and cannot be approved.");

        var actorId = _currentUser.UserId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        var previouslyApproved = await _db.DocumentVersions
            .Where(v => v.DocumentId == version.DocumentId && v.Status == DocumentVersionStatus.Approved)
            .ToListAsync(cancellationToken);

        foreach (var superseded in previouslyApproved)
        {
            superseded.Status = DocumentVersionStatus.Superseded;
            superseded.ModifiedBy = actorId;
            superseded.ModifiedDate = now;
        }

        version.MajorVersion += 1;
        version.MinorVersion = 0;
        version.Status = DocumentVersionStatus.Approved;
        version.ModifiedBy = actorId;
        version.ModifiedDate = now;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
