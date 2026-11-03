using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SCPM.Application.Reporting.Dtos;
using SCPM.Domain.Common;

namespace SCPM.Infrastructure.Reporting;

/// <summary>
/// The PDF of a branded document, laid out to match the Word version.
///
/// Kept apart from <see cref="CommitteeReportExporter"/> for the same reason the Open XML builder
/// is: the layout is long enough to bury the dispatch it would sit inside.
///
/// The two formats have to agree, because they are the same document. Until now they did not —
/// the PDF carried the old plain layout, the pre-logo palette and no mark at all, so pressing
/// "PDF" instead of "Word" produced something a reader would not recognise as the same report.
/// That mattered little while these were printed from Word; it matters a great deal now they are
/// read on screen and shared as files, which is what the PDF is for.
///
/// One thing cannot be made to agree, and it is a constraint rather than an oversight. A PDF
/// embeds its fonts at generation time, on the server; a Word document uses the fonts on the
/// reader's own machine. Palatino Linotype is licensed and cannot be redistributed, so it can only
/// be embedded here if it is installed on the host that generates the file. On Windows hosting it
/// is, and the two formats match exactly. On Linux hosting it is not, and the fallback chain below
/// decides what happens instead — predictably, rather than by whatever the machine happens to
/// have first.
/// </summary>
internal static class PdfReportBuilder
{
    /// <summary>
    /// The serif, in order of preference. Palatino Linotype where the host has it; then the two
    /// names the URW Palladio clone goes by, which Linux distributions package; then Georgia and
    /// a last-resort serif that is present almost everywhere.
    ///
    /// Stated as a chain rather than left to the renderer so that a given host always produces the
    /// same file. "Whatever serif this machine defaults to" is how two servers in the same
    /// deployment come to emit different-looking documents.
    ///
    /// QuestPDF takes the whole chain in one FontFamily call and walks it in order, so the first
    /// name present on the host wins.
    /// </summary>
    private static readonly string[] SerifChain =
        ["Palatino Linotype", "P052", "URW Palladio L", "Georgia", "DejaVu Serif"];

    private static readonly string[] SansChain =
        ["Segoe UI", "Liberation Sans", "DejaVu Sans"];

    private const string BodyInk = "#1F1B2A";
    private const string MutedInk = "#5C6770";
    private const string PanelTint = "#F4F2F8";

    /// <summary>Logo height in the footer: 11mm, in points — the same 11mm the Word footer uses.</summary>
    private const float LogoHeightPoints = 11f * 72f / 25.4f;

