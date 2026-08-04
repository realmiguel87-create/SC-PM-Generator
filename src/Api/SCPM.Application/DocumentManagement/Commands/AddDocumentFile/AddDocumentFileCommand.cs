using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.DocumentManagement.Commands.AddDocumentFile;

/// <summary>Uploads a physical file to SharePoint and records it against a document version.
/// Every call creates a new DocumentFile row — an existing file for that version/type is never
/// overwritten, matching the "never overwrite files" rule.</summary>
public record AddDocumentFileCommand(
    Guid DocumentVersionId, string FileType, string Category, string FileName, Stream Content, string ContentType) : IRequest<Guid>;

public class AddDocumentFileCommandHandler : IRequestHandler<AddDocumentFileCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ISharePointDocumentStore _sharePointStore;
    private readonly ICurrentUserService _currentUser;

    public AddDocumentFileCommandHandler(IAppDbContext db, ISharePointDocumentStore sharePointStore, ICurrentUserService currentUser)
    {
        _db = db;
        _sharePointStore = sharePointStore;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(AddDocumentFileCommand request, CancellationToken cancellationToken)
    {
        var version = await _db.DocumentVersions
            .Include(v => v.Document)
            .ThenInclude(d => d.Project)
            .FirstOrDefaultAsync(v => v.Id == request.DocumentVersionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document version {request.DocumentVersionId} not found.");

        var sizeBytes = request.Content.CanSeek ? request.Content.Length : 0;

        var sharePointUrl = await _sharePointStore.UploadAsync(
            version.Document.Project.ProjectRef, request.FileName, request.Content, request.ContentType, cancellationToken);

        var file = new DocumentFile
        {
            DocumentVersionId = request.DocumentVersionId,
            FileType = request.FileType,
            Category = request.Category,
            FileName = request.FileName,
            SharePointUrl = sharePointUrl,
            SizeBytes = sizeBytes,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.DocumentFiles.Add(file);
        await _db.SaveChangesAsync(cancellationToken);

        return file.Id;
    }
}
