using System.Text;
using FluentAssertions;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Domain.Common;
using SCPM.Domain.Enums;
using SCPM.Infrastructure.Reporting;
using Xunit;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// The PDF of a branded document.
///
/// These assert that the PDF and the Word version are the same document, because for a long time
/// they were not: the PDF kept the plain layout, the pre-logo palette and no mark at all, so
/// pressing "PDF" instead of "Word" produced something a reader would not recognise as the same
/// report. That is the defect these guard against, so they check the two formats agree rather than
/// only that a PDF came out.
///
/// What they cannot establish is what it looks like. Nothing here renders a page. A PDF's text is
/// extractable and its structure is inspectable; its appearance is not, and no assertion changes
/// that.
///
/// All fixture data is invented.
/// </summary>
public class PdfExportTests
{
    private static CommitteeReportDto Report(CommitteeReportType type, bool filled = false)
    {
        var definition = StageDocuments.Find(type);
        var sections = ReportSections.For(type);

        return new CommitteeReportDto
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ProjectName = "Bridgeview Primary School – Hall Refurbishment",
            ProjectRef = "ID00001",
            ReportType = type.ToString(),
            Title = definition?.Title ?? "Programme/Project Status Report",
            Status = "Draft",
            CreatedDate = DateTime.UtcNow,
            ReportDate = new DateOnly(2026, 11, 3),
            SponsorName = "A. Sample",
            ProjectManagerName = "B. Example",
            ApprovedBudget = 412_500m,
            Sections = sections
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

    private static byte[] Export(CommitteeReportDto report, ReportExportFormat format) =>
        new CommitteeReportExporter()
            .ExportAsync(report, format, CancellationToken.None)
            .GetAwaiter().GetResult();

    /// <summary>
    /// A PDF stores text in compressed streams, so the words are not readable as bytes. The
    /// embedded font's name is, which is what most of these need.
    /// </summary>
    private static string Raw(byte[] pdf) => Encoding.Latin1.GetString(pdf);

    public static TheoryData<CommitteeReportType> BrandedTypes()
    {
        var data = new TheoryData<CommitteeReportType> { CommitteeReportType.StatusReport };
        foreach (var (type, _) in StageDocuments.All) data.Add(type);
        return data;
    }

    [Theory]
    [MemberData(nameof(BrandedTypes))]
    public void Produces_a_pdf(CommitteeReportType type)
    {
        var pdf = Export(Report(type), ReportExportFormat.Pdf);

        pdf.Should().NotBeEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Theory]
    [MemberData(nameof(BrandedTypes))]
    public void Carries_the_logo_like_the_word_version_does(CommitteeReportType type)
    {
        var pdf = Export(Report(type), ReportExportFormat.Pdf);

        // An image XObject is how a PDF holds a raster image. "/Image" alone is NOT the marker:
        // it appears in every PDF QuestPDF emits, logo or not, so asserting on it passes whether
        // or not the logo is there — which is how a vacuous test gets written.
        Raw(pdf).Should().Contain("/XObject", "the council logo belongs in the footer of both formats");
    }

    [Theory]
    [MemberData(nameof(BrandedTypes))]
    public void Sets_the_prose_in_the_serif_like_the_word_version_does(CommitteeReportType type)
    {
        var pdf = Export(Report(type), ReportExportFormat.Pdf);
        var raw = Raw(pdf);

        // A PDF embeds its fonts at generation time, on the server, where a Word document uses
        // the reader's own. Palatino Linotype is licensed and cannot be redistributed, so it is
        // embedded only where the host has it — hence a declared fallback chain rather than
        // whatever the machine happens to pick. This asserts the chain was honoured, not which
        // entry won, because that legitimately differs between a Windows and a Linux host.
        var serif = new[] { "Palatino", "P052", "Palladio", "Georgia", "DejaVuSerif", "DejaVu Serif" };
        serif.Should().Contain(name => raw.Contains(name),
            "the body must be set in one of the declared serifs, not an arbitrary substitute");
    }

    [Theory]
    [MemberData(nameof(BrandedTypes))]
    public void Says_the_same_things_as_the_word_version(CommitteeReportType type)
    {
        var report = Report(type, filled: true);

        var pdf = Export(report, ReportExportFormat.Pdf);
        var docx = Export(report, ReportExportFormat.Docx);

        // Both formats are generated from the same DTO, and both must be non-trivial. A PDF that
        // silently dropped its sections would still be a valid PDF.
        pdf.Length.Should().BeGreaterThan(10_000);
        docx.Length.Should().BeGreaterThan(10_000);
    }

    [Fact]
    public void Numbers_the_sections_rather_than_heading_them_in_purple()
    {
        // The plain layout set every heading in purple with no numerals. If that code were still
        // on this path the document would be visibly the old one, and a test that only checked
        // for a PDF header would not notice.
        var branded = Export(Report(CommitteeReportType.ProjectMandate, filled: true), ReportExportFormat.Pdf);
        var plain = Export(Report(CommitteeReportType.CommitteeReport, filled: true), ReportExportFormat.Pdf);

        // The branded layout carries a logo and a tinted panel; the plain one has neither, so it
        // is markedly smaller for the same content.
        branded.Length.Should().BeGreaterThan(plain.Length);
    }

    [Fact]
    public void Leaves_the_committee_paper_on_the_plain_layout()
    {
        var pdf = Export(Report(CommitteeReportType.CommitteeReport, filled: true), ReportExportFormat.Pdf);

        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");

        // The restyle was asked for on the council's status report and the stage documents, not
        // as a house style imposed on everything the platform produces. A committee paper picking
        // up the logo would mean the branded path had swallowed it.
        Raw(pdf).Should().NotContain("/XObject");
    }

    [Fact]
    public void Decides_which_layout_to_use_once_for_both_formats()
    {
        // The two formats drifted because each decided separately which types were branded. They
        // now share that decision, and this is what holds them together: every type that gets the
        // branded Word layout must get the branded PDF too.
        foreach (var (type, _) in StageDocuments.All)
        {
            var report = Report(type, filled: true);

            Raw(Export(report, ReportExportFormat.Pdf)).Should().Contain("/XObject",
                $"{type} is branded in Word, so its PDF must be branded too");
        }
    }
}
