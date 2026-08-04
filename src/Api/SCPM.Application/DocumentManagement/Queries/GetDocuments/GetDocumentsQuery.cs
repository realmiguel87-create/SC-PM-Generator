using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.DocumentManagement.Dtos;

namespace SCPM.Application.DocumentManagement.Queries.GetDocuments;

public record GetDocumentsQuery(Guid ProjectId) : IRequest<List<DocumentListItemDto>>;

public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, List<DocumentListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetDocumentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<DocumentListItemDto>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        var documents = await _db.Documents
            .Include(d => d.Versions)
            .Where(d => d.ProjectId == request.ProjectId)
            .OrderBy(d => d.Title)
            .ToListAsync(cancellationToken);

        return documents.Select(d =>
        {
            var latest = d.Versions.OrderByDescending(v => v.MajorVersion).ThenByDescending(v => v.MinorVersion).First();
            return new DocumentListItemDto
            {
                Id = d.Id,
                Title = d.Title,
                Category = d.Category,
                RibaStageNumber = d.RibaStageNumber,
                LatestVersionLabel = latest.VersionLabel,
                LatestVersionStatus = latest.Status.ToString()
            };
        }).ToList();
    }
}
