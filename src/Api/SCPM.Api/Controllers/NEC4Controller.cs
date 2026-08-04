using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.NEC4.Commands.CloseEarlyWarning;
using SCPM.Application.NEC4.Commands.CreateAcceptedProgrammeEntry;
using SCPM.Application.NEC4.Commands.CreateChangeRegisterItem;
using SCPM.Application.NEC4.Commands.CreateCompensationEvent;
using SCPM.Application.NEC4.Commands.CreateContractDataEntry;
using SCPM.Application.NEC4.Commands.CreateEarlyWarning;
using SCPM.Application.NEC4.Commands.CreatePaymentAssessment;
using SCPM.Application.NEC4.Commands.CreateRiskAllocationItem;
using SCPM.Application.NEC4.Commands.UpdateChangeRegisterItemStatus;
using SCPM.Application.NEC4.Commands.UpdateCompensationEventStatus;
using SCPM.Application.NEC4.Commands.UpdatePaymentAssessmentStatus;
using SCPM.Application.NEC4.Dtos;
using SCPM.Application.NEC4.Queries.GetAcceptedProgrammeEntries;
using SCPM.Application.NEC4.Queries.GetChangeRegisterItems;
using SCPM.Application.NEC4.Queries.GetCompensationEvents;
using SCPM.Application.NEC4.Queries.GetContractDataEntries;
using SCPM.Application.NEC4.Queries.GetEarlyWarnings;
using SCPM.Application.NEC4.Queries.GetPaymentAssessments;
using SCPM.Application.NEC4.Queries.GetRiskAllocationItems;
using SCPM.Domain.Enums;

namespace SCPM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}")]
public class NEC4Controller : ControllerBase
{
    private readonly ISender _mediator;

    public NEC4Controller(ISender mediator) => _mediator = mediator;

    // --- Early Warnings ---

