using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.Governance.Commands.CreateDecision;
using SCPM.Application.Governance.Commands.CreateGateway;
using SCPM.Application.Governance.Commands.DecideGateway;
using SCPM.Application.Governance.Dtos;
using SCPM.Application.Governance.Queries.GetDecisions;

namespace SCPM.Api.Controllers;

[ApiController]
[Authorize]
public class GovernanceController : ControllerBase
{
    private readonly ISender _mediator;

    public GovernanceController(ISender mediator) => _mediator = mediator;

    public record CreateGatewayRequest(string GatewayType, DateOnly? DueDate);

    /// <summary>Opens a stage-gate approval request. Required before a project can advance past this stage.</summary>
    [HttpPost("api/projects/{projectId:guid}/stages/{stageNumber}/gateway")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateGateway(Guid projectId, byte stageNumber, CreateGatewayRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateGatewayCommand(projectId, stageNumber, request.GatewayType, request.DueDate), ct);
        return Ok(id);
    }

    public record DecideGatewayRequest(SCPM.Domain.Enums.ApprovalDecision Decision, string? Comments);

    /// <summary>Records an approver's decision. Approving moves the stage to Gated, unblocking AdvanceRibaStage.</summary>
    [HttpPost("api/gateways/{gatewayId:guid}/decision")]
    [Authorize(Policy = "CanApprove")]
    public async Task<IActionResult> DecideGateway(Guid gatewayId, DecideGatewayRequest request, CancellationToken ct)
    {
        await _mediator.Send(new DecideGatewayCommand(gatewayId, request.Decision, request.Comments), ct);
        return NoContent();
    }

    [HttpGet("api/projects/{projectId:guid}/decisions")]
    public async Task<ActionResult<List<DecisionDto>>> GetDecisions(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetDecisionsQuery(projectId), ct));

    public record CreateDecisionRequest(string Title, string Description, DateOnly DecisionDate, string? Rationale);

    [HttpPost("api/projects/{projectId:guid}/decisions")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateDecision(Guid projectId, CreateDecisionRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(
            new CreateDecisionCommand(projectId, request.Title, request.Description, request.DecisionDate, request.Rationale), ct);
        return Ok(id);
    }
}
