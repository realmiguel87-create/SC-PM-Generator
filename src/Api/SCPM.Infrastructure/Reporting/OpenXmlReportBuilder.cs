using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using SCPM.Application.Reporting.Dtos;
using SCPM.Application.Reporting.Export;
using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace SCPM.Infrastructure.Reporting;

/// <summary>
/// DOCX and PPTX generation, kept out of CommitteeReportExporter because the Open XML SDK is
/// verbose in a way the other four formats are not — a presentation has to be assembled part by
/// part (presentation, slide master, layout, theme, slides) before it will open at all, and
/// mixing that with QuestPDF and ClosedXML calls would bury the shared report structure.
///
/// Two things worth knowing before editing anything here.
///
/// Element order is part of the schema, not a style preference. Open XML types are sequences:
/// inside a run's properties, `w:color` must precede `w:sz`, and reversing them produces a file
/// that still saves, still unzips, and still looks like a document — but is invalid, and Word
/// may repair or reject it. That exact mistake was made and caught while building this, by
/// OpenXmlValidator rather than by reading, which is why the tests validate every generated file
/// rather than checking it starts with a ZIP header.
///
/// Colours here are bare six-digit hex with no leading '#'. Open XML rejects the CSS form, and
/// the constants in CommitteeReportExporter carry the '#' because they came from the web palette
/// — hence Rgb() below rather than passing them straight through.
/// </summary>
internal static class OpenXmlReportBuilder
{
    /// <summary>Open XML wants "675A8F"; the shared palette constants are CSS-style "#675A8F".</summary>
    private static string Rgb(string cssHex) => cssHex.TrimStart('#');

    /// <summary>
    /// The facts printed under the title: who owns the project, its reference, its budget.
    ///
    /// Only the ones actually known are returned. A header row reading "Project Sponsor: —" tells
    /// a reader nothing except that the platform has a field for it.
    /// </summary>
    internal static IEnumerable<(string Label, string Value)> HeaderFacts(CommitteeReportDto report)
    {
        yield return ("Project ID Ref", report.ProjectRef);

        if (!string.IsNullOrWhiteSpace(report.SponsorName))
        {
            yield return ("Project Sponsor", report.SponsorName);
        }

        if (!string.IsNullOrWhiteSpace(report.ProjectManagerName))
        {
            yield return ("Project Manager", report.ProjectManagerName);
        }

        if (report.ApprovedBudget > 0)
        {
            yield return ("Budget", report.ApprovedBudget.ToString("C0", Culture));
        }
    }

    /// <summary>
    /// Sterling, explicitly. Without a culture the server's own decides, and a council report
    /// rendering "$358,000" on a machine that happens to be configured for the United States is
    /// the kind of defect that is only ever found by a reader.
    /// </summary>
    internal static readonly System.Globalization.CultureInfo Culture = new("en-GB");

