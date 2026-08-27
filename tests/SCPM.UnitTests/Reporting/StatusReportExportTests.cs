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
/// The Infrastructure Delivery status report, checked against the council's controlled template
/// PD.01.25.
///
/// What these can establish is that the document has the structure the template has: the right
/// grid, the right rows, the headings in the council's own wording, content in the right cells,
/// and a package Word will open. What they cannot establish is whether it *looks* right. Nobody
/// has seen this document — LibreOffice is unavailable in the environment it was built in, and it
/// cannot open the council's own template either, so rendering was not an option. That limitation
/// is real and is recorded rather than glossed.
///
/// All fixture data here is invented. The council's own status report contains live project
/// information about a named school, a named sponsor and a real budget, and none of it belongs in
/// a repository.
/// </summary>
public class StatusReportExportTests
{
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

    private static void AssertValid(OpenXmlPackage package)
    {
        var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(package).ToList();

        // Described by hand rather than through FluentAssertions' formatter, which throws while
        // rendering ValidationErrorInfo and hides the very errors being reported.
        if (errors.Count > 0)
        {
            Assert.Fail(string.Join("\n", errors.Select(e => $"{e.Description} at {e.Path?.XPath}")));
        }
    }

    [Fact]
    public void Produces_a_document_word_will_open()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = WordprocessingDocument.Open(stream, false);

        AssertValid(document);
    }

    [Fact]
    public void Uses_the_councils_own_column_widths()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = WordprocessingDocument.Open(stream, false);

        var grid = document.MainDocumentPart!.Document.Body!
            .Descendants<W.TableGrid>().Single();

        // Taken from the template itself. The proportions are what make a generated report line up
        // with one produced by hand — a table that merely contains the right words in the wrong
        // shape invites a conversation about the tool rather than about the project.
        grid.Elements<W.GridColumn>().Select(c => c.Width?.Value)
            .Should().Equal("3000", "3091", "1984", "2119");
    }

    [Fact]
    public void Lays_out_three_header_rows_and_one_row_per_section()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = WordprocessingDocument.Open(stream, false);

        var rows = document.MainDocumentPart!.Document.Body!
            .Descendants<W.TableRow>().ToList();

        rows.Should().HaveCount(9);

        // Three rows of four cells: label, value, label, value.
        rows.Take(3).Should().OnlyContain(r => r.Elements<W.TableCell>().Count() == 4);

        // Six rows of two, the value spanning the remaining three grid columns.
        rows.Skip(3).Should().OnlyContain(r => r.Elements<W.TableCell>().Count() == 2);
        rows.Skip(3).Should().OnlyContain(
            r => r.Elements<W.TableCell>().Last()
                  .Descendants<W.GridSpan>().Single().Val! == 3);
    }

    [Fact]
    public void Carries_the_headings_in_the_councils_wording()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = WordprocessingDocument.Open(stream, false);
        var text = document.MainDocumentPart!.Document.Body!.InnerText;

        // Verbatim, including the slash and the lower-case "reporting period". A heading that is
        // nearly the council's wording is worse than one that matches it: the difference is the
        // sort of thing a reader notices and nobody can explain.
        text.Should().Contain("Infrastructure Delivery: Programme Governance");
        text.Should().Contain("Programme/Project Status Report");
        text.Should().Contain("Key Activities in previous reporting period:");
        text.Should().Contain("Schedule/Programme Update:");
        text.Should().Contain("Cost/Budget Position:");
    }

    [Fact]
    public void Fills_the_header_block_from_the_project()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = WordprocessingDocument.Open(stream, false);
        var text = document.MainDocumentPart!.Document.Body!.InnerText;

        text.Should().Contain("ID00001");
        text.Should().Contain("A. Sample");
        text.Should().Contain("B. Example");
        text.Should().Contain("27 August 2026");

        // Sterling explicitly, not whatever the server's culture happens to be. A council report
        // rendering "$412,500.00" is a defect only ever found by a reader.
        text.Should().Contain("£412,500.00");
    }

    [Fact]
    public void Renders_a_multi_line_section_as_separate_lines()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = WordprocessingDocument.Open(stream, false);

        var activitiesRow = document.MainDocumentPart!.Document.Body!
            .Descendants<W.TableRow>()
            .First(r => r.InnerText.Contains("Key Activities"));

        // The council's template uses bullet lists throughout. Two items collapsed into one run of
        // prose is the difference between a document someone scans and one they read twice.
        activitiesRow.Elements<W.TableCell>().Last()
            .Elements<W.Paragraph>().Should().HaveCount(2);
    }

    [Fact]
    public void Carries_the_controlled_document_footer()
    {
        using var stream = new MemoryStream(Export(StatusReport()));
        using var document = WordprocessingDocument.Open(stream, false);

        var footer = document.MainDocumentPart!.FooterParts.Single();

        // Reproduced because it is what makes the output a version of PD.01.25 rather than a
        // lookalike of it.
        footer.Footer.InnerText.Should().Contain("PD.01.25");
        footer.Footer.InnerText.Should().Contain("Project Delivery");
    }

    [Fact]
    public void Leaves_the_committee_paper_layout_alone()
    {
        var paper = StatusReport();
        paper.ReportType = "CommitteeReport";

        using var stream = new MemoryStream(Export(paper));
        using var document = WordprocessingDocument.Open(stream, false);

        AssertValid(document);

        // Committee papers keep the heading-and-paragraph layout they have always had. The status
        // report's table is specific to the council's template, not a house style for everything.
        document.MainDocumentPart!.Document.Body!
            .Descendants<W.TableRow>().Should().BeEmpty();
        document.MainDocumentPart.Document.Body.InnerText.Should().Contain("Stirling Council");
    }

    [Fact]
    public void Leaves_an_unwritten_section_blank_rather_than_omitting_its_row()
    {
        var report = StatusReport();
        report.Sections.Single(s => s.Key == "issues").Content = null;

        using var stream = new MemoryStream(Export(report));
        using var document = WordprocessingDocument.Open(stream, false);

        AssertValid(document);

        // The row stays. On a controlled template every heading is expected to be present, and an
        // absent "Issues" row reads as a template someone has edited rather than a section nobody
        // had anything to say about.
        document.MainDocumentPart!.Document.Body!
            .Descendants<W.TableRow>().Should().HaveCount(9);
        document.MainDocumentPart.Document.Body.InnerText.Should().Contain("Issues:");
    }
}
