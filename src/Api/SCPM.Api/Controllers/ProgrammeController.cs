using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.ProgrammeManagement.Commands.CreateMilestone;
using SCPM.Application.ProgrammeManagement.Commands.RebaselineProgramme;
using SCPM.Application.ProgrammeManagement.Commands.UpdateMilestoneStatus;
using SCPM.Application.ProgrammeManagement.Dtos;
using SCPM.Application.ProgrammeManagement.Queries.GetMilestones;
using SCPM.Application.ProgrammeManagement.Queries.GetProgrammeAgainstBaseline;
using SCPM.Application.ProgrammeManagement.Queries.GetProgrammeBaselines;
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

    [HttpGet("api/projects/{projectId:guid}/baselines")]
    public async Task<ActionResult<List<ProgrammeBaselineDto>>> GetBaselines(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetProgrammeBaselinesQuery(projectId), ct));

    /// <summary>
    /// The programme measured against a baseline — the current one when <c>baselineId</c> is
    /// omitted. 404 when the project has never been baselined, which is a different state from a
    /// baseline against which nothing has slipped.
    /// </summary>
    [HttpGet("api/projects/{projectId:guid}/baseline-comparison")]
    public async Task<ActionResult<ProgrammeAgainstBaselineDto>> GetAgainstBaseline(
        Guid projectId, [FromQuery] Guid? baselineId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProgrammeAgainstBaselineQuery(projectId, baselineId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    public record RebaselineProgrammeRequest(string Name, string Reason, Guid? ApprovedBy, DateOnly? ApprovedDate);

    /// <summary>
    /// Rebaselines the programme. Requires approval rights rather than write rights: this changes
    /// the measure the project is judged against, which is a governance act rather than an edit.
    /// </summary>
    [HttpPost("api/projects/{projectId:guid}/baselines")]
    [Authorize(Policy = "CanApprove")]
    public async Task<ActionResult<Guid>> Rebaseline(
        Guid projectId, RebaselineProgrammeRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new RebaselineProgrammeCommand(
            projectId, request.Name, request.Reason, request.ApprovedBy, request.ApprovedDate), ct);
        return Ok(id);
    }
}
