using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.DocumentManagement.Commands.CreateDocument;

/// <summary>Creates a new logical document and its first version — 1.0, Draft. Never
/// overwritten: every later revision or approval creates a new DocumentVersion row.</summary>
public record CreateDocumentCommand(Guid ProjectId, string Title, string Category, byte? RibaStageNumber) : IRequest<Guid>;

public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateDocumentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var document = new Document
        {
            ProjectId = request.ProjectId,
            Title = request.Title,
            Category = request.Category,
            RibaStageNumber = request.RibaStageNumber,
            CreatedBy = actorId
        };

        document.Versions.Add(new DocumentVersion
        {
            MajorVersion = 1,
            MinorVersion = 0,
            CreatedBy = actorId
        });

        _db.Documents.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        return document.Id;
    }
}
