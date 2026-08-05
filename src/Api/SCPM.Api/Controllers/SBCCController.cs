using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.SBCC.Commands.CreateArchitectsInstruction;
using SCPM.Application.SBCC.Commands.CreateExtensionOfTime;
using SCPM.Application.SBCC.Commands.CreateInterimValuation;
using SCPM.Application.SBCC.Commands.CreateLossAndExpenseClaim;
using SCPM.Application.SBCC.Commands.CreateVariation;
using SCPM.Application.SBCC.Commands.UpdateExtensionOfTimeStatus;
using SCPM.Application.SBCC.Commands.UpdateVariationStatus;
using SCPM.Application.SBCC.Dtos;
using SCPM.Application.SBCC.Queries.GetArchitectsInstructions;
using SCPM.Application.SBCC.Queries.GetExtensionsOfTime;
using SCPM.Application.SBCC.Queries.GetInterimValuations;
using SCPM.Application.SBCC.Queries.GetLossAndExpenseClaims;
using SCPM.Application.SBCC.Queries.GetVariations;
using SCPM.Domain.Enums;

namespace SCPM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}")]
public class SBCCController : ControllerBase
{
    private readonly ISender _mediator;

    public SBCCController(ISender mediator) => _mediator = mediator;

    // --- Variations ---

    [HttpGet("sbcc/variations")]
    public async Task<ActionResult<List<VariationDto>>> GetVariations(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetVariationsQuery(projectId), ct));

    public record CreateVariationRequest(string Reference, string Description, decimal ValueImpact);

    [HttpPost("sbcc/variations")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateVariation(Guid projectId, CreateVariationRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateVariationCommand(projectId, request.Reference, request.Description, request.ValueImpact), ct));

    public record UpdateVariationStatusRequest(VariationStatus Status);

    [HttpPut("~/api/sbcc/variations/{variationId:guid}/status")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> UpdateVariationStatus(Guid variationId, UpdateVariationStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateVariationStatusCommand(variationId, request.Status), ct);
        return NoContent();
    }

    // --- Extensions of Time ---

    [HttpGet("sbcc/extensions-of-time")]
    public async Task<ActionResult<List<ExtensionOfTimeDto>>> GetExtensionsOfTime(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetExtensionsOfTimeQuery(projectId), ct));

    public record CreateExtensionOfTimeRequest(string Reference, string Reason, int DaysClaimed);

    [HttpPost("sbcc/extensions-of-time")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateExtensionOfTime(Guid projectId, CreateExtensionOfTimeRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateExtensionOfTimeCommand(projectId, request.Reference, request.Reason, request.DaysClaimed), ct));

    public record UpdateExtensionOfTimeStatusRequest(ExtensionOfTimeStatus Status, int? DaysAwarded);

    [HttpPut("~/api/sbcc/extensions-of-time/{extensionOfTimeId:guid}/status")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> UpdateExtensionOfTimeStatus(Guid extensionOfTimeId, UpdateExtensionOfTimeStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateExtensionOfTimeStatusCommand(extensionOfTimeId, request.Status, request.DaysAwarded), ct);
        return NoContent();
    }

    // --- Loss & Expense ---

    [HttpGet("sbcc/loss-and-expense")]
    public async Task<ActionResult<List<LossAndExpenseClaimDto>>> GetLossAndExpenseClaims(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetLossAndExpenseClaimsQuery(projectId), ct));

    public record CreateLossAndExpenseClaimRequest(string Reference, string Description, decimal ClaimedAmount);

    [HttpPost("sbcc/loss-and-expense")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateLossAndExpenseClaim(Guid projectId, CreateLossAndExpenseClaimRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateLossAndExpenseClaimCommand(projectId, request.Reference, request.Description, request.ClaimedAmount), ct));

    // --- Architect's Instructions ---

    [HttpGet("sbcc/architects-instructions")]
    public async Task<ActionResult<List<ArchitectsInstructionDto>>> GetArchitectsInstructions(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetArchitectsInstructionsQuery(projectId), ct));

    public record CreateArchitectsInstructionRequest(int InstructionNumber, string Description, DateOnly IssuedDate);

    [HttpPost("sbcc/architects-instructions")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateArchitectsInstruction(Guid projectId, CreateArchitectsInstructionRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateArchitectsInstructionCommand(projectId, request.InstructionNumber, request.Description, request.IssuedDate), ct));

    // --- Interim Valuations ---

    [HttpGet("sbcc/interim-valuations")]
    public async Task<ActionResult<List<InterimValuationDto>>> GetInterimValuations(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetInterimValuationsQuery(projectId), ct));

    public record CreateInterimValuationRequest(int ValuationNumber, DateOnly ValuationDate, decimal GrossValuation, decimal NetPayment);

    [HttpPost("sbcc/interim-valuations")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateInterimValuation(Guid projectId, CreateInterimValuationRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateInterimValuationCommand(
            projectId, request.ValuationNumber, request.ValuationDate, request.GrossValuation, request.NetPayment), ct));
}
