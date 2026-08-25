using SCPM.Application.Reporting.Export;

namespace SCPM.Application.Common.Interfaces;

/// <summary>
/// Renders a format-independent <see cref="ExportDocument"/> — a title and a set of tables — into
/// any of the six export formats.
///
/// Separate from ICommitteeReportExporter because the two render different shapes: a committee
/// report is prose (heading, paragraph, heading, paragraph), a snapshot comparison is tabular.
/// Forcing one interface to do both would have meant either a lowest-common-denominator model
/// that served neither well, or a flag in every renderer.
/// </summary>
public interface ITabularDocumentExporter
{
    Task<byte[]> ExportAsync(ExportDocument document, ReportExportFormat format, CancellationToken cancellationToken);
}
