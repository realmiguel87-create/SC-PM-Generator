using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.Cost.Commands.CreateCostPlan;
using SCPM.Application.Cost.Commands.RecordForecast;
using SCPM.Application.Cost.Dtos;
using SCPM.Application.Cost.Queries.GetCostSummary;

namespace SCPM.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/cost")]
[Authorize]
public class CostController : ControllerBase
{
    private readonly ISender _mediator;

    public CostController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<CostSummaryDto>> GetSummary(Guid projectId, CancellationToken ct)
    {
        var summary = await _mediator.Send(new GetCostSummaryQuery(projectId), ct);
        return summary is null ? NotFound() : Ok(summary);
    }

    public record CreateCostPlanRequest(string Name, bool IsBaseline, List<CreateCostPlanLineInput> Lines);

    [HttpPost("plans")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateCostPlan(Guid projectId, CreateCostPlanRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateCostPlanCommand(projectId, request.Name, request.IsBaseline, request.Lines), ct);
        return Ok(id);
    }

    public record RecordForecastRequest(DateOnly ForecastDate, decimal ForecastCost, string? CommentaryNotes);

    [HttpPost("forecasts")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> RecordForecast(Guid projectId, RecordForecastRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(
            new RecordForecastCommand(projectId, request.ForecastDate, request.ForecastCost, request.CommentaryNotes), ct);
        return Ok(id);
    }
}
