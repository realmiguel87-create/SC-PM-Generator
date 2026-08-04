using SCPM.Application.Reporting.Dtos;

namespace SCPM.Application.Common.Interfaces;

public enum ReportExportFormat
{
    Pdf,
    Xlsx,
    Csv,
    Json
}

/// <summary>
/// The export engine (docs/architecture.md §9): one abstraction, one branded output per format,
/// generated from the same CommitteeReportDto so PDF/XLSX/CSV/JSON never drift from each other.
/// DOCX and PPTX are deferred — see docs/roadmap.md Phase 6 — rather than shipped half-verified.
/// </summary>
public interface ICommitteeReportExporter
{
    Task<byte[]> ExportAsync(CommitteeReportDto report, ReportExportFormat format, CancellationToken cancellationToken);
}
