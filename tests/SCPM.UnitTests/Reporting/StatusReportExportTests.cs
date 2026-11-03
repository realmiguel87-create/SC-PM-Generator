using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using FluentAssertions;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Infrastructure.Reporting;
using Xunit;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// The Infrastructure Delivery status report — same headings and information as the council's
/// template PD.01.25, restyled.
///
/// These assert the four decisions that carry the restyle, because each is a departure that a
/// later change could silently undo: no cell borders, a purple band, labels demoted below their
/// values, and real bullets. A document that quietly reverted to a ruled grid would still pass a
/// test that only checked the words were present.
///
/// What they cannot establish is whether it looks right. LibreOffice is unavailable in this
/// environment — it cannot open the council's own template either — so nothing here has been
/// rendered. That gap is closed by a person opening it in Word, not by another assertion.
///
/// All fixture data is invented. The council's own status report contains live project information
/// about a named school, a named sponsor and a real budget, none of which belongs in a repository.
/// </summary>
public class StatusReportExportTests
{
    /// <summary>
    /// Measured from the logo file, not sampled by eye. #675A8F and #4F8377 were in use before
    /// the logo arrived as a file; neither is the mark's actual colour, and #4F8377 was 4.33:1
    /// against white — under the 4.5:1 minimum for normal text.
    /// </summary>
    private const string Purple = "3E2880";

    private const string Green = "006D5B";

    private const string BodyInk = "1F1B2A";

    /// <summary>Hermann Zapf, 1948–50, and on every Windows install.</summary>
    private const string Serif = "Palatino Linotype";

    private const string Sans = "Segoe UI";

