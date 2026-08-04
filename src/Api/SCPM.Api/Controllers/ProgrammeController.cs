using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.Programme.Commands.CreateMilestone;
using SCPM.Application.Programme.Commands.UpdateMilestoneStatus;
using SCPM.Application.Programme.Dtos;
using SCPM.Application.Programme.Queries.GetMilestones;
using SCPM.Domain.Enums;

namespace SCPM.Api.Controllers;

[ApiController]
[Authorize]
public class ProgrammeController : ControllerBase
{
    private readonly ISender _mediator;

    public ProgrammeController(ISender mediator) => _mediator = mediator;

    [HttpGet("api/projects/{projectId:guid}/milestones")]
    public async Task<ActionResult<List<MilestoneDto>>> GetMilestones(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetMilestonesQuery(projectId), ct));

    public record CreateMilestoneRequest(string Name, string? Description, DateOnly BaselineDate, DateOnly ForecastDate, bool IsKeyMilestone);

    [HttpPost("api/projects/{projectId:guid}/milestones")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateMilestone(Guid projectId, CreateMilestoneRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateMilestoneCommand(
            projectId, request.Name, request.Description, request.BaselineDate, request.ForecastDate, request.IsKeyMilestone), ct);
        return Ok(id);
    }

    public record UpdateMilestoneStatusRequest(MilestoneStatus Status, DateOnly? ActualDate);

    [HttpPut("api/milestones/{milestoneId:guid}/status")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> UpdateMilestoneStatus(Guid milestoneId, UpdateMilestoneStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateMilestoneStatusCommand(milestoneId, request.Status, request.ActualDate), ct);
        return NoContent();
    }
}
