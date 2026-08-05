using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.Projects.Commands.AdvanceRibaStage;
using SCPM.Application.Projects.Commands.CreateProject;
using SCPM.Application.Projects.Dtos;
using SCPM.Application.Projects.Queries.GetProjectById;
using SCPM.Application.Projects.Queries.GetProjects;

namespace SCPM.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ISender _mediator;

    public ProjectsController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<ProjectListItemDto>>> GetProjects(
        [FromQuery] string? status, [FromQuery] Guid? programmeId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetProjectsQuery(status, programmeId), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> GetProject(Guid id, CancellationToken ct)
    {
        var project = await _mediator.Send(new GetProjectByIdQuery(id), ct);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateProject(CreateProjectCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetProject), new { id }, id);
    }

    [HttpPost("{id:guid}/advance-stage")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> AdvanceStage(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new AdvanceRibaStageCommand(id), ct);
        return NoContent();
    }
}
