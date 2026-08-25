using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Export;

namespace SCPM.Infrastructure.Reporting;

/// <summary>
/// Renders an ExportDocument into all six formats, sharing the Stirling branding with
/// CommitteeReportExporter.
///
/// Every format shows the same tables in the same order, including the empty ones — an omitted
/// section reads as an oversight, whereas "No individual risk changed between these two points"
/// is an answer. That matters more for a comparison than for a report: a reader looking for
/// movement needs to be able to tell "nothing moved" from "this document forgot to say".
/// </summary>
public class TabularDocumentExporter : ITabularDocumentExporter
{
    private const string StirlingPurple = "#675A8F";
    private const string TextSecondary = "#5C6770";
    private const string HeaderFill = "#EFECF5";

    static TabularDocumentExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> ExportAsync(
        ExportDocument document, ReportExportFormat format, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(format switch
        {
            ReportExportFormat.Pdf => ExportPdf(document),
            ReportExportFormat.Xlsx => ExportXlsx(document),
            ReportExportFormat.Csv => ExportCsv(document),
            ReportExportFormat.Json => ExportJson(document),
            ReportExportFormat.Docx => OpenXmlReportBuilder.BuildTabularDocx(document, StirlingPurple, TextSecondary),
            ReportExportFormat.Pptx => OpenXmlReportBuilder.BuildTabularPptx(document, StirlingPurple, TextSecondary),
            _ => throw new NotSupportedException($"Export format {format} is not supported.")
        });
    }

    private static byte[] ExportPdf(ExportDocument document)
    {
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Landscape: the comparison tables run to six columns, and portrait would either
                // wrap every cell or shrink the type past readability.
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("Stirling Council").FontColor(StirlingPurple).Bold().FontSize(10);
                    col.Item().Text(document.Title).FontSize(18).Bold();
                    col.Item().Text(document.Subtitle).FontColor(TextSecondary);
                    foreach (var line in document.MetaLines)
                        col.Item().Text(line).FontColor(TextSecondary).FontSize(8);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(StirlingPurple);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    foreach (var table in document.Tables)
                    {
                        col.Item().PaddingTop(10).Text(table.Heading)
                            .FontColor(StirlingPurple).Bold().FontSize(11);

                        if (table.Rows.Count == 0)
                        {
                            col.Item().PaddingTop(2).Text(table.EmptyMessage ?? "No entries.")
                                .FontColor(TextSecondary).Italic();
                            continue;
                        }

                        col.Item().PaddingTop(3).Table(t =>
                        {
                            t.ColumnsDefinition(columns =>
                            {
                                foreach (var _ in table.Columns) columns.RelativeColumn();
                            });

                            foreach (var column in table.Columns)
                            {
                                t.Cell().Background(HeaderFill).Padding(3)
                                    .Text(column).Bold().FontColor(StirlingPurple);
                            }

                            foreach (var row in table.Rows)
                                foreach (var cell in row)
                                    t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(cell);
                        });
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

        return pdf.GeneratePdf();
    }

    private static byte[] ExportXlsx(ExportDocument document)
    {
        using var workbook = new XLWorkbook();

        // One worksheet per table rather than one long sheet: a spreadsheet is opened to be
        // sorted and filtered, and that only works when a sheet holds a single table with a
        // single header row.
        foreach (var table in document.Tables)
        {
            var sheet = workbook.Worksheets.Add(SheetName(table.Heading, workbook));

            for (var c = 0; c < table.Columns.Count; c++)
            {
                var cell = sheet.Cell(1, c + 1);
                cell.Value = table.Columns[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderFill);
            }

            for (var r = 0; r < table.Rows.Count; r++)
                for (var c = 0; c < table.Rows[r].Count; c++)
                    sheet.Cell(r + 2, c + 1).Value = table.Rows[r][c];

            if (table.Rows.Count == 0 && table.EmptyMessage is not null)
            {
                sheet.Cell(2, 1).Value = table.EmptyMessage;
                sheet.Cell(2, 1).Style.Font.Italic = true;
            }
            else if (table.Rows.Count > 0)
            {
                sheet.Range(1, 1, table.Rows.Count + 1, table.Columns.Count).SetAutoFilter();
            }

            sheet.Columns().AdjustToContents(8, 60);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Excel sheet names cap at 31 characters and reject : \ / ? * [ ] — and silently corrupt the
    /// file if a duplicate slips through, so uniqueness is enforced here rather than hoped for.
    /// </summary>
    private static string SheetName(string heading, XLWorkbook workbook)
    {
        var cleaned = new string(heading.Where(c => !":\\/?*[]".Contains(c)).ToArray());
        var name = cleaned.Length <= 31 ? cleaned : cleaned[..31];

        var suffix = 2;
        while (workbook.Worksheets.Any(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            var tag = $" ({suffix++})";
            name = (cleaned.Length + tag.Length <= 31 ? cleaned : cleaned[..(31 - tag.Length)]) + tag;
        }

        return name;
    }

    private static byte[] ExportCsv(ExportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CsvEscape(document.Title));
        sb.AppendLine(CsvEscape(document.Subtitle));
        foreach (var line in document.MetaLines) sb.AppendLine(CsvEscape(line));

        foreach (var table in document.Tables)
        {
            // A blank line then the heading: CSV has no notion of sections, so the separation has
            // to be visual. Anyone parsing this programmatically should use the JSON export, which
            // keeps the structure instead of flattening it.
            sb.AppendLine();
            sb.AppendLine(CsvEscape(table.Heading));

            if (table.Rows.Count == 0)
            {
                sb.AppendLine(CsvEscape(table.EmptyMessage ?? "No entries."));
                continue;
            }

            sb.AppendLine(string.Join(",", table.Columns.Select(CsvEscape)));
            foreach (var row in table.Rows)
                sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private static byte[] ExportJson(ExportDocument document) =>
        JsonSerializer.SerializeToUtf8Bytes(document, new JsonSerializerOptions { WriteIndented = true });
}
