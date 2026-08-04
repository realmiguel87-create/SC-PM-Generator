using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.DocumentManagement.Dtos;

namespace SCPM.Application.DocumentManagement.Queries.GetDocumentVersions;

public record GetDocumentVersionsQuery(Guid DocumentId) : IRequest<DocumentDetailDto?>;

public class GetDocumentVersionsQueryHandler : IRequestHandler<GetDocumentVersionsQuery, DocumentDetailDto?>
{
    private readonly IAppDbContext _db;

    public GetDocumentVersionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<DocumentDetailDto?> Handle(GetDocumentVersionsQuery request, CancellationToken cancellationToken)
    {
        var document = await _db.Documents
            .Include(d => d.Versions).ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken);

        if (document is null)
            return null;

        var orderedVersions = document.Versions
            .OrderByDescending(v => v.MajorVersion).ThenByDescending(v => v.MinorVersion)
            .ToList();
        var latest = orderedVersions.First();

        return new DocumentDetailDto
        {
            Id = document.Id,
            Title = document.Title,
            Category = document.Category,
            RibaStageNumber = document.RibaStageNumber,
            LatestVersionLabel = latest.VersionLabel,
            LatestVersionStatus = latest.Status.ToString(),
            Versions = orderedVersions.Select(v => new DocumentVersionDto
            {
                Id = v.Id,
                VersionLabel = v.VersionLabel,
                Status = v.Status.ToString(),
                CreatedDate = v.CreatedDate,
                Files = v.Files.Select(f => new DocumentFileDto
                {
                    Id = f.Id,
                    FileType = f.FileType,
                    Category = f.Category,
                    FileName = f.FileName,
                    SharePointUrl = f.SharePointUrl,
                    BlobArchiveUrl = f.BlobArchiveUrl,
                    SizeBytes = f.SizeBytes,
                    CreatedDate = f.CreatedDate
                }).ToList()
            }).ToList()
        };
    }
}
