using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Infrastructure.Reporting;

/// <summary>
/// Generates every export format from the same CommitteeReportDto, so they all show the same
/// content and the Stirling branding (purple header, muted greys) is defined once. The section
/// list below is the single definition of what a report contains and in what order — adding a
/// section here adds it to all six formats at once, which is the whole point of the shared array.
///
/// DOCX and PPTX live in OpenXmlReportBuilder: the Open XML SDK needs far more scaffolding than
/// QuestPDF or ClosedXML, and inlining it here would bury the report structure under it.
/// </summary>
public class CommitteeReportExporter : ICommitteeReportExporter
{
    // Measured from the logo file itself, not sampled by eye. The values previously here
    // (#675A8F and #4F8377) are lighter than the mark: putting the real logo in the footer under
    // a #675A8F strip would have shown two different purples on one page. #4F8377 was also
    // 4.33:1 against white, under the 4.5:1 minimum for normal text — the logo's own green is
    // 6.29:1, so using the real colour fixes the accessibility gap rather than trading against it.
    private static readonly string StirlingPurple = "#3E2880";
    private static readonly string StirlingGreen = "#006D5B";

    private static readonly string TextSecondary = "#5C6770";

    static CommitteeReportExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> ExportAsync(CommitteeReportDto report, ReportExportFormat format, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(format switch
        {
            ReportExportFormat.Pdf => ExportPdf(report),
            ReportExportFormat.Xlsx => ExportXlsx(report),
            ReportExportFormat.Csv => ExportCsv(report),
            ReportExportFormat.Json => ExportJson(report),
            ReportExportFormat.Docx => ExportDocx(report),
            ReportExportFormat.Pptx => OpenXmlReportBuilder.BuildPptx(report, report.Sections, StirlingPurple, TextSecondary),
            _ => throw new NotSupportedException($"Export format {format} is not supported.")
        });
    }

    /// <summary>
    /// Picks the Word layout for the report type.
    ///
    /// The status report and the five RIBA stage documents share the branded layout — purple band,
    /// facts grid, green headings. Committee papers keep the plain heading-and-paragraph layout
    /// they have always had: the restyle was asked for on the council's status report, not as a
    /// house style imposed on every document the platform produces.
    ///
    /// An unrecognised type falls through to the plain layout rather than throwing. A report type
    /// added to the enum without a layout should still export.
    /// </summary>
    private static byte[] ExportDocx(CommitteeReportDto report)
    {
        if (Meeting(report) is { } meeting)
        {
            return OpenXmlReportBuilder.BuildMeetingDocx(
                report, meeting, StirlingPurple, StirlingGreen, CouncilLogo.Png);
        }

        var branded = BrandedLayout(report);

        if (branded is null)
        {
            return OpenXmlReportBuilder.BuildDocx(report, report.Sections, StirlingPurple, TextSecondary);
        }

        return branded.Definition is null
            ? OpenXmlReportBuilder.BuildStatusReportDocx(
                report, report.Sections, StirlingPurple, StirlingGreen, CouncilLogo.Png)
            : OpenXmlReportBuilder.BuildStageDocumentDocx(
                report, branded.Definition, report.Sections, StirlingPurple, StirlingGreen,
                CouncilLogo.Png);
    }

    /// <summary>
    /// Which documents get the branded layout, and the stage definition where there is one.
    ///
    /// Shared by the Word and PDF paths deliberately. They had drifted — the PDF kept the plain
    /// layout and the pre-logo palette long after the Word version moved on, so the same report
    /// looked like two different documents depending on which button was pressed. Deciding once
    /// means a report type cannot be branded in one format and not the other.
    ///
    /// Null means the plain layout. A null <c>Definition</c> inside a non-null result means the
    /// status report, which is branded but is not a RIBA stage document.
    /// </summary>
    private sealed record BrandedDocument(DocumentDefinition? Definition);

    private static BrandedDocument? BrandedLayout(CommitteeReportDto report)
    {
        if (report.ReportType == nameof(CommitteeReportType.StatusReport))
        {
            return new BrandedDocument(null);
        }

        // An unrecognised type falls through to the plain layout rather than throwing: a report
        // type added to the enum without a layout should still export.
        return Enum.TryParse<CommitteeReportType>(report.ReportType, out var type)
            && StageDocuments.Find(type) is { } definition
                ? new BrandedDocument(definition)
                : null;
    }

