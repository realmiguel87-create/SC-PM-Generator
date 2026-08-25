using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;

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
    private static readonly string StirlingPurple = "#675A8F";
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
            ReportExportFormat.Docx => OpenXmlReportBuilder.BuildDocx(report, Sections, StirlingPurple, TextSecondary),
            ReportExportFormat.Pptx => OpenXmlReportBuilder.BuildPptx(report, Sections, StirlingPurple, TextSecondary),
            _ => throw new NotSupportedException($"Export format {format} is not supported.")
        });
    }

    private static readonly (string Heading, Func<CommitteeReportDto, string?> Select)[] Sections =
    [
        ("Executive Summary", r => r.ExecutiveSummary),
        ("Background", r => r.Background),
        ("Current Position", r => r.CurrentPosition),
        ("Finance", r => r.FinanceCommentary),
        ("Programme", r => r.ProgrammeCommentary),
        ("Risk", r => r.RiskCommentary),
        ("Stakeholders", r => r.StakeholderCommentary),
        ("Sustainability", r => r.SustainabilityCommentary),
        ("Equality Impact", r => r.EqualityImpactCommentary),
        ("Recommendations", r => r.Recommendations),
    ];

    private byte[] ExportPdf(CommitteeReportDto report)
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
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(StirlingPurple);
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    foreach (var (heading, select) in Sections)
                    {
                        var value = select(report);
                        if (string.IsNullOrWhiteSpace(value)) continue;

                        col.Item().PaddingTop(10).Text(heading).FontColor(StirlingPurple).Bold().FontSize(12);
                        col.Item().PaddingTop(2).Text(value);
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
        foreach (var (heading, select) in Sections)
        {
            var value = select(report);
            if (string.IsNullOrWhiteSpace(value)) continue;

            sheet.Cell(row, 1).Value = heading;
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 2).Value = value;
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

        foreach (var (heading, select) in Sections)
        {
            var value = select(report);
            if (string.IsNullOrWhiteSpace(value)) continue;
            sb.AppendLine($"{CsvEscape(heading)},{CsvEscape(value)}");
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
