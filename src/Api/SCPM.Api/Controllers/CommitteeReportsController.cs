using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Commands.CreateCommitteeReport;
using SCPM.Application.Reporting.Commands.SubmitCommitteeReport;
using SCPM.Application.Reporting.Commands.UpdateCommitteeReport;
using SCPM.Application.Reporting.Dtos;
using SCPM.Application.Reporting.Queries.CompareSnapshotItems;
using SCPM.Application.Reporting.Queries.CompareSnapshots;
using SCPM.Application.Reporting.Queries.GetCommitteeReport;
using SCPM.Application.Reporting.Queries.GetCommitteeReports;
using SCPM.Domain.Enums;

namespace SCPM.Api.Controllers;

[ApiController]
[Authorize]
public class CommitteeReportsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ICommitteeReportExporter _exporter;

    public CommitteeReportsController(ISender mediator, ICommitteeReportExporter exporter)
    {
        _mediator = mediator;
        _exporter = exporter;
    }

    /// <summary>The Reporting Centre — every report across the portfolio, or one project's when
    /// projectId is supplied (the workspace Reports tab).</summary>
    [HttpGet("api/committee-reports")]
    public async Task<ActionResult<List<CommitteeReportListItemDto>>> GetReports([FromQuery] Guid? projectId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCommitteeReportsQuery(projectId), ct));

    [HttpGet("api/committee-reports/{id:guid}")]
    public async Task<ActionResult<CommitteeReportDto>> GetReport(Guid id, CancellationToken ct)
    {
        var report = await _mediator.Send(new GetCommitteeReportQuery(id), ct);
        return report is null ? NotFound() : Ok(report);
    }

    public record CreateCommitteeReportRequest(CommitteeReportType ReportType, string Title, DateOnly? MeetingDate, Guid? SnapshotId);

    [HttpPost("api/projects/{projectId:guid}/committee-reports")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateReport(Guid projectId, CreateCommitteeReportRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateCommitteeReportCommand(projectId, request.ReportType, request.Title, request.MeetingDate, request.SnapshotId), ct));

    public record UpdateCommitteeReportRequest(
        string ExecutiveSummary, string? Background, string? CurrentPosition, string? FinanceCommentary,
        string? ProgrammeCommentary, string? RiskCommentary, string? StakeholderCommentary,
        string? SustainabilityCommentary, string? EqualityImpactCommentary, string? Recommendations);

    [HttpPut("api/committee-reports/{id:guid}")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> UpdateReport(Guid id, UpdateCommitteeReportRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateCommitteeReportCommand(
            id, request.ExecutiveSummary, request.Background, request.CurrentPosition, request.FinanceCommentary,
            request.ProgrammeCommentary, request.RiskCommentary, request.StakeholderCommentary,
            request.SustainabilityCommentary, request.EqualityImpactCommentary, request.Recommendations), ct);
        return NoContent();
    }

    [HttpPut("api/committee-reports/{id:guid}/submit")]
    [Authorize(Policy = "CanApprove")]
    public async Task<IActionResult> SubmitReport(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new SubmitCommitteeReportCommand(id), ct);
        return NoContent();
    }

    private static readonly Dictionary<ReportExportFormat, string> ContentTypes = new()
    {
        [ReportExportFormat.Pdf] = "application/pdf",
        [ReportExportFormat.Xlsx] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [ReportExportFormat.Csv] = "text/csv",
        [ReportExportFormat.Json] = "application/json",
    };

    [HttpGet("api/committee-reports/{id:guid}/export/{format}")]
    public async Task<IActionResult> ExportReport(Guid id, ReportExportFormat format, CancellationToken ct)
    {
        var report = await _mediator.Send(new GetCommitteeReportQuery(id), ct);
        if (report is null) return NotFound();

        var bytes = await _exporter.ExportAsync(report, format, ct);
        var fileName = $"{report.ProjectRef}-{report.ReportType}-{report.Title}.{format.ToString().ToLowerInvariant()}";

        return File(bytes, ContentTypes[format], fileName);
    }

    [HttpGet("api/snapshots/compare")]
    public async Task<ActionResult<SnapshotComparisonDto>> CompareSnapshots(
        [FromQuery] Guid fromSnapshotId, [FromQuery] Guid toSnapshotId, CancellationToken ct)
        => Ok(await _mediator.Send(new CompareSnapshotsQuery(fromSnapshotId, toSnapshotId), ct));

    /// <summary>
    /// The item-level counterpart of the comparison above: which risks and milestones changed,
    /// rather than by how much the counts did. Separate endpoint rather than a flag on the other
    /// one, because it reads the temporal history instead of the snapshot rows and is therefore a
    /// materially more expensive query — a caller should opt into that cost knowingly.
    /// </summary>
    [HttpGet("api/snapshots/compare/items")]
    public async Task<ActionResult<SnapshotItemComparisonDto>> CompareSnapshotItems(
        [FromQuery] Guid fromSnapshotId, [FromQuery] Guid toSnapshotId, CancellationToken ct)
        => Ok(await _mediator.Send(new CompareSnapshotItemsQuery(fromSnapshotId, toSnapshotId), ct));
}
