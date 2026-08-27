using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Commands.CreateCommitteeReport;
using SCPM.Application.Reporting.Commands.SubmitCommitteeReport;
using SCPM.Application.Reporting.Commands.UpdateCommitteeReport;
using SCPM.Application.Reporting.Dtos;
using SCPM.Application.Reporting.Export;
using SCPM.Application.Reporting.Queries.CompareSnapshotItems;
using SCPM.Application.Reporting.Queries.CompareSnapshots;
using SCPM.Application.Reporting.Queries.GetCommitteeReport;
using SCPM.Application.Reporting.Queries.GetSnapshotIntervalActivity;
using SCPM.Application.Reporting.Queries.GetCommitteeReports;
using SCPM.Domain.Enums;

namespace SCPM.Api.Controllers;

[ApiController]
[Authorize]
public class CommitteeReportsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ICommitteeReportExporter _exporter;
    private readonly ITabularDocumentExporter _tabularExporter;

    public CommitteeReportsController(
        ISender mediator, ICommitteeReportExporter exporter, ITabularDocumentExporter tabularExporter)
    {
        _mediator = mediator;
        _tabularExporter = tabularExporter;
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

    /// <summary>
    /// <c>ReportDate</c> is the date the position is reported as at — the status report's own
    /// "Report Date". <c>MeetingDate</c> is when a committee sits, and is left null on a status
    /// report, which is not written for a meeting.
    /// </summary>
    public record CreateCommitteeReportRequest(
        CommitteeReportType ReportType,
        string Title,
        DateOnly? MeetingDate,
        DateOnly? ReportDate,
        Guid? SnapshotId);

    [HttpPost("api/projects/{projectId:guid}/committee-reports")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<Guid>> CreateReport(
        Guid projectId, CreateCommitteeReportRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateCommitteeReportCommand(
            projectId, request.ReportType, request.Title,
            request.MeetingDate, request.ReportDate, request.SnapshotId), ct));

    /// <summary>
    /// Only the sections being changed need be sent. The previous shape took all ten fields, so a
    /// client editing one paragraph had to return the whole document — and anything it omitted was
    /// written as null, silently erasing sections nobody meant to touch.
    /// </summary>
    public record UpdateCommitteeReportRequest(
        List<ReportSectionUpdate> Sections,
        DateOnly? ReportDate);

    [HttpPut("api/committee-reports/{id:guid}")]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> UpdateReport(
        Guid id, UpdateCommitteeReportRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateCommitteeReportCommand(
            id, request.Sections ?? [], request.ReportDate), ct);
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
        [ReportExportFormat.Docx] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [ReportExportFormat.Pptx] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
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

    /// <summary>
    /// What happened between two snapshots that comparing their endpoints cannot reveal — items
    /// raised and removed inside the window, or changed and changed back. Separate from the
    /// comparison endpoints because it reads every row version in the period rather than the
    /// state at two instants, and is the most expensive of the three.
    /// </summary>
    [HttpGet("api/snapshots/compare/interval-activity")]
    public async Task<ActionResult<SnapshotIntervalActivityDto>> GetSnapshotIntervalActivity(
        [FromQuery] Guid fromSnapshotId, [FromQuery] Guid toSnapshotId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetSnapshotIntervalActivityQuery(fromSnapshotId, toSnapshotId), ct));

    /// <summary>
    /// The whole comparison as a downloadable document: headline movements, the item-level
    /// changes, and the activity that happened in between.
    ///
    /// All three go in one file rather than three, because they answer one question at three
    /// depths and a pack containing only the aggregates would be true and misleading — it would
    /// show the risk count rising by two without showing that a third risk was raised and closed
    /// inside the period.
    /// </summary>
    [HttpGet("api/snapshots/compare/export/{format}")]
    public async Task<IActionResult> ExportComparison(
        ReportExportFormat format,
        [FromQuery] Guid fromSnapshotId,
        [FromQuery] Guid toSnapshotId,
        CancellationToken ct)
    {
        var summary = await _mediator.Send(new CompareSnapshotsQuery(fromSnapshotId, toSnapshotId), ct);
        var items = await _mediator.Send(new CompareSnapshotItemsQuery(fromSnapshotId, toSnapshotId), ct);
        var interval = await _mediator.Send(new GetSnapshotIntervalActivityQuery(fromSnapshotId, toSnapshotId), ct);

        var document = ComparisonExportBuilder.Build(summary, items, interval);
        var bytes = await _tabularExporter.ExportAsync(document, format, ct);

        var fileName = $"Snapshot-comparison-{summary.FromCapturedAt:yyyy-MM-dd}-to-"
            + $"{summary.ToCapturedAt:yyyy-MM-dd}.{format.ToString().ToLowerInvariant()}";

        return File(bytes, ContentTypes[format], fileName);
    }
}
