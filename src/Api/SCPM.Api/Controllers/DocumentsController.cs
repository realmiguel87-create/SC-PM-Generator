using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.DocumentManagement.Commands.AddDocumentFile;
using SCPM.Application.DocumentManagement.Commands.ApproveVersion;
using SCPM.Application.DocumentManagement.Commands.ArchiveVersion;
using SCPM.Application.DocumentManagement.Commands.CreateDocument;
using SCPM.Application.DocumentManagement.Commands.CreateDraftRevision;
using SCPM.Application.DocumentManagement.Commands.RejectVersion;
using SCPM.Application.DocumentManagement.Dtos;
using SCPM.Application.DocumentManagement.Queries.GetDocuments;
using SCPM.Application.DocumentManagement.Queries.GetDocumentVersions;

namespace SCPM.Api.Controllers;

[ApiController]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly ISender _mediator;

    public DocumentsController(ISender mediator) => _mediator = mediator;

    [HttpGet("api/projects/{projectId:guid}/documents")]
    public async Task<ActionResult<List<DocumentListItemDto>>> GetDocuments(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetDocumentsQuery(projectId), ct));

    [HttpGet("api/documents/{documentId:guid}")]
    public async Task<ActionResult<DocumentDetailDto>> GetDocument(Guid documentId, CancellationToken ct)
    {
        var document = await _mediator.Send(new GetDocumentVersionsQuery(documentId), ct);
        return document is null ? NotFound() : Ok(document);
    }

    public record CreateDocumentRequest(string Title, string Category, byte? RibaStageNumber);

    [HttpPost("api/projects/{projectId:guid}/documents")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateDocument(Guid projectId, CreateDocumentRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateDocumentCommand(projectId, request.Title, request.Category, request.RibaStageNumber), ct));

    [HttpPost("api/documents/{documentId:guid}/revisions")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateDraftRevision(Guid documentId, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateDraftRevisionCommand(documentId), ct));

    [HttpPut("api/document-versions/{documentVersionId:guid}/approve")]
    [Authorize(Policy = "CanApprove")]
    public async Task<IActionResult> ApproveVersion(Guid documentVersionId, CancellationToken ct)
    {
        await _mediator.Send(new ApproveVersionCommand(documentVersionId), ct);
        return NoContent();
    }

    [HttpPut("api/document-versions/{documentVersionId:guid}/reject")]
    [Authorize(Policy = "CanApprove")]
    public async Task<IActionResult> RejectVersion(Guid documentVersionId, CancellationToken ct)
    {
        await _mediator.Send(new RejectVersionCommand(documentVersionId), ct);
        return NoContent();
    }

    [HttpPut("api/document-versions/{documentVersionId:guid}/archive")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> ArchiveVersion(Guid documentVersionId, CancellationToken ct)
    {
        await _mediator.Send(new ArchiveVersionCommand(documentVersionId), ct);
        return NoContent();
    }

    /// <summary>Multipart upload — the file goes to SharePoint (ISharePointDocumentStore), only
    /// its metadata and resulting URL are persisted in SQL.</summary>
    [HttpPost("api/document-versions/{documentVersionId:guid}/files")]
    [Authorize(Policy = "CanWrite")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<Guid>> AddDocumentFile(
        Guid documentVersionId, [FromForm] string fileType, [FromForm] string category, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var id = await _mediator.Send(
            new AddDocumentFileCommand(documentVersionId, fileType, category, file.FileName, stream, file.ContentType), ct);
        return Ok(id);
    }
}