    private static CommitteeReportDto StatusReport() => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        ProjectName = "Bridgeview Primary School – Hall Refurbishment",
        ProjectRef = "ID00001",
        ReportType = "StatusReport",
        Title = "Programme/Project Status Report",
        Status = "Draft",
        CreatedDate = DateTime.UtcNow,
        ReportDate = new DateOnly(2026, 8, 27),
        SponsorName = "A. Sample",
        ProjectManagerName = "B. Example",
        ApprovedBudget = 412_500m,
        Sections =
        [
            new() { Key = "key-activities", Heading = "Key Activities in previous reporting period",
                    Content = "Detailed design signed off\nCost plan issued to external QS" },
            new() { Key = "planned-activities", Heading = "Planned Activities",
                    Content = "Tender package returned from QS" },
            new() { Key = "issues", Heading = "Issues", Content = "Phasing to be confirmed" },
            new() { Key = "risks", Heading = "Risks", Content = "Warrant timeline could delay mobilisation" },
            new() { Key = "schedule-update", Heading = "Schedule/Programme Update",
                    Content = "3 milestone(s) outstanding, of which 1 are behind baseline." },
            new() { Key = "cost-position", Heading = "Cost/Budget Position",
                    Content = "Approved budget: £412,500." },
        ],
    };

    private static byte[] Export(CommitteeReportDto report) =>
        new CommitteeReportExporter()
            .ExportAsync(report, ReportExportFormat.Docx, CancellationToken.None)
            .GetAwaiter().GetResult();

    private static WordprocessingDocument Open(MemoryStream stream) =>
        WordprocessingDocument.Open(stream, false);

    private static void AssertValid(OpenXmlPackage package)
    {
        var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(package).ToList();

        // Described by hand: FluentAssertions throws while formatting ValidationErrorInfo and
        // hides the very errors being reported.
        if (errors.Count > 0)
        {
            Assert.Fail(string.Join("\n", errors.Select(e => $"{e.Description} at {e.Path?.XPath}")));
        }
    }

    [Fact]
    public void Produces_a_document_word_will_open()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);

        AssertValid(document);
    }

    [Fact]
    public void Draws_no_cell_borders_anywhere()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);

        var borders = document.MainDocumentPart!.Document.Body!
            .Descendants<W.TableBorders>()
            .SelectMany(b => b.ChildElements.OfType<W.BorderType>())
            .ToList();

        // The council's template rules every cell on four sides, which is what makes a document
        // read as a spreadsheet. Removing them is the single change that does most of the work,
        // and it is the one most easily undone by accident.
        borders.Should().NotBeEmpty();
        borders.Should().OnlyContain(b => b.Val! == W.BorderValues.None);
    }

    [Fact]
    public void Names_the_service_in_a_purple_strip_and_sets_the_title_below_it()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);
        var body = document.MainDocumentPart!.Document.Body!;

        var strip = body.Descendants<W.Table>().First();

        strip.Descendants<W.Shading>().Single().Fill!.Value
            .Should().Be(Purple, "the strip is where the brand colour carries identity");
        strip.InnerText.Should().Contain("Infrastructure Delivery");

        // The title is deliberately NOT in the strip. A solid block carrying both the service and
        // the report name gives the two equal weight, where the title is what a reader needs
        // first — so the strip states the service in one line and the title takes the page's
        // largest type on white. Folding it back into the strip is the obvious "tidy-up".
        strip.InnerText.Should().NotContain("Programme/Project Status Report");

        var title = body.Elements<W.Paragraph>()
            .Single(p => p.InnerText == "Programme/Project Status Report");

        // 32pt. Up from 30 when the title was set in a sans: a serif at the same point size
        // reads smaller.
        title.Descendants<W.FontSize>().Single().Val!.Value.Should().Be("64");
        title.Descendants<W.Color>().Single().Val!.Value.Should().Be(BodyInk);
    }

    [Fact]
    public void Sets_the_section_numerals_in_forest_green()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);
        var body = document.MainDocumentPart!.Document.Body!;

        var numerals = body.Descendants<W.Paragraph>()
            .Where(p => p.InnerText.Length == 2 && p.InnerText.All(char.IsDigit))
            .ToList();

        numerals.Should().HaveCount(6);

        // Each colour does one job: purple says whose document this is, green says where you are
        // in it. Green on the numerals and near-black on the headings keeps the heading readable
        // at body weight while the green still marks progress down the page.
        numerals.Should().OnlyContain(
            p => p.Descendants<W.Color>().Any(c => c.Val! == Green));

        var heading = body.Descendants<W.Paragraph>().First(p => p.InnerText == "Issues");

        heading.Descendants<W.Color>().Single().Val!.Value.Should().Be(BodyInk);
        heading.Descendants<W.Caps>().Should().BeEmpty("headings read as sentences, not labels");
    }

    [Fact]
    public void Separates_sections_by_number_rather_than_by_rule()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);

        var body = document.MainDocumentPart!.Document.Body!;

        // A hairline between every section plus a numeral for each would be two separators doing
        // one job. The numerals won, so no paragraph border should remain anywhere in the body —
        // the footer's rule lives in its own part, not here.
        body.Descendants<W.ParagraphBorders>().Should().BeEmpty();

        // Two digits throughout and in document order. A document of six sections numbered 1..6
        // and one of twelve numbered 01..12 would not look like the same family.
        body.Descendants<W.Paragraph>()
            .Select(p => p.InnerText)
            .Where(t => t.Length == 2 && t.All(char.IsDigit))
            .Should().ContainInOrder("01", "02", "03", "04", "05", "06");
    }

    [Fact]
    public void Demotes_the_labels_and_promotes_the_values()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);

        var sponsorCell = document.MainDocumentPart!.Document.Body!
            .Descendants<W.TableCell>()
            .First(c => c.InnerText.Contains("Project Sponsor"));

        var paragraphs = sponsorCell.Elements<W.Paragraph>().ToList();
        var labelSize = int.Parse(paragraphs[0].Descendants<W.FontSize>().Single().Val!);
        var valueSize = int.Parse(paragraphs[1].Descendants<W.FontSize>().Single().Val!);

        // In the template the label and the name are the same size and weight, so the eye lands on
        // the question rather than the answer.
        valueSize.Should().BeGreaterThan(labelSize);
        paragraphs[0].InnerText.Should().Be("Project Sponsor");
        paragraphs[1].InnerText.Should().Be("A. Sample");
    }

    [Fact]
    public void Sets_section_content_as_real_bullets()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);

        var bullets = document.MainDocumentPart!.Document.Body!
            .Descendants<W.Paragraph>()
            .Where(p => p.Descendants<W.NumberingProperties>().Any())
            .ToList();

        // Seven lines of content across six sections — the first has two.
        bullets.Should().HaveCount(7);
        bullets.Select(b => b.InnerText).Should().Contain("Cost plan issued to external QS");

        // A hanging indent, so a wrapped line sits under the text rather than under the bullet.
        var indent = document.MainDocumentPart.NumberingDefinitionsPart!
            .Numbering.Descendants<W.Indentation>().First();

        indent.Hanging!.Value.Should().Be("170");
    }

    [Fact]
    public void Sets_the_prose_in_the_serif_and_the_data_in_the_sans()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);
        var body = document.MainDocumentPart!.Document.Body!;

        // Word's own default differs by version — Calibri on older installs, Aptos on newer — so
        // the same report would look different depending on who opened it.
        document.MainDocumentPart.StyleDefinitionsPart!
            .Styles!.Descendants<W.RunFonts>().First()
            .Ascii!.Value.Should().Be(Serif, "the running text carries the document's voice");

        // The split is the whole point of this setting, and it is the part a later tidy-up would
        // undo by unifying everything onto one face. The prose wants a serif's warmth; the facts
        // panel wants to be scanned, and figures in a serif at this size go soft.
        var heading = body.Descendants<W.Paragraph>().First(p => p.InnerText == "Issues");
        heading.Descendants<W.RunFonts>().Single().Ascii!.Value.Should().Be(Serif);

        var label = body.Descendants<W.Paragraph>().First(p => p.InnerText == "Project Sponsor");
        label.Descendants<W.RunFonts>().Single().Ascii!.Value.Should().Be(Sans);

        var value = body.Descendants<W.Paragraph>().First(p => p.InnerText == "A. Sample");
        value.Descendants<W.RunFonts>().Single().Ascii!.Value.Should().Be(Sans);

        // The footer is document control, which is data about the page rather than the page.
        document.MainDocumentPart.FooterParts.Single()
            .Footer.Descendants<W.RunFonts>().First().Ascii!.Value.Should().Be(Sans);
    }

    [Fact]
    public void Carries_the_headings_and_facts_in_the_councils_wording()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);
        var text = document.MainDocumentPart!.Document.Body!.InnerText;

        // Verbatim, slash included. A heading that is nearly the council's wording is worse than
        // one that matches: the difference is the sort of thing a reader notices and nobody can
        // explain.
        text.Should().Contain("Key Activities in previous reporting period");
        text.Should().Contain("Schedule/Programme Update");
        text.Should().Contain("ID00001");
        text.Should().Contain("27 August 2026");

        // Sterling explicitly, not the server's culture. "$412,500.00" on a council report is a
        // defect only ever found by a reader.
        text.Should().Contain("£412,500.00");
    }

    [Fact]
    public void Carries_the_controlled_document_footer()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);

        var footer = document.MainDocumentPart!.FooterParts.Single();

        // Reproduced because it is what makes the output a version of PD.01.25 rather than a
        // lookalike of it.
        footer.Footer.InnerText.Should().Contain("PD.01.25");
        footer.Footer.InnerText.Should().Contain("Project Delivery");
    }

    [Fact]
    public void Keeps_the_heading_of_a_section_nobody_has_written()
    {
        var report = StatusReport();
        report.Sections.Single(s => s.Key == "issues").Content = null;

        using var stream = new MemoryStream(Export(report));
        using var document = Open(stream);

        AssertValid(document);

        // On a controlled template every heading is expected to be present. An absent "Issues"
        // reads as a template someone has edited rather than a section nobody had anything to say
        // about.
        document.MainDocumentPart!.Document.Body!.InnerText.Should().Contain("Issues");
    }

    [Fact]
    public void Leaves_the_committee_paper_layout_alone()
    {
        var paper = StatusReport();
        paper.ReportType = "CommitteeReport";

        using var stream = new MemoryStream(Export(paper));
        using var document = Open(stream);

        AssertValid(document);

        // Committee papers keep the heading-and-paragraph layout they have always had. The restyle
        // is specific to the council's status report, not a house style imposed on everything.
        document.MainDocumentPart!.Document.Body!
            .Descendants<W.TableRow>().Should().BeEmpty();
        document.MainDocumentPart.Document.Body.InnerText.Should().Contain("Stirling Council");
    }
}