    [HttpGet("nec4/early-warnings")]
    public async Task<ActionResult<List<EarlyWarningDto>>> GetEarlyWarnings(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetEarlyWarningsQuery(projectId), ct));

    public record CreateEarlyWarningRequest(string Title, string? Description, DateOnly RaisedDate, string? MitigationAction);

    [HttpPost("nec4/early-warnings")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateEarlyWarning(Guid projectId, CreateEarlyWarningRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateEarlyWarningCommand(projectId, request.Title, request.Description, request.RaisedDate, request.MitigationAction), ct));

    [HttpPut("~/api/nec4/early-warnings/{earlyWarningId:guid}/close")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> CloseEarlyWarning(Guid earlyWarningId, CancellationToken ct)
    {
        await _mediator.Send(new CloseEarlyWarningCommand(earlyWarningId), ct);
        return NoContent();
    }

    // --- Compensation Events ---

    [HttpGet("nec4/compensation-events")]
    public async Task<ActionResult<List<CompensationEventDto>>> GetCompensationEvents(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCompensationEventsQuery(projectId), ct));

    public record CreateCompensationEventRequest(string Reference, string Title, string? ClauseReference, decimal EstimatedValue, DateOnly NotifiedDate);

    [HttpPost("nec4/compensation-events")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateCompensationEvent(Guid projectId, CreateCompensationEventRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateCompensationEventCommand(
            projectId, request.Reference, request.Title, request.ClauseReference, request.EstimatedValue, request.NotifiedDate), ct));

    public record UpdateCompensationEventStatusRequest(CompensationEventStatus Status);

    [HttpPut("~/api/nec4/compensation-events/{compensationEventId:guid}/status")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> UpdateCompensationEventStatus(Guid compensationEventId, UpdateCompensationEventStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateCompensationEventStatusCommand(compensationEventId, request.Status), ct);
        return NoContent();
    }

    // --- Contract Data ---

    [HttpGet("nec4/contract-data")]
    public async Task<ActionResult<List<ContractDataEntryDto>>> GetContractDataEntries(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetContractDataEntriesQuery(projectId), ct));

    public record CreateContractDataEntryRequest(ContractDataPart Part, string ClauseReference, string Description, string Value);

    [HttpPost("nec4/contract-data")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateContractDataEntry(Guid projectId, CreateContractDataEntryRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateContractDataEntryCommand(projectId, request.Part, request.ClauseReference, request.Description, request.Value), ct));

    // --- Risk Allocation Matrix ---

    [HttpGet("nec4/risk-allocation")]
    public async Task<ActionResult<List<RiskAllocationItemDto>>> GetRiskAllocationItems(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetRiskAllocationItemsQuery(projectId), ct));

    public record CreateRiskAllocationItemRequest(string Description, RiskAllocationParty AllocatedTo, string? MitigationOwner);

    [HttpPost("nec4/risk-allocation")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateRiskAllocationItem(Guid projectId, CreateRiskAllocationItemRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateRiskAllocationItemCommand(projectId, request.Description, request.AllocatedTo, request.MitigationOwner), ct));

    // --- Accepted Programme ---

    [HttpGet("nec4/accepted-programme")]
    public async Task<ActionResult<List<AcceptedProgrammeEntryDto>>> GetAcceptedProgrammeEntries(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAcceptedProgrammeEntriesQuery(projectId), ct));

    public record CreateAcceptedProgrammeEntryRequest(int RevisionNumber, DateOnly AcceptedDate, string? Notes);

    [HttpPost("nec4/accepted-programme")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateAcceptedProgrammeEntry(Guid projectId, CreateAcceptedProgrammeEntryRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateAcceptedProgrammeEntryCommand(projectId, request.RevisionNumber, request.AcceptedDate, request.Notes), ct));

    // --- Payment Assessments ---

    [HttpGet("nec4/payment-assessments")]
    public async Task<ActionResult<List<PaymentAssessmentDto>>> GetPaymentAssessments(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetPaymentAssessmentsQuery(projectId), ct));

    public record CreatePaymentAssessmentRequest(int AssessmentNumber, DateOnly AssessmentDate, decimal AmountDue);

    [HttpPost("nec4/payment-assessments")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreatePaymentAssessment(Guid projectId, CreatePaymentAssessmentRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreatePaymentAssessmentCommand(projectId, request.AssessmentNumber, request.AssessmentDate, request.AmountDue), ct));

    public record UpdatePaymentAssessmentStatusRequest(PaymentAssessmentStatus Status);

    [HttpPut("~/api/nec4/payment-assessments/{paymentAssessmentId:guid}/status")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> UpdatePaymentAssessmentStatus(Guid paymentAssessmentId, UpdatePaymentAssessmentStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdatePaymentAssessmentStatusCommand(paymentAssessmentId, request.Status), ct);
        return NoContent();
    }

    // --- Change Register ---

    [HttpGet("nec4/change-register")]
    public async Task<ActionResult<List<ChangeRegisterItemDto>>> GetChangeRegisterItems(Guid projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetChangeRegisterItemsQuery(projectId), ct));

    public record CreateChangeRegisterItemRequest(string Title, string? Description, decimal ValueImpact, int TimeImpactDays);

    [HttpPost("nec4/change-register")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateChangeRegisterItem(Guid projectId, CreateChangeRegisterItemRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateChangeRegisterItemCommand(projectId, request.Title, request.Description, request.ValueImpact, request.TimeImpactDays), ct));

    public record UpdateChangeRegisterItemStatusRequest(ChangeRegisterStatus Status);

    [HttpPut("~/api/nec4/change-register/{changeRegisterItemId:guid}/status")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> UpdateChangeRegisterItemStatus(Guid changeRegisterItemId, UpdateChangeRegisterItemStatusRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateChangeRegisterItemStatusCommand(changeRegisterItemId, request.Status), ct);
        return NoContent();
    }
}