    public static byte[] Build(
        CommitteeReportDto report,
        DocumentDefinition? definition,
        IReadOnlyList<ReportSectionDto> sections,
        string purple,
        string green,
        byte[]? logo)
    {
        var eyebrow = definition?.Eyebrow ?? "Infrastructure Delivery · Programme Governance";
        var title = definition?.Title ?? report.Title;
        var facts = Facts(report, definition);
        var footerText = definition is null
            ? "Doc No: PD.01.25      Issued by: Project Delivery      Date: Jun-17      Version: 01"
            : $"{definition.Title}      Doc No: [to be assigned]      "
              + "Issued by: Project Delivery      Version: 01";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.MarginTop(1.5f, Unit.Centimetre);
                // Deeper than the top, to clear the footer and its logo — the same asymmetry the
                // Word document has, and for the same reason.
                page.MarginBottom(2.2f, Unit.Centimetre);

                page.DefaultTextStyle(x => x
                    .FontFamily(SerifChain)
                    .FontSize(11)
                    .FontColor(BodyInk));

                page.Content().Column(column =>
                {
                    // The strip: the service in one line, the title below it on white. The title
                    // is what a reader needs first, so it takes the page's largest type rather
                    // than sharing a block with the service name.
                    column.Item()
                        .Background(purple)
                        .PaddingVertical(5)
                        .PaddingHorizontal(11)
                        .Text(eyebrow.ToUpperInvariant())
                        .FontFamily(SansChain)
                        .FontSize(7.5f).Bold().LetterSpacing(0.17f).FontColor("#FFFFFF");

                    column.Item().PaddingTop(16).Text(title).FontSize(32).Bold().FontColor(BodyInk);

                    var date = FormatDate(report);
                    if (definition is not null && !string.IsNullOrWhiteSpace(date))
                    {
                        column.Item().PaddingTop(3).Text(date.ToUpperInvariant())
                            .FontFamily(SansChain)
                            .FontSize(9).LetterSpacing(0.12f).FontColor(MutedInk);
                    }

                    column.Item().PaddingTop(16).Element(e => FactsPanel(e, facts));

                    column.Item().PaddingTop(18).Element(e => Sections(e, sections, green));
                });

                page.Footer().Element(e => Footer(e, footerText, purple, logo));
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// The facts in a tinted panel, three to a row — one tinted block rather than six boxes, so
    /// the eye takes it as a single field.
    /// </summary>
    private static void FactsPanel(IContainer container, IReadOnlyList<(string Label, string Value)> facts)
    {
        container.Background(PanelTint).Padding(11).Column(column =>
        {
            var rows = facts.Chunk(3).ToList();

            for (var r = 0; r < rows.Count; r++)
            {
                var row = rows[r];

                column.Item().PaddingTop(r == 0 ? 0 : 8).Row(line =>
                {
                    foreach (var (label, value) in row)
                    {
                        line.RelativeItem().PaddingRight(10).Column(cell =>
                        {
                            // Label small, spaced and grey; value larger and bold. The eye should
                            // land on the answer, not the question.
                            cell.Item().Text(label.ToUpperInvariant())
                                .FontFamily(SansChain)
                                .FontSize(7).Bold().LetterSpacing(0.16f).FontColor(MutedInk);

                            cell.Item().PaddingTop(1)
                                .Text(string.IsNullOrWhiteSpace(value) ? " " : value)
                                .FontFamily(SansChain)
                                .FontSize(9).SemiBold().FontColor(BodyInk);
                        });
                    }

                    // A short final row would otherwise stretch its cells across the full width.
                    for (var i = row.Length; i < 3; i++) line.RelativeItem();
                });
            }
        });
    }

    private static void Sections(
        IContainer container, IReadOnlyList<ReportSectionDto> sections, string green)
    {
        container.Column(column =>
        {
            for (var i = 0; i < sections.Count; i++)
            {
                var section = sections[i];

                column.Item().PaddingTop(i == 0 ? 0 : 14).Row(row =>
                {
                    // Two digits throughout, so 9 and 10 align. A document of nine sections
                    // numbered 1..9 and one of twelve numbered 01..12 would not look like the
                    // same family.
                    row.ConstantItem(30).Text((i + 1).ToString("00"))
                        .FontSize(14).Bold().FontColor(green);

                    row.RelativeItem().Column(content =>
                    {
                        content.Item().Text(section.Heading).FontSize(11).Bold().FontColor(BodyInk);

                        if (!string.IsNullOrWhiteSpace(section.Content))
                        {
                            foreach (var line in OpenXmlReportBuilder.SplitLines(section.Content))
                            {
                                content.Item().PaddingTop(4).Row(bullet =>
                                {
                                    bullet.ConstantItem(10).Text("•").FontColor(BodyInk);
                                    bullet.RelativeItem().Text(line).LineHeight(1.35f);
                                });
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(section.Prompt))
                        {
                            // Guidance, shown only while the section is empty: grey and italic, so
                            // it is visibly an instruction rather than content.
                            content.Item().PaddingTop(4).PaddingLeft(10)
                                .Text(section.Prompt)
                                .FontSize(10).Italic().FontColor(MutedInk).LineHeight(1.35f);
                        }
                    });
                });
            }
        });
    }

    /// <summary>
    /// The document-control line under a purple rule, with the logo at the right where one is
    /// embedded. Page numbering is deliberately absent: it is not on the Word version, and a
    /// difference between the two formats is exactly what this layout exists to remove.
    /// </summary>
    private static void Footer(IContainer container, string text, string purple, byte[]? logo)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(5).LineHorizontal(1.5f).LineColor(purple);

            column.Item().Row(row =>
            {
                row.RelativeItem().AlignBottom().Text(text)
                    .FontFamily(SansChain)
                    .FontSize(7).FontColor(MutedInk);

                if (logo is null) return;

                var dimensions = CouncilLogo.Dimensions(logo);
                if (dimensions is null) return;

                // Scaled from the file's real dimensions, so the mark cannot come out stretched.
                var width = LogoHeightPoints * dimensions.Value.Width / dimensions.Value.Height;

                row.ConstantItem(width).Height(LogoHeightPoints).Image(logo).FitArea();
            });
        });
    }

    private static IReadOnlyList<(string Label, string Value)> Facts(
        CommitteeReportDto report, DocumentDefinition? definition)
    {
        var budget = report.ApprovedBudget > 0
            ? report.ApprovedBudget.ToString("C2", OpenXmlReportBuilder.Culture)
            : string.Empty;

        // A stage document shows its RIBA stage where the status report shows its date; the status
        // report's date belongs among its facts, a stage document's belongs under its title.
        var third = definition is null
            ? ("Report Date", FormatDate(report))
            : ("RIBA Stage", definition.RibaStage);

        return
        [
            ("Project / Programme", report.ProjectName),
            ("Project ID Ref", report.ProjectRef),
            third,
            ("Project Sponsor", report.SponsorName ?? string.Empty),
            ("Project Manager", report.ProjectManagerName ?? string.Empty),
            ("Budget", budget),
        ];
    }

    private static string FormatDate(CommitteeReportDto report) =>
        report.ReportDate?.ToString("d MMMM yyyy", OpenXmlReportBuilder.Culture)
        ?? report.MeetingDate?.ToString("d MMMM yyyy", OpenXmlReportBuilder.Culture)
        ?? string.Empty;
}