    /// <summary>
    /// Splits section content into display lines, dropping blanks. Handles both newline
    /// conventions: text arriving from a browser textarea carries \r\n, and splitting on \n
    /// alone leaves a stray carriage return that Word renders as a box.
    /// </summary>
    internal static IEnumerable<string> SplitLines(string content) =>
        content.Replace("\r\n", "\n").Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0);

    // --- DOCX ---

    public static byte[] BuildDocx(
        CommitteeReportDto report,
        IReadOnlyList<ReportSectionDto> sections,
        string purple,
        string secondary)
    {
        using var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new W.Body();

            body.Append(TextParagraph("Stirling Council", 20, Rgb(purple), bold: true));
            body.Append(TextParagraph(report.Title, 36, "000000", bold: true));
            body.Append(TextParagraph(
                $"{report.ReportType} · {report.ProjectRef} — {report.ProjectName}", 20, Rgb(secondary)));

            if (report.MeetingDate.HasValue)
            {
                body.Append(TextParagraph(
                    $"Meeting date: {report.MeetingDate:d MMMM yyyy}", 20, Rgb(secondary)));
            }

            if (report.ReportDate.HasValue)
            {
                body.Append(TextParagraph(
                    $"Report date: {report.ReportDate:d MMMM yyyy}", 20, Rgb(secondary)));
            }

            // The status report's header block: sponsor, manager, reference, budget. Rendered as
            // label/value pairs rather than a table, because a Word table needs a grid, borders
            // and per-cell widths declared in a specific order, and the value here is the
            // information rather than the ruled box around it.
            foreach (var (label, value) in HeaderFacts(report))
            {
                body.Append(TextParagraph($"{label}: {value}", 20, Rgb(secondary)));
            }

            foreach (var section in sections)
            {
                if (string.IsNullOrWhiteSpace(section.Content)) continue;

                body.Append(TextParagraph(section.Heading, 26, Rgb(purple), bold: true));

                // Split on newlines so a section typed as a list reads as one. The council's
                // template uses bullets throughout, and a five-item list collapsed into a single
                // run of prose is the difference between a document someone scans and one they
                // have to read twice.
                foreach (var line in SplitLines(section.Content))
                {
                    body.Append(TextParagraph(line, 20, "000000"));
                }
            }

            main.Document = new W.Document(body);
        }

        // ToArray after the package is disposed: disposing flushes the final ZIP entries, and
        // MemoryStream.ToArray still works on a disposed stream. Reading it earlier returns a
        // truncated, unopenable file.
        return stream.ToArray();
    }

    private static W.Paragraph TextParagraph(string text, int halfPoints, string rgb, bool bold = false)
    {
        // Schema order inside w:rPr: b, then color, then sz. Not alphabetical, not arbitrary —
        // see the class comment.
        var properties = new W.RunProperties();
        if (bold) properties.Append(new W.Bold());
        properties.Append(new W.Color { Val = rgb });
        properties.Append(new W.FontSize { Val = halfPoints.ToString() });

        var run = new W.Run(properties);

        // Report bodies are author-written prose and contain newlines. Without an explicit break
        // element they would all collapse onto one line, since XML whitespace is not layout.
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) run.Append(new W.Break());
            run.Append(new W.Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
        }

        return new W.Paragraph(run);
    }

    // --- DOCX: tabular documents (snapshot comparison) ---

    public static byte[] BuildTabularDocx(ExportDocument document, string purple, string secondary)
    {
        using var stream = new MemoryStream();

        using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = wordDocument.AddMainDocumentPart();
            var body = new W.Body();

            body.Append(TextParagraph("Stirling Council", 20, Rgb(purple), bold: true));
            body.Append(TextParagraph(document.Title, 36, "000000", bold: true));
            body.Append(TextParagraph(document.Subtitle, 22, Rgb(secondary)));

            foreach (var line in document.MetaLines)
                body.Append(TextParagraph(line, 16, Rgb(secondary)));

            foreach (var table in document.Tables)
            {
                body.Append(TextParagraph(table.Heading, 26, Rgb(purple), bold: true));

                if (table.Rows.Count == 0)
                {
                    body.Append(TextParagraph(table.EmptyMessage ?? "No entries.", 20, Rgb(secondary)));
                    continue;
                }

                body.Append(Table(table, purple));

                // Word merges two adjacent tables into one if nothing separates them, which would
                // silently run six sections together into a single unreadable grid.
                body.Append(new W.Paragraph());
            }

            main.Document = new W.Document(body);
        }

        return stream.ToArray();
    }

    // --- Status report (council template PD.01.25, restyled) ---

    /// <summary>Body text size in half-points — 10.5pt.</summary>
    private const int BodyHalfPoints = 21;

    /// <summary>A4 portrait, in twentieths of a point.</summary>
    private const int PageWidth = 11906;
    private const int PageHeight = 16838;
    private const int SideMargin = 851;

    /// <summary>Usable width between the margins: what a full-bleed element spans.</summary>
    private const int ContentWidth = PageWidth - (SideMargin * 2);

    private const string RuleGrey = "E4E0EC";
    private const string MutedInk = "5C6770";
    private const string BodyInk = "1F1B2A";

    /// <summary>
    /// The Infrastructure Delivery status report.
    ///
    /// Same headings, same information and same document control as the council's template
    /// PD.01.25, restyled. Four decisions carry the difference, and each is a deliberate departure
    /// from the original rather than an accident of generation:
    ///
    ///   Cell borders are gone. The template rules every cell on four sides, which is what makes a
    ///   document read as a spreadsheet. One hairline separates each section instead.
    ///
    ///   The title sits in a solid purple band, giving the page somewhere to start and putting the
    ///   brand colour where it carries identity rather than decoration.
    ///
    ///   Labels are demoted and values promoted. In the template "Project Sponsor:" and the
    ///   sponsor's name are the same size and weight, so the eye lands on the question rather than
    ///   the answer.
    ///
    ///   Sections are set as real bullets with a hanging indent, because that is how they are
    ///   written. Multi-line content previously ran together as prose.
    ///
    /// Green carries the six section headings and purple the band. Each colour does one job:
    /// purple says whose document this is, green says where you are in it.
    /// </summary>
    public static byte[] BuildStatusReportDocx(
        CommitteeReportDto report,
        IReadOnlyList<ReportSectionDto> sections,
        string purple,
        string green)
    {
        using var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            AppendDocumentDefaults(main);
            var bulletNumberId = AppendBulletNumbering(main);

            var body = new W.Body();

            body.Append(HeaderBand(report, purple));
            body.Append(Spacer(120));
            body.Append(FactsTable(report, green));

            var first = true;
            foreach (var section in sections)
            {
                // A top border on the heading is the separator. Rules between sections rather than
                // around every cell — the single change that does most of the work.
                body.Append(SectionHeading(section.Heading, green, rule: !first));
                first = false;

                if (string.IsNullOrWhiteSpace(section.Content))
                {
                    // The heading stays even when nothing is written under it. On a controlled
                    // template every heading is expected to be present, and an absent one reads as
                    // a template someone has edited.
                    body.Append(Spacer(80));
                    continue;
                }

                foreach (var line in SplitLines(section.Content))
                {
                    body.Append(BulletParagraph(line, bulletNumberId));
                }
            }

            main.Document = new W.Document(body);
            AppendDocumentControlFooter(main, green);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Sets Segoe UI as the document default.
    ///
    /// Named explicitly rather than left to Word's default, which differs by version — Calibri on
    /// older installs, Aptos on newer ones — so the same report would look different depending on
    /// who opened it. Segoe UI is present on every Windows machine.
    /// </summary>
    private static void AppendDocumentDefaults(MainDocumentPart main)
    {
        var stylesPart = main.AddNewPart<StyleDefinitionsPart>();

        stylesPart.Styles = new W.Styles(
            new W.DocDefaults(
                new W.RunPropertiesDefault(
                    new W.RunPropertiesBaseStyle(
                        new W.RunFonts { Ascii = "Segoe UI", HighAnsi = "Segoe UI", ComplexScript = "Segoe UI" },
                        new W.Color { Val = BodyInk },
                        new W.FontSize { Val = BodyHalfPoints.ToString() })),
                new W.ParagraphPropertiesDefault(
                    new W.ParagraphPropertiesBaseStyle(
                        // Space after each paragraph and slightly open leading. The template had
                        // neither, which is the largest single cause of it reading as cramped.
                        new W.SpacingBetweenLines
                        {
                            After = "80",
                            Line = "276",
                            LineRule = W.LineSpacingRuleValues.Auto,
                        }))));

        stylesPart.Styles.Save();
    }

    /// <summary>
    /// Defines the bullet used by the six sections, with a hanging indent so a wrapped line sits
    /// under the text rather than under the bullet.
    /// </summary>
    private static int AppendBulletNumbering(MainDocumentPart main)
    {
        const int abstractId = 40;
        const int numberId = 41;

        var numberingPart = main.AddNewPart<NumberingDefinitionsPart>();

        var level = new W.Level(
            new W.StartNumberingValue { Val = 1 },
            new W.NumberingFormat { Val = W.NumberFormatValues.Bullet },
            new W.LevelText { Val = "•" },
            new W.LevelJustification { Val = W.LevelJustificationValues.Left },
            new W.PreviousParagraphProperties(
                new W.Indentation { Left = "284", Hanging = "170" }),
            new W.NumberingSymbolRunProperties(
                new W.RunFonts { Ascii = "Segoe UI", HighAnsi = "Segoe UI" }))
        { LevelIndex = 0 };

        numberingPart.Numbering = new W.Numbering(
            new W.AbstractNum(level) { AbstractNumberId = abstractId },
            new W.NumberingInstance(
                new W.AbstractNumId { Val = abstractId })
            { NumberID = numberId });

        numberingPart.Numbering.Save();
        return numberId;
    }

    /// <summary>
    /// The purple band carrying the service and the report name.
    ///
    /// A single-cell shaded table rather than a shaded paragraph, because a paragraph's shading
    /// stops at the text and a table cell can be given real padding on all four sides.
    /// </summary>
    private static W.Table HeaderBand(CommitteeReportDto report, string purple)
    {
        var cell = new W.TableCell(
            new W.TableCellProperties(
                new W.TableCellWidth { Width = ContentWidth.ToString(), Type = W.TableWidthUnitValues.Dxa },
                new W.Shading { Val = W.ShadingPatternValues.Clear, Color = "auto", Fill = Rgb(purple) },
                new W.TableCellMargin(
                    new W.TopMargin { Width = "260", Type = W.TableWidthUnitValues.Dxa },
                    new W.LeftMargin { Width = "280", Type = W.TableWidthUnitValues.Dxa },
                    new W.BottomMargin { Width = "240", Type = W.TableWidthUnitValues.Dxa },
                    new W.RightMargin { Width = "280", Type = W.TableWidthUnitValues.Dxa })),
            StyledParagraph(
                "Infrastructure Delivery · Programme Governance",
                16, "FFFFFF", bold: true, caps: true, letterSpacing: 30, spaceAfter: 40),
            StyledParagraph(
                "Programme/Project Status Report",
                38, "FFFFFF", bold: true, spaceAfter: 0));

        return new W.Table(
            new W.TableProperties(
                new W.TableWidth { Width = ContentWidth.ToString(), Type = W.TableWidthUnitValues.Dxa },
                NoBorders()),
            new W.TableGrid(new W.GridColumn { Width = ContentWidth.ToString() }),
            new W.TableRow(cell));
    }

    /// <summary>
    /// The six header facts as three columns of stacked label-over-value, with no cell borders at
    /// all and one hairline beneath the block.
    /// </summary>
    private static W.Table FactsTable(CommitteeReportDto report, string green)
    {
        var column = ContentWidth / 3;

        var budget = report.ApprovedBudget > 0
            ? report.ApprovedBudget.ToString("C2", Culture)
            : string.Empty;

        var facts = new (string Label, string Value)[]
        {
            ("Project / Programme", report.ProjectName),
            ("Project ID Ref", report.ProjectRef),
            ("Report Date", FormatReportDate(report)),
            ("Project Sponsor", report.SponsorName ?? string.Empty),
            ("Project Manager", report.ProjectManagerName ?? string.Empty),
            ("Budget", budget),
        };

        var table = new W.Table(
            new W.TableProperties(
                new W.TableWidth { Width = ContentWidth.ToString(), Type = W.TableWidthUnitValues.Dxa },
                NoBorders(),
                new W.TableCellMarginDefault(
                    new W.TopMargin { Width = "120", Type = W.TableWidthUnitValues.Dxa },
                    new W.TableCellLeftMargin { Width = 0, Type = W.TableWidthValues.Dxa },
                    new W.BottomMargin { Width = "220", Type = W.TableWidthUnitValues.Dxa },
                    new W.TableCellRightMargin { Width = 220, Type = W.TableWidthValues.Dxa })),
            new W.TableGrid(
                new W.GridColumn { Width = column.ToString() },
                new W.GridColumn { Width = column.ToString() },
                new W.GridColumn { Width = column.ToString() }));

        foreach (var row in facts.Chunk(3))
        {
            var tableRow = new W.TableRow();

            foreach (var (label, value) in row)
            {
                tableRow.Append(new W.TableCell(
                    new W.TableCellProperties(
                        new W.TableCellWidth { Width = column.ToString(), Type = W.TableWidthUnitValues.Dxa }),
                    // Label small, spaced and grey; value larger and bold. The eye should land on
                    // the answer, not the question.
                    StyledParagraph(label, 15, MutedInk, bold: true, caps: true, letterSpacing: 22, spaceAfter: 20),
                    StyledParagraph(
                        string.IsNullOrWhiteSpace(value) ? " " : value,
                        22, BodyInk, bold: true, spaceAfter: 0)));
            }

            table.Append(tableRow);
        }

        return table;
    }

    /// <summary>A section heading: green, spaced small caps, with a hairline above it.</summary>
    private static W.Paragraph SectionHeading(string heading, string green, bool rule)
    {
        var paragraph = StyledParagraph(
            heading, 17, Rgb(green), bold: true, caps: true, letterSpacing: 24,
            spaceBefore: rule ? 280 : 200, spaceAfter: 100);

        if (rule)
        {
            var properties = paragraph.GetFirstChild<W.ParagraphProperties>()!;

            // pBdr comes before spacing in the schema's ordering for w:pPr — inserted first rather
            // than appended, since StyledParagraph has already added spacing.
            properties.InsertAt(
                new W.ParagraphBorders(
                    new W.TopBorder
                    {
                        Val = W.BorderValues.Single,
                        Size = 4,
                        Color = RuleGrey,
                        Space = 8,
                    }),
                0);
        }

        return paragraph;
    }

    private static W.Paragraph BulletParagraph(string text, int numberId)
    {
        var properties = new W.ParagraphProperties(
            new W.NumberingProperties(
                new W.NumberingLevelReference { Val = 0 },
                new W.NumberingId { Val = numberId }),
            new W.SpacingBetweenLines { After = "60", Line = "264", LineRule = W.LineSpacingRuleValues.Auto });

        var run = new W.Run(
            new W.RunProperties(
                new W.Color { Val = BodyInk },
                new W.FontSize { Val = BodyHalfPoints.ToString() }),
            new W.Text(text) { Space = SpaceProcessingModeValues.Preserve });

        return new W.Paragraph(properties, run);
    }

    private static W.Paragraph Spacer(int twips) =>
        new(new W.ParagraphProperties(
                new W.SpacingBetweenLines { After = twips.ToString(), Line = "20", LineRule = W.LineSpacingRuleValues.Exact }));

    /// <summary>
    /// A paragraph with the full run of styling the design uses.
    ///
    /// Both element orders here are schema-enforced and neither is obvious:
    ///   w:pPr — pBdr, spacing, ind, jc, rPr
    ///   w:rPr — rFonts, b, caps, color, spacing, sz
    /// Getting either wrong produces a file Word declares corrupt rather than one that looks odd.
    /// </summary>
    private static W.Paragraph StyledParagraph(
        string text,
        int halfPoints,
        string rgb,
        bool bold = false,
        bool caps = false,
        int letterSpacing = 0,
        int spaceBefore = 0,
        int spaceAfter = 80)
    {
        var spacing = new W.SpacingBetweenLines { After = spaceAfter.ToString() };
        if (spaceBefore > 0) spacing.Before = spaceBefore.ToString();

        var paragraphProperties = new W.ParagraphProperties(spacing);

        var runProperties = new W.RunProperties(
            new W.RunFonts { Ascii = "Segoe UI", HighAnsi = "Segoe UI", ComplexScript = "Segoe UI" });

        if (bold) runProperties.Append(new W.Bold());
        if (caps) runProperties.Append(new W.Caps());
        runProperties.Append(new W.Color { Val = rgb });
        if (letterSpacing > 0) runProperties.Append(new W.Spacing { Val = letterSpacing });
        runProperties.Append(new W.FontSize { Val = halfPoints.ToString() });

        var run = new W.Run(runProperties, new W.Text(text) { Space = SpaceProcessingModeValues.Preserve });

        return new W.Paragraph(paragraphProperties, run);
    }

    /// <summary>No borders at all — the default, stated explicitly so the intent is visible.</summary>
    private static W.TableBorders NoBorders() => new(
        new W.TopBorder { Val = W.BorderValues.None },
        new W.LeftBorder { Val = W.BorderValues.None },
        new W.BottomBorder { Val = W.BorderValues.None },
        new W.RightBorder { Val = W.BorderValues.None },
        new W.InsideHorizontalBorder { Val = W.BorderValues.None },
        new W.InsideVerticalBorder { Val = W.BorderValues.None });

    /// <summary>
    /// The report date as the template writes it. Blank rather than a placeholder when unset: an
    /// empty value reads as "not filled in yet", which is true, where a dash reads as a decision.
    /// </summary>
    private static string FormatReportDate(CommitteeReportDto report) =>
        report.ReportDate?.ToString("d MMMM yyyy", Culture)
        ?? report.MeetingDate?.ToString("d MMMM yyyy", Culture)
        ?? string.Empty;

    /// <summary>
    /// The controlled-document footer, above a hairline.
    ///
    /// The council logo belongs here or in a strip above the band, and is not yet placed: the file
    /// has not been supplied. Word embeds an image in a footer without difficulty, so this is an
    /// insertion rather than a rearrangement when it arrives.
    /// </summary>
    private static void AppendDocumentControlFooter(MainDocumentPart main, string green)
    {
        var footerPart = main.AddNewPart<FooterPart>();

        var paragraph = StyledParagraph(
            "Doc No: PD.01.25      Issued by: Project Delivery      Date: Jun-17      Version: 01",
            14, MutedInk, letterSpacing: 8, spaceAfter: 0);

        paragraph.GetFirstChild<W.ParagraphProperties>()!.InsertAt(
            new W.ParagraphBorders(
                new W.TopBorder { Val = W.BorderValues.Single, Size = 4, Color = RuleGrey, Space = 6 }),
            0);

        footerPart.Footer = new W.Footer(paragraph);
        footerPart.Footer.Save();

        var footerId = main.GetIdOfPart(footerPart);

        // sectPr goes last in the body, after all content. A section properties element among the
        // paragraphs is a validation error rather than something Word tolerates.
        main.Document.Body!.Append(new W.SectionProperties(
            new W.FooterReference { Type = W.HeaderFooterValues.Default, Id = footerId },
            new W.PageSize { Width = (uint)PageWidth, Height = (uint)PageHeight },
            new W.PageMargin
            {
                Top = 851,
                Right = (uint)SideMargin,
                Bottom = 851,
                Left = (uint)SideMargin,
                Header = 567,
                Footer = 454,
            }));

        main.Document.Save();
    }

    private static W.Table Table(ExportTable table, string purple)
    {
        // Three orderings matter here and none of them is alphabetical or intuitive; all three
        // were got wrong first time and caught by OpenXmlValidator rather than by reading.
        //   w:tblPr    — tblW before tblBorders
        //   tblBorders — top, left, bottom, right, insideH, insideV
        //   w:tbl      — tblGrid before any tr
        var element = new W.Table(
            new W.TableProperties(
                new W.TableWidth { Width = "5000", Type = W.TableWidthUnitValues.Pct },
                new W.TableBorders(
                    new W.TopBorder { Val = W.BorderValues.Single, Size = 4, Color = "D9D9D9" },
                    new W.LeftBorder { Val = W.BorderValues.Single, Size = 4, Color = "D9D9D9" },
                    new W.BottomBorder { Val = W.BorderValues.Single, Size = 4, Color = "D9D9D9" },
                    new W.RightBorder { Val = W.BorderValues.Single, Size = 4, Color = "D9D9D9" },
                    new W.InsideHorizontalBorder { Val = W.BorderValues.Single, Size = 4, Color = "D9D9D9" },
                    new W.InsideVerticalBorder { Val = W.BorderValues.Single, Size = 4, Color = "D9D9D9" })),
            // Equal-width columns. The grid is required even when the widths are nominal — Word
            // uses it to lay the table out, and a table without one is invalid.
            new W.TableGrid(table.Columns.Select(_ => new W.GridColumn())));

        var header = new W.TableRow();
        foreach (var column in table.Columns)
            header.Append(Cell(column, 18, Rgb(purple), bold: true, shadeHex: "EFECF5"));
        element.Append(header);

        foreach (var row in table.Rows)
        {
            var tableRow = new W.TableRow();
            foreach (var cell in row) tableRow.Append(Cell(cell, 18, "000000"));
            element.Append(tableRow);
        }

        return element;
    }

    private static W.TableCell Cell(string text, int halfPoints, string rgb, bool bold = false, string? shadeHex = null)
    {
        var properties = new W.TableCellProperties();
        if (shadeHex is not null)
        {
            properties.Append(new W.Shading
            {
                Val = W.ShadingPatternValues.Clear,
                Color = "auto",
                Fill = shadeHex,
            });
        }

        // A table cell must contain at least one paragraph; an empty one produces a file Word
        // reports as corrupt rather than one with a blank cell.
        return new W.TableCell(properties, TextParagraph(text, halfPoints, rgb, bold));
    }

    // --- PPTX ---

    // 16:9 at 914,400 EMU per inch — the default for every version of PowerPoint since 2013.
    private const long SlideWidth = 12192000;
    private const long SlideHeight = 6858000;

    public static byte[] BuildPptx(
        CommitteeReportDto report,
        IReadOnlyList<ReportSectionDto> sections,
        string purple,
        string secondary)
    {
        var subtitle = $"{report.ReportType} · {report.ProjectRef} — {report.ProjectName}";
        if (report.MeetingDate.HasValue)
            subtitle += $"\nMeeting date: {report.MeetingDate:d MMMM yyyy}";

        var slides = new List<(string Title, string Body)> { (report.Title, subtitle) };

        // One slide per populated section. Long sections are not paginated across slides — see
        // docs/roadmap.md; a section longer than a slide overflows its text box rather than
        // continuing onto a second slide.
        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section.Content)) continue;
            slides.Add((section.Heading, section.Content));
        }

        return BuildPptxFrom(slides, purple, secondary, monospacedBody: false);
    }

    /// <summary>
    /// Assembles a presentation from a list of title/body slides.
    ///
    /// A presentation will not open without a slide master, a layout and a theme, even when every
    /// slide is blank and none of them is referenced for styling — they are structural
    /// requirements of the format rather than design choices, which is why this scaffolding is
    /// here at all and why it is built once rather than per caller.
    /// </summary>
    private static byte[] BuildPptxFrom(
        IReadOnlyList<(string Title, string Body)> slides,
        string purple,
        string secondary,
        bool monospacedBody)
    {
        using var stream = new MemoryStream();

        using (var presentation = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
        {
            var presentationPart = presentation.AddPresentationPart();
            presentationPart.Presentation = new P.Presentation();

            var masterPart = presentationPart.AddNewPart<SlideMasterPart>("rIdMaster");
            var layoutPart = masterPart.AddNewPart<SlideLayoutPart>("rIdLayout");

            layoutPart.SlideLayout = new P.SlideLayout(
                new P.CommonSlideData(EmptyShapeTree()),
                new P.ColorMapOverride(new D.MasterColorMapping()))
            { Type = P.SlideLayoutValues.Blank };

            masterPart.SlideMaster = new P.SlideMaster(
                new P.CommonSlideData(EmptyShapeTree()),
                DefaultColorMap(),
                new P.SlideLayoutIdList(new P.SlideLayoutId { Id = 2147483649U, RelationshipId = "rIdLayout" }));

            masterPart.AddNewPart<ThemePart>("rIdTheme").Theme = MinimalTheme(purple, secondary);

            var slideIdList = new P.SlideIdList();
            var slideId = 256U;
            var slideNumber = 1;

            foreach (var (title, body) in slides)
            {
                var relationshipId = $"rIdSlide{slideNumber}";
                var slidePart = presentationPart.AddNewPart<SlidePart>(relationshipId);
                slidePart.Slide = BuildSlide(title, body, purple, monospacedBody);
                slidePart.AddPart(layoutPart, $"rIdLayout{slideNumber}");

                slideIdList.Append(new P.SlideId { Id = slideId++, RelationshipId = relationshipId });
                slideNumber++;
            }

            presentationPart.Presentation.Append(
                new P.SlideMasterIdList(new P.SlideMasterId { Id = 2147483648U, RelationshipId = "rIdMaster" }),
                slideIdList,
                new P.SlideSize { Cx = (Int32Value)SlideWidth, Cy = (Int32Value)SlideHeight },
                new P.NotesSize { Cx = 6858000, Cy = 9144000 });
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Tables rendered as aligned text lines, one slide per table.
    ///
    /// PresentationML can draw a real table (a graphicFrame wrapping a:tbl), and it is not worth
    /// it here: a comparison table runs to six columns and a slide is not where anyone reads six
    /// columns of figures. The deck exists to be talked over, and the PDF and XLSX exports carry
    /// the same content in a form suited to being read. Columns are padded to align, which is why
    /// the shape uses a monospaced typeface.
    /// </summary>
    public static byte[] BuildTabularPptx(ExportDocument document, string purple, string secondary)
    {
        var slides = new List<(string Title, string Body)>
        {
            (document.Title, string.Join("\n", new[] { document.Subtitle }.Concat(document.MetaLines))),
        };

        foreach (var table in document.Tables)
        {
            slides.Add((
                table.Heading,
                table.Rows.Count == 0
                    ? table.EmptyMessage ?? "No entries."
                    : RenderAsText(table)));
        }

        return BuildPptxFrom(slides, purple, secondary, monospacedBody: true);
    }

    private static string RenderAsText(ExportTable table)
    {
        var widths = table.Columns
            .Select((column, index) => Math.Max(
                column.Length,
                table.Rows.Count == 0 ? 0 : table.Rows.Max(row => index < row.Count ? row[index].Length : 0)))
            .ToArray();

        // Capped so one long description cannot push every later column off the slide.
        const int maxWidth = 28;
        for (var i = 0; i < widths.Length; i++) widths[i] = Math.Min(widths[i], maxWidth);

        var lines = new List<string> { Line(table.Columns, widths) };
        lines.AddRange(table.Rows.Select(row => Line(row, widths)));

        return string.Join("\n", lines);
    }

    private static string Line(IReadOnlyList<string> cells, int[] widths) =>
        string.Join("  ", cells.Select((cell, i) =>
        {
            var width = i < widths.Length ? widths[i] : cell.Length;
            var text = cell.Length <= width ? cell : cell[..Math.Max(0, width - 1)] + "…";
            return text.PadRight(width);
        })).TrimEnd();

    private static P.Slide BuildSlide(string title, string body, string purple, bool monospacedBody)
    {
        var tree = EmptyShapeTree();

        tree.Append(TextShape(2U, "Title", title,
            x: 457200, y: 685800, cx: 11277600, cy: 1143000, fontSize: 3200, bold: true, rgb: Rgb(purple)));

        // Tabular slides use a smaller monospaced face: the columns are aligned with padding
        // spaces, which a proportional font would immediately undo.
        tree.Append(TextShape(3U, "Body", body,
            x: 457200, y: 2057400, cx: 11277600, cy: 3600000,
            fontSize: monospacedBody ? 1100 : 1800, bold: false, rgb: "1A1A1A",
            typeface: monospacedBody ? "Consolas" : null));

        return new P.Slide(new P.CommonSlideData(tree), new P.ColorMapOverride(new D.MasterColorMapping()));
    }

    private static P.Shape TextShape(uint id, string name, string text,
        long x, long y, long cx, long cy, int fontSize, bool bold, string rgb, string? typeface = null)
    {
        var textBody = new P.TextBody(
            new D.BodyProperties { Wrap = D.TextWrappingValues.Square },
            new D.ListStyle());

        // One drawing paragraph per line: unlike Word, PresentationML has no break element inside
        // a run, so a multi-line body becomes multiple paragraphs.
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            var runProperties = new D.RunProperties(new D.SolidFill(new D.RgbColorModelHex { Val = rgb }))
            {
                Language = "en-GB",
                FontSize = fontSize,
                Bold = bold,
            };

            // Schema order: the fill is already in place, and the latin typeface follows it.
            if (typeface is not null)
                runProperties.Append(new D.LatinFont { Typeface = typeface });

            textBody.Append(new D.Paragraph(new D.Run(runProperties, new D.Text(line))));
        }

        return new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = id, Name = name },
                new P.NonVisualShapeDrawingProperties(new D.ShapeLocks { NoGrouping = true }),
                new P.ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                new D.Transform2D(new D.Offset { X = x, Y = y }, new D.Extents { Cx = cx, Cy = cy }),
                new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle }),
            textBody);
    }

    private static P.ShapeTree EmptyShapeTree() => new(
        new P.NonVisualGroupShapeProperties(
            new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
            new P.NonVisualGroupShapeDrawingProperties(),
            new P.ApplicationNonVisualDrawingProperties()),
        new P.GroupShapeProperties(new D.TransformGroup()));

    private static P.ColorMap DefaultColorMap() => new()
    {
        Background1 = D.ColorSchemeIndexValues.Light1,
        Text1 = D.ColorSchemeIndexValues.Dark1,
        Background2 = D.ColorSchemeIndexValues.Light2,
        Text2 = D.ColorSchemeIndexValues.Dark2,
        Accent1 = D.ColorSchemeIndexValues.Accent1,
        Accent2 = D.ColorSchemeIndexValues.Accent2,
        Accent3 = D.ColorSchemeIndexValues.Accent3,
        Accent4 = D.ColorSchemeIndexValues.Accent4,
        Accent5 = D.ColorSchemeIndexValues.Accent5,
        Accent6 = D.ColorSchemeIndexValues.Accent6,
        Hyperlink = D.ColorSchemeIndexValues.Hyperlink,
        FollowedHyperlink = D.ColorSchemeIndexValues.FollowedHyperlink,
    };

    /// <summary>
    /// The minimum theme a presentation needs, with Stirling's palette in the two accent slots
    /// so a user restyling the deck in PowerPoint gets the council's colours rather than Office's.
    /// </summary>
    private static D.Theme MinimalTheme(string purple, string secondary) => new(
        new D.ThemeElements(
            new D.ColorScheme(
                new D.Dark1Color(new D.SystemColor { Val = D.SystemColorValues.WindowText }),
                new D.Light1Color(new D.SystemColor { Val = D.SystemColorValues.Window }),
                new D.Dark2Color(new D.RgbColorModelHex { Val = "44546A" }),
                new D.Light2Color(new D.RgbColorModelHex { Val = "E7E6E6" }),
                new D.Accent1Color(new D.RgbColorModelHex { Val = Rgb(purple) }),
                new D.Accent2Color(new D.RgbColorModelHex { Val = Rgb(secondary) }),
                new D.Accent3Color(new D.RgbColorModelHex { Val = "A5A5A5" }),
                new D.Accent4Color(new D.RgbColorModelHex { Val = "FFC000" }),
                new D.Accent5Color(new D.RgbColorModelHex { Val = "5B9BD5" }),
                new D.Accent6Color(new D.RgbColorModelHex { Val = "70AD47" }),
                new D.Hyperlink(new D.RgbColorModelHex { Val = "0563C1" }),
                new D.FollowedHyperlinkColor(new D.RgbColorModelHex { Val = "954F72" }))
            { Name = "Stirling" },
            new D.FontScheme(
                new D.MajorFont(
                    new D.LatinFont { Typeface = "Calibri Light" },
                    new D.EastAsianFont { Typeface = "" },
                    new D.ComplexScriptFont { Typeface = "" }),
                new D.MinorFont(
                    new D.LatinFont { Typeface = "Calibri" },
                    new D.EastAsianFont { Typeface = "" },
                    new D.ComplexScriptFont { Typeface = "" }))
            { Name = "Stirling" },
            new D.FormatScheme(
                new D.FillStyleList(PhSolidFill(), PhSolidFill(), PhSolidFill()),
                new D.LineStyleList(PhOutline(6350), PhOutline(12700), PhOutline(19050)),
                new D.EffectStyleList(
                    new D.EffectStyle(new D.EffectList()),
                    new D.EffectStyle(new D.EffectList()),
                    new D.EffectStyle(new D.EffectList())),
                new D.BackgroundFillStyleList(PhSolidFill(), PhSolidFill(), PhSolidFill()))
            { Name = "Stirling" }))
    { Name = "Stirling Theme" };

    // The format scheme requires exactly three entries in each list; PhColor is the placeholder
    // that means "whatever colour this style is applied with".
    private static D.SolidFill PhSolidFill() =>
        new(new D.SchemeColor { Val = D.SchemeColorValues.PhColor });

    private static D.Outline PhOutline(int width) =>
        new(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })) { Width = width };
}
