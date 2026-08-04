using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.Reporting.Commands.CreateSnapshot;
using SCPM.Application.Reporting.Dtos;
using SCPM.Application.Reporting.Queries.GetSnapshots;
using SCPM.Domain.Enums;

namespace SCPM.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/snapshots")]
[Authorize]
public class ReportingController : ControllerBase
{
    private readonly ISender _mediator;

    public ReportingController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<SnapshotDto>>> GetSnapshots(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetSnapshotsQuery(projectId), ct));

    public record CreateManualSnapshotRequest(string Label);

    [HttpPost]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateManualSnapshot(Guid projectId, CreateManualSnapshotRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateSnapshotCommand(projectId, SnapshotType.Manual, request.Label), ct);
        return Ok(id);
    }
}
