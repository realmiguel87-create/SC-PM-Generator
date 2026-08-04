using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.RiskManagement.Commands.CreateEscalation;
using SCPM.Application.RiskManagement.Commands.CreateIssue;
using SCPM.Application.RiskManagement.Commands.CreateOpportunity;
using SCPM.Application.RiskManagement.Commands.CreateRisk;
using SCPM.Application.RiskManagement.Commands.ResolveEscalation;
using SCPM.Application.RiskManagement.Commands.UpdateIssueStatus;
using SCPM.Application.RiskManagement.Commands.UpdateOpportunityStatus;
using SCPM.Application.RiskManagement.Commands.UpdateRiskStatus;
using SCPM.Application.RiskManagement.Dtos;
using SCPM.Application.RiskManagement.Queries.GetEscalations;
using SCPM.Application.RiskManagement.Queries.GetIssues;
using SCPM.Application.RiskManagement.Queries.GetOpportunities;
using SCPM.Application.RiskManagement.Queries.GetRisks;
using SCPM.Domain.Enums;

namespace SCPM.Api.Controllers;

[ApiController]
[Authorize]
public class RiskController : ControllerBase
{
    private readonly ISender _mediator;

    public RiskController(ISender mediator) => _mediator = mediator;

    // --- Risks ---

    [HttpGet("api/projects/{projectId:guid}/risks")]
    public async Task<ActionResult<List<RiskDto>>> GetRisks(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetRisksQuery(projectId), ct));

    public record CreateRiskRequest(string Title, string? Description, string Category, int Probability, int Impact, string? MitigationPlan);

    [HttpPost("api/projects/{projectId:guid}/risks")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateRisk(Guid projectId, CreateRiskRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateRiskCommand(
            projectId, request.Title, request.Description, request.Category, request.Probability, request.Impact, request.MitigationPlan), ct);
        return Ok(id);
    }

    public record UpdateRiskStatusRequest(RiskStatus Status, string? MitigationPlan);

    [HttpPut("api/risks/{riskId:guid}/status")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> UpdateRiskStatus(Guid riskId, UpdateRiskStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateRiskStatusCommand(riskId, request.Status, request.MitigationPlan), ct);
        return NoContent();
    }

    // --- Issues ---

    [HttpGet("api/projects/{projectId:guid}/issues")]
    public async Task<ActionResult<List<IssueDto>>> GetIssues(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetIssuesQuery(projectId), ct));

    public record CreateIssueRequest(string Title, string? Description, IssueSeverity Severity, DateOnly RaisedDate);

    [HttpPost("api/projects/{projectId:guid}/issues")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateIssue(Guid projectId, CreateIssueRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateIssueCommand(projectId, request.Title, request.Description, request.Severity, request.RaisedDate), ct);
        return Ok(id);
    }

    public record UpdateIssueStatusRequest(IssueStatus Status, string? ResolutionNotes);

    [HttpPut("api/issues/{issueId:guid}/status")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> UpdateIssueStatus(Guid issueId, UpdateIssueStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateIssueStatusCommand(issueId, request.Status, request.ResolutionNotes), ct);
        return NoContent();
    }

    // --- Opportunities ---

    [HttpGet("api/projects/{projectId:guid}/opportunities")]
    public async Task<ActionResult<List<OpportunityDto>>> GetOpportunities(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetOpportunitiesQuery(projectId), ct));

    public record CreateOpportunityRequest(string Title, string? Description, decimal PotentialValue, int Probability);

    [HttpPost("api/projects/{projectId:guid}/opportunities")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateOpportunity(Guid projectId, CreateOpportunityRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateOpportunityCommand(projectId, request.Title, request.Description, request.PotentialValue, request.Probability), ct);
        return Ok(id);
    }

    public record UpdateOpportunityStatusRequest(OpportunityStatus Status);

    [HttpPut("api/opportunities/{opportunityId:guid}/status")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> UpdateOpportunityStatus(Guid opportunityId, UpdateOpportunityStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateOpportunityStatusCommand(opportunityId, request.Status), ct);
        return NoContent();
    }

    // --- Escalations ---

    [HttpGet("api/projects/{projectId:guid}/escalations")]
    public async Task<ActionResult<List<EscalationDto>>> GetEscalations(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetEscalationsQuery(projectId), ct));

    public record CreateEscalationRequest(Guid? RiskId, Guid? IssueId, string Reason);

    [HttpPost("api/projects/{projectId:guid}/escalations")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateEscalation(Guid projectId, CreateEscalationRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateEscalationCommand(projectId, request.RiskId, request.IssueId, request.Reason), ct);
        return Ok(id);
    }

    public record ResolveEscalationRequest(string ResolutionNotes);

    [HttpPut("api/escalations/{escalationId:guid}/resolve")]
    [Authorize(Policy = "CanApprove")]
    public async Task<IActionResult> ResolveEscalation(Guid escalationId, ResolveEscalationRequest request, CancellationToken ct)
    {
        await _mediator.Send(new ResolveEscalationCommand(escalationId, request.ResolutionNotes), ct);
        return NoContent();
    }
}
