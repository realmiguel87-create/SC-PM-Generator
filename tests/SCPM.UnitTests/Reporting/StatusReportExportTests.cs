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
    private const string Purple = "675A8F";
    private const string Green = "4F8377";

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
    public void Puts_the_report_name_in_a_purple_band()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);

        var band = document.MainDocumentPart!.Document.Body!
            .Descendants<W.Table>().First();

        band.Descendants<W.Shading>().Single().Fill!.Value
            .Should().Be(Purple, "the band is where the brand colour carries identity");

        band.InnerText.Should().Contain("Infrastructure Delivery");
        band.InnerText.Should().Contain("Programme/Project Status Report");
    }

    [Fact]
    public void Sets_section_headings_in_forest_green()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);

        var headings = document.MainDocumentPart!.Document.Body!
            .Descendants<W.Paragraph>()
            .Where(p => p.InnerText.StartsWith("Key Activities")
                     || p.InnerText == "Issues"
                     || p.InnerText == "Cost/Budget Position")
            .ToList();

        headings.Should().HaveCount(3);

        // Each colour does one job: purple says whose document this is, green says where you are
        // in it. Green appearing in two roles would mean it signalled neither.
        headings.Should().OnlyContain(
            p => p.Descendants<W.Color>().Any(c => c.Val! == Green));
        headings.Should().OnlyContain(p => p.Descendants<W.Caps>().Any());
    }

    [Fact]
    public void Separates_sections_with_a_rule_but_not_the_first()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);

        var body = document.MainDocumentPart!.Document.Body!;

        var firstHeading = body.Descendants<W.Paragraph>()
            .First(p => p.InnerText.StartsWith("Key Activities"));
        var secondHeading = body.Descendants<W.Paragraph>()
            .First(p => p.InnerText == "Planned Activities");

        // A rule above the first section would sit directly under the facts block, which already
        // has one — two hairlines a few millimetres apart reads as a mistake.
        firstHeading.Descendants<W.ParagraphBorders>().Should().BeEmpty();
        secondHeading.Descendants<W.ParagraphBorders>().Should().ContainSingle();
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
    public void Names_the_typeface_rather_than_leaving_it_to_word()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = Open(stream);

        var defaultFont = document.MainDocumentPart!.StyleDefinitionsPart!
            .Styles!.Descendants<W.RunFonts>().First();

        // Word's default differs by version — Calibri on older installs, Aptos on newer — so the
        // same report would look different depending on who opened it.
        defaultFont.Ascii!.Value.Should().Be("Segoe UI");
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
