using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using FluentAssertions;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Domain.Common;
using SCPM.Domain.Enums;
using SCPM.Infrastructure.Reporting;
using Xunit;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// The RIBA Stage 0–1 governance pack: Project Mandate, Strategic Business Case, Project Brief,
/// Feasibility Study, Project Execution Plan.
///
/// These assert what distinguishes a template from a document: that every heading is present even
/// when nothing has been written, that the author guidance appears only while a section is empty,
/// and that no document claims a controlled-document number the council has not issued. Each is a
/// decision a later change could undo without breaking anything that looks like a test.
///
/// What they cannot establish is whether the documents look right. LibreOffice is unavailable in
/// this environment — it cannot open the council's own template either — so nothing here has been
/// rendered. That gap is closed by a person opening them in Word.
///
/// All fixture data is invented.
/// </summary>
public class StageDocumentExportTests
{
    private const string Purple = "3E2880";
    private const string Green = "006D5B";
    private const string PanelTint = "F4F2F8";

    private static CommitteeReportDto Document(CommitteeReportType type, bool filled = false)
    {
        var definition = StageDocuments.Find(type)!;

        return new CommitteeReportDto
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ProjectName = "Bridgeview Primary School – Hall Refurbishment",
            ProjectRef = "ID00001",
            ReportType = type.ToString(),
            Title = definition.Title,
            Status = "Draft",
            CreatedDate = DateTime.UtcNow,
            ReportDate = new DateOnly(2026, 11, 3),
            SponsorName = "A. Sample",
            ProjectManagerName = "B. Example",
            ApprovedBudget = 412_500m,
            Sections = definition.Sections
                .Select(s => new ReportSectionDto
                {
                    Key = s.Key,
                    Heading = s.Heading,
                    Prompt = s.Prompt,
                    Content = filled ? $"Written content for {s.Heading}." : null,
                })
                .ToList(),
        };
    }

    private static byte[] Export(CommitteeReportDto report) =>
        new CommitteeReportExporter()
            .ExportAsync(report, ReportExportFormat.Docx, CancellationToken.None)
            .GetAwaiter().GetResult();

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

    public static TheoryData<CommitteeReportType> AllFive()
    {
        var data = new TheoryData<CommitteeReportType>();
        foreach (var (type, _) in StageDocuments.All) data.Add(type);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllFive))]
    public void Produces_a_document_word_will_open(CommitteeReportType type)
    {
        using var stream = new MemoryStream(Export(Document(type)));
        using var document = WordprocessingDocument.Open(stream, false);

        AssertValid(document);
    }

    [Theory]
    [MemberData(nameof(AllFive))]
    public void Carries_every_heading_even_when_nothing_is_written(CommitteeReportType type)
    {
        var definition = StageDocuments.Find(type)!;

        using var stream = new MemoryStream(Export(Document(type)));
        using var document = WordprocessingDocument.Open(stream, false);
        var text = document.MainDocumentPart!.Document.Body!.InnerText;

        // A template is the set of headings. One missing from a blank document is the difference
        // between a template and a document someone has already edited.
        foreach (var section in definition.Sections)
        {
            text.Should().Contain(section.Heading);
        }
    }

    [Theory]
    [MemberData(nameof(AllFive))]
    public void Shows_author_guidance_while_a_section_is_empty(CommitteeReportType type)
    {
        var definition = StageDocuments.Find(type)!;

        using var stream = new MemoryStream(Export(Document(type)));
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart!.Document.Body!;

        // Every section of this pack has guidance — the prompts are what make these usable by
        // someone who has not written one before, and a heading without one would be a gap.
        definition.Sections.Should().OnlyContain(s => !string.IsNullOrWhiteSpace(s.Prompt));

        var italics = body.Descendants<W.Paragraph>()
            .Where(p => p.Descendants<W.Italic>().Any())
            .ToList();

        italics.Should().HaveCount(definition.Sections.Count);
        italics.Should().OnlyContain(p => p.Descendants<W.Color>().Any(c => c.Val! == "5C6770"));
    }

    [Theory]
    [MemberData(nameof(AllFive))]
    public void Drops_the_guidance_once_the_section_is_written(CommitteeReportType type)
    {
        var definition = StageDocuments.Find(type)!;

        using var stream = new MemoryStream(Export(Document(type, filled: true)));
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart!.Document.Body!;

        // Guidance left beside real content would end up in a document going to committee. This
        // is the assertion that stops that happening.
        body.Descendants<W.Italic>().Should().BeEmpty();
        body.InnerText.Should().NotContain(definition.Sections[0].Prompt!);

        // And the content is there as bullets, one per section.
        body.Descendants<W.Paragraph>()
            .Count(p => p.Descendants<W.NumberingProperties>().Any())
            .Should().Be(definition.Sections.Count);
    }

    [Theory]
    [MemberData(nameof(AllFive))]
    public void Claims_no_document_number_the_council_has_not_issued(CommitteeReportType type)
    {
        using var stream = new MemoryStream(Export(Document(type)));
        using var document = WordprocessingDocument.Open(stream, false);

        var footer = document.MainDocumentPart!.FooterParts.Single().Footer.InnerText;

        // PD.01.25 is the council's reference for its own status report template. Carrying it, or
        // a plausible-looking sibling, onto a document the council has never issued would put a
        // false reference on a controlled document.
        footer.Should().NotContain("PD.01.25");
        footer.Should().Contain("[to be assigned]");
        footer.Should().Contain(StageDocuments.Find(type)!.Title);
    }

    [Theory]
    [MemberData(nameof(AllFive))]
    public void Shares_the_status_reports_styling(CommitteeReportType type)
    {
        using var stream = new MemoryStream(Export(Document(type)));
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart!.Document.Body!;

        // One look across the family. Five documents that each looked slightly different would
        // undo the reason for restyling the status report in the first place.
        body.Descendants<W.Table>().First()
            .Descendants<W.Shading>().Single().Fill!.Value.Should().Be(Purple);

        body.Descendants<W.TableBorders>()
            .SelectMany(b => b.ChildElements.OfType<W.BorderType>())
            .Should().OnlyContain(b => b.Val! == W.BorderValues.None);

        document.MainDocumentPart.StyleDefinitionsPart!
            .Styles!.Descendants<W.RunFonts>().First()
            .Ascii!.Value.Should().Be("Segoe UI");

        var definition = StageDocuments.Find(type)!;

        var numerals = body.Descendants<W.Paragraph>()
            .Where(p => p.InnerText.Length == 2 && p.InnerText.All(char.IsDigit))
            .ToList();

        numerals.Should().HaveCount(definition.Sections.Count);
        numerals.Should().OnlyContain(p => p.Descendants<W.Color>().Any(c => c.Val! == Green));

        // The facts sit in a tinted panel, which is the second shaded element on the page after
        // the strip. A panel that lost its tint would leave the facts floating.
        body.Descendants<W.Shading>().Select(sh => sh.Fill!.Value)
            .Should().Contain(PanelTint);
    }

    [Theory]
    [MemberData(nameof(AllFive))]
    public void Names_the_riba_stage_and_the_project(CommitteeReportType type)
    {
        var definition = StageDocuments.Find(type)!;

        using var stream = new MemoryStream(Export(Document(type)));
        using var document = WordprocessingDocument.Open(stream, false);
        var text = document.MainDocumentPart!.Document.Body!.InnerText;

        text.Should().Contain(definition.Title);
        text.Should().Contain(definition.RibaStage);
        text.Should().Contain("ID00001");
        text.Should().Contain("3 November 2026");

        // Sterling explicitly, not the server's culture.
        text.Should().Contain("£412,500.00");
    }

    [Fact]
    public void Follows_the_treasury_five_case_model_for_the_business_case()
    {
        var headings = StageDocuments.StrategicBusinessCase.Sections
            .Select(s => s.Heading)
            .ToList();

        // The five cases in the Treasury's order and under their standard names. A reviewer looks
        // for "Economic Case" and expects options appraisal under it; renaming it to something
        // more self-explanatory would cost more in familiarity than it gained in clarity.
        headings.Should().ContainInOrder(
            "Strategic Case", "Economic Case", "Commercial Case", "Financial Case", "Management Case");
    }

    [Fact]
    public void Keeps_the_pack_in_the_order_a_project_meets_it()
    {
        StageDocuments.All.Select(d => d.Type).Should().ContainInOrder(
            CommitteeReportType.ProjectMandate,
            CommitteeReportType.StrategicBusinessCase,
            CommitteeReportType.ProjectBrief,
            CommitteeReportType.FeasibilityStudy,
            CommitteeReportType.ProjectExecutionPlan);
    }

    [Fact]
    public void Leaves_the_status_report_and_committee_paper_out_of_the_pack()
    {
        // Find() is how the exporter decides which layout to use, and both of these have their
        // own. A stray entry here would silently restyle a document already in use.
        StageDocuments.Find(CommitteeReportType.StatusReport).Should().BeNull();
        StageDocuments.Find(CommitteeReportType.CommitteeReport).Should().BeNull();
    }

    [Fact]
    public void Gives_the_status_report_no_prompts()
    {
        // The status report is written through the platform's editor, which carries its own hints.
        // Guidance appearing in a document that has been in use for months would be a change
        // nobody asked for.
        ReportSections.StatusReport.Should().OnlyContain(s => s.Prompt == null);
        ReportSections.CommitteePaper.Should().OnlyContain(s => s.Prompt == null);
    }
}