    /// <summary>The meeting definition for this report type, or null when it is not a meeting.</summary>
    private static MeetingDefinition? Meeting(CommitteeReportDto report) =>
        Enum.TryParse<CommitteeReportType>(report.ReportType, out var type)
            ? MeetingDocuments.Find(type)
            : null;

    private byte[] ExportPdf(CommitteeReportDto report)
    {
        if (Meeting(report) is { } meeting)
        {
            return PdfReportBuilder.BuildMeeting(
                report, meeting, StirlingPurple, StirlingGreen, CouncilLogo.Png);
        }

        if (BrandedLayout(report) is { } branded)
        {
            return PdfReportBuilder.Build(
                report, branded.Definition, report.Sections, StirlingPurple, StirlingGreen,
                CouncilLogo.Png);
        }

        return ExportPlainPdf(report);
    }

    /// <summary>
    /// The heading-and-paragraph layout committee papers have always had. Unchanged: the restyle
    /// was asked for on the council's status report and the stage documents, not as a house style
    /// imposed on everything the platform produces.
    /// </summary>
    private byte[] ExportPlainPdf(CommitteeReportDto report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Stirling Council").FontColor(StirlingPurple).Bold().FontSize(10);
                    col.Item().Text(report.Title).FontSize(18).Bold();
                    col.Item().Text($"{report.ReportType} · {report.ProjectRef} — {report.ProjectName}").FontColor(TextSecondary);
                    if (report.MeetingDate.HasValue)
                        col.Item().Text($"Meeting date: {report.MeetingDate:d MMMM yyyy}").FontColor(TextSecondary);
                    if (report.ReportDate.HasValue)
                        col.Item().Text($"Report date: {report.ReportDate:d MMMM yyyy}").FontColor(TextSecondary);

                    // Sponsor, manager, reference, budget — the status report's header block.
                    foreach (var (label, value) in OpenXmlReportBuilder.HeaderFacts(report))
                        col.Item().Text($"{label}: {value}").FontColor(TextSecondary);

                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(StirlingPurple);
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    foreach (var section in report.Sections)
                    {
                        if (string.IsNullOrWhiteSpace(section.Content)) continue;

                        col.Item().PaddingTop(10).Text(section.Heading)
                            .FontColor(StirlingPurple).Bold().FontSize(12);

                        // One paragraph per line: the council's template uses bullet lists
                        // throughout, and collapsing a five-item list into one run of prose is the
                        // difference between a document someone scans and one they read twice.
                        foreach (var line in OpenXmlReportBuilder.SplitLines(section.Content))
                            col.Item().PaddingTop(2).Text(line);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ").FontColor(TextSecondary);
                    x.CurrentPageNumber().FontColor(TextSecondary);
                    x.Span(" of ").FontColor(TextSecondary);
                    x.TotalPages().FontColor(TextSecondary);
                });
            });
        });

        return document.GeneratePdf();
    }

    private byte[] ExportXlsx(CommitteeReportDto report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Report");

        sheet.Cell(1, 1).Value = report.Title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Cell(2, 1).Value = $"{report.ReportType} · {report.ProjectRef} — {report.ProjectName}";

        var row = 4;
        foreach (var section in report.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.Content)) continue;

            sheet.Cell(row, 1).Value = section.Heading;
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 2).Value = section.Content;
            sheet.Cell(row, 2).Style.Alignment.WrapText = true;
            row++;
        }

        sheet.Column(1).Width = 22;
        sheet.Column(2).Width = 90;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] ExportCsv(CommitteeReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Section,Content");
        sb.AppendLine($"{CsvEscape("Title")},{CsvEscape(report.Title)}");
        sb.AppendLine($"{CsvEscape("Project")},{CsvEscape($"{report.ProjectRef} — {report.ProjectName}")}");

        foreach (var section in report.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.Content)) continue;
            sb.AppendLine($"{CsvEscape(section.Heading)},{CsvEscape(section.Content)}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private static byte[] ExportJson(CommitteeReportDto report) =>
        JsonSerializer.SerializeToUtf8Bytes(report, new JsonSerializerOptions { WriteIndented = true });
}
