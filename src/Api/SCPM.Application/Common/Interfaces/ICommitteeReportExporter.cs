using SCPM.Application.Reporting.Dtos;

namespace SCPM.Application.Common.Interfaces;

public enum ReportExportFormat
{
    Pdf,
    Xlsx,
    Csv,
    Json,
    Docx,
    Pptx
}

/// <summary>
/// The export engine (docs/architecture.md §9): one abstraction, one branded output per format,
/// all generated from the same CommitteeReportDto so the formats can never drift from each other.
///
/// DOCX and PPTX were deliberately held back when the first four shipped — six half-verified
/// formats being a worse outcome than four verified ones — and added once they could be checked
/// the same way. Every generated file is re-opened and run through OpenXmlValidator in the tests,
/// so "it produced bytes" is never mistaken for "it produced a document Word or PowerPoint will
/// actually open".
/// </summary>
public interface ICommitteeReportExporter
{
    Task<byte[]> ExportAsync(CommitteeReportDto report, ReportExportFormat format, CancellationToken cancellationToken);
}
