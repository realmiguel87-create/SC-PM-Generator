using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.StakeholderManagement.Commands.CreateEngagement;
using SCPM.Application.StakeholderManagement.Commands.CreateStakeholder;
using SCPM.Application.StakeholderManagement.Dtos;
using SCPM.Application.StakeholderManagement.Queries.GetStakeholders;
using SCPM.Domain.Enums;

namespace SCPM.Api.Controllers;

[ApiController]
[Authorize]
public class StakeholderController : ControllerBase
{
    private readonly ISender _mediator;

    public StakeholderController(ISender mediator) => _mediator = mediator;

    [HttpGet("api/projects/{projectId:guid}/stakeholders")]
    public async Task<ActionResult<List<StakeholderDto>>> GetStakeholders(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStakeholdersQuery(projectId), ct));

    public record CreateStakeholderRequest(
        string Name, string? Organisation, string? RoleTitle, string? ContactEmail,
        StakeholderInfluence Influence, StakeholderInterest Interest);

    [HttpPost("api/projects/{projectId:guid}/stakeholders")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateStakeholder(Guid projectId, CreateStakeholderRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateStakeholderCommand(
            projectId, request.Name, request.Organisation, request.RoleTitle, request.ContactEmail, request.Influence, request.Interest), ct);
        return Ok(id);
    }

    public record CreateEngagementRequest(DateOnly EngagementDate, string Method, string Summary, string? Outcome);

    [HttpPost("api/stakeholders/{stakeholderId:guid}/engagements")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateEngagement(Guid stakeholderId, CreateEngagementRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateEngagementCommand(
            stakeholderId, request.EngagementDate, request.Method, request.Summary, request.Outcome), ct);
        return Ok(id);
    }
}
