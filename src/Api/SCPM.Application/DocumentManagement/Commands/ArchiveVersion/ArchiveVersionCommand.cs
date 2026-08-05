using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.DocumentManagement.Commands.ArchiveVersion;

/// <summary>Moves a version's files to the Blob archive tier and marks it Archived. Only
/// Superseded or Rejected versions are eligible — the current Approved/Draft version of a
/// document stays in SharePoint where people are actually working with it.</summary>
public record ArchiveVersionCommand(Guid DocumentVersionId) : IRequest<Unit>;

public class ArchiveVersionCommandHandler : IRequestHandler<ArchiveVersionCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IBlobArchiveStore _blobArchiveStore;
    private readonly ICurrentUserService _currentUser;

    public ArchiveVersionCommandHandler(IAppDbContext db, IBlobArchiveStore blobArchiveStore, ICurrentUserService currentUser)
    {
        _db = db;
        _blobArchiveStore = blobArchiveStore;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ArchiveVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _db.DocumentVersions
            .Include(v => v.Files)
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.Id == request.DocumentVersionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document version {request.DocumentVersionId} not found.");

        if (version.Status is not (DocumentVersionStatus.Superseded or DocumentVersionStatus.Rejected))
            throw new InvalidOperationException(
                $"Version {version.VersionLabel} is {version.Status}; only Superseded or Rejected versions can be archived.");

        var actorId = _currentUser.UserId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        foreach (var file in version.Files.Where(f => f.SharePointUrl is not null && f.BlobArchiveUrl is null))
        {
            var blobPath = $"{version.Document.ProjectId}/{version.DocumentId}/{version.VersionLabel}/{file.FileName}";
            file.BlobArchiveUrl = await _blobArchiveStore.ArchiveAsync(file.SharePointUrl!, blobPath, cancellationToken);
        }

        version.Status = DocumentVersionStatus.Archived;
        version.ModifiedBy = actorId;
        version.ModifiedDate = now;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
