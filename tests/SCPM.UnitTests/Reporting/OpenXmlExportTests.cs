using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using FluentAssertions;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Infrastructure.Reporting;
using Xunit;
using P = DocumentFormat.OpenXml.Presentation;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// DOCX and PPTX export, verified by re-opening each generated file and running it through
/// OpenXmlValidator.
///
/// "It produced bytes starting with PK" is not the same claim as "Word will open this". Open XML
/// element order is part of the schema, so a run that puts `w:sz` before `w:color` still zips,
/// still unzips, and still looks like a document — while being invalid. That precise mistake was
/// made while writing OpenXmlReportBuilder and was caught here rather than by reading the code,
/// which is why these tests validate rather than sniff magic numbers. It is also why DOCX/PPTX
/// were held back when the first four formats shipped: they could not be checked this way yet.
/// </summary>
public class OpenXmlExportTests
{
    private static readonly CommitteeReportExporter Exporter = new();

    private static CommitteeReportDto SampleReport() => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        ProjectName = "Stirling Community Campus",
        ProjectRef = "PRJ-0001",
        ReportType = "CommitteeReport",
        Title = "Monthly Progress Report",
        Status = "Draft",
        CreatedDate = DateTime.UtcNow,
        MeetingDate = new DateOnly(2026, 9, 15),
        ExecutiveSummary = "First line of the summary.\nSecond line, after a newline.",
        FinanceCommentary = "Approved budget: £25,000,000. Current forecast: £26,500,000.",
        RiskCommentary = "3 open risks, highest score 16/25.",
        Recommendations = "Members are asked to note the report.",
    };

    private static IReadOnlyList<ValidationErrorInfo> Validate(OpenXmlPackage package) =>
        new OpenXmlValidator(FileFormatVersions.Office2019).Validate(package).ToList();

    private static string Describe(IEnumerable<ValidationErrorInfo> errors) =>
        string.Join("; ", errors.Select(e => $"{e.Description} at {e.Path?.XPath}"));

    [Fact]
    public async Task Docx_export_is_a_schema_valid_word_document()
    {
        var bytes = await Exporter.ExportAsync(SampleReport(), ReportExportFormat.Docx, CancellationToken.None);

        bytes.Should().NotBeEmpty();

        using var stream = new MemoryStream(bytes);
        using var document = WordprocessingDocument.Open(stream, false);

        var errors = Validate(document);
        errors.Should().BeEmpty(Describe(errors));
    }

    [Fact]
    public async Task Docx_export_contains_the_report_content()
    {
        var report = SampleReport();
        var bytes = await Exporter.ExportAsync(report, ReportExportFormat.Docx, CancellationToken.None);

        using var stream = new MemoryStream(bytes);
        using var document = WordprocessingDocument.Open(stream, false);
        var text = document.MainDocumentPart!.Document.Body!.InnerText;

        // Valid-but-empty is a real failure mode for generated documents, and one that a schema
        // check alone would pass.
        text.Should().Contain(report.Title);
        text.Should().Contain("Stirling Council");
        text.Should().Contain("Executive Summary");
        text.Should().Contain("First line of the summary.");
        text.Should().Contain("Second line, after a newline.");
        text.Should().Contain(report.Recommendations!);

        // Sections with no content are skipped rather than emitted as empty headings.
        text.Should().NotContain("Sustainability");
    }

    [Fact]
    public async Task Pptx_export_is_a_schema_valid_presentation()
    {
        var bytes = await Exporter.ExportAsync(SampleReport(), ReportExportFormat.Pptx, CancellationToken.None);

        bytes.Should().NotBeEmpty();

        using var stream = new MemoryStream(bytes);
        using var presentation = PresentationDocument.Open(stream, false);

        var errors = Validate(presentation);
        errors.Should().BeEmpty(Describe(errors));
    }

    [Fact]
    public async Task Pptx_export_has_a_title_slide_plus_one_slide_per_populated_section()
    {
        var report = SampleReport();
        var bytes = await Exporter.ExportAsync(report, ReportExportFormat.Pptx, CancellationToken.None);

        using var stream = new MemoryStream(bytes);
        using var presentation = PresentationDocument.Open(stream, false);
        var presentationPart = presentation.PresentationPart!;

        // Title slide, plus the four sections this report populates.
        presentationPart.SlideParts.Should().HaveCount(5);

        var slideText = presentationPart.SlideParts.Select(p => p.Slide.InnerText).ToList();
        slideText.Should().Contain(t => t.Contains(report.Title));
        slideText.Should().Contain(t => t.Contains("Executive Summary"));
        slideText.Should().Contain(t => t.Contains("Members are asked to note the report."));

        // A slide id list that does not match the slide parts produces a file PowerPoint opens
        // with some slides missing — structurally valid, visibly wrong.
        presentationPart.Presentation.SlideIdList!.Elements<P.SlideId>().Should().HaveCount(5);
    }

    [Fact]
    public async Task Pptx_export_carries_the_structural_parts_a_presentation_cannot_open_without()
    {
        var bytes = await Exporter.ExportAsync(SampleReport(), ReportExportFormat.Pptx, CancellationToken.None);

        using var stream = new MemoryStream(bytes);
        using var presentation = PresentationDocument.Open(stream, false);
        var presentationPart = presentation.PresentationPart!;

        // Master, layout and theme are format requirements, not styling choices — a presentation
        // without them fails to open even when every slide is blank.
        var masterPart = presentationPart.SlideMasterParts.Should().ContainSingle().Subject;
        masterPart.SlideLayoutParts.Should().ContainSingle();
        masterPart.ThemePart.Should().NotBeNull();

        presentationPart.Presentation.SlideSize!.Cx!.Value.Should().Be(12192000, "16:9 widescreen");
    }

    [Fact]
    public async Task Every_export_format_produces_output()
    {
        var report = SampleReport();

        // Guards the switch in ExportAsync: a format added to the enum without a case there
        // throws NotSupportedException, and nothing else would notice until someone clicked it.
        foreach (var format in Enum.GetValues<ReportExportFormat>())
        {
            var bytes = await Exporter.ExportAsync(report, format, CancellationToken.None);
            bytes.Should().NotBeEmpty($"{format} export should produce bytes");
        }
    }
}
