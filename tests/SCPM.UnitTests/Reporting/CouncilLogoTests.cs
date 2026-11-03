using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using FluentAssertions;
using SCPM.Application.Reporting.Dtos;
using SCPM.Domain.Common;
using SCPM.Infrastructure.Reporting;
using Xunit;
using D = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// The council logo in the document footer.
///
/// No logo file has been supplied — it has only ever arrived as a pasted image — so
/// <see cref="CouncilLogo.Png"/> is null and every document currently renders a text-only footer.
/// These tests exist so that is the only thing missing: the Open XML image path is exercised with
/// a generated PNG, so embedding the real file is a drop-in rather than the start of debugging.
///
/// The fixture is a 120×40 PNG built in code. A 3:1 aspect ratio is the point of those numbers —
/// it is what makes a stretched image detectable, which a square test logo would hide.
/// </summary>
public class CouncilLogoTests
{
    /// <summary>120×40 RGBA PNG, generated rather than committed as a binary fixture.</summary>
    private const string TestPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAHgAAAAoCAYAAAA16j4lAAAAWUlEQVR42u3RQREAAAQAQTkVEEdkcjD7"
        + "uAK3Udmjv4UJgAVYgAVYgAVYgAELsAALsAALsAADFmABFmABFmABFmDAAizAAizAAizAgAVYgAVYgAVY"
        + "gAELsG62k+9LxszaUAQAAAAASUVORK5CYII=";

    private static byte[] TestPng() => Convert.FromBase64String(TestPngBase64);

    private const long ExpectedHeightEmu = 11 * 36000L;

    private static CommitteeReportDto Mandate()
    {
        var definition = StageDocuments.ProjectMandate;

        return new CommitteeReportDto
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ProjectName = "Bridgeview Primary School – Hall Refurbishment",
            ProjectRef = "ID00001",
            ReportType = nameof(SCPM.Domain.Enums.CommitteeReportType.ProjectMandate),
            Title = definition.Title,
            Status = "Draft",
            CreatedDate = DateTime.UtcNow,
            ApprovedBudget = 412_500m,
            Sections = definition.Sections
                .Select(s => new ReportSectionDto { Key = s.Key, Heading = s.Heading, Prompt = s.Prompt })
                .ToList(),
        };
    }

    private static byte[] Export(byte[]? logo) =>
        OpenXmlReportBuilder.BuildStageDocumentDocx(
            Mandate(), StageDocuments.ProjectMandate, Mandate().Sections, "#3E2880", "#006D5B", logo);

    [Fact]
    public void Reads_the_pixel_dimensions_from_the_png_header()
    {
        // Read rather than assumed, because the drawing is sized in EMU and picking a width
        // without the aspect ratio is how a logo comes out stretched.
        CouncilLogo.Dimensions(TestPng()).Should().Be((120, 40));
    }

    [Theory]
    [InlineData("not a png at all, just text")]
    [InlineData("")]
    public void Treats_anything_that_is_not_a_png_as_having_no_dimensions(string content)
    {
        // A footer that threw on a malformed file would take the whole export down with it. The
        // document should lose its logo, not fail to generate.
        CouncilLogo.Dimensions(System.Text.Encoding.UTF8.GetBytes(content)).Should().BeNull();
    }

    [Fact]
    public void Treats_a_truncated_png_as_having_no_dimensions()
    {
        CouncilLogo.Dimensions(TestPng()[..20]).Should().BeNull();
    }

    [Fact]
    public void Treats_a_zero_sized_png_as_having_no_dimensions()
    {
        var png = TestPng();

        // Width to zero, in the IHDR. Scaling by this would divide by zero.
        png[16] = 0; png[17] = 0; png[18] = 0; png[19] = 0;

        CouncilLogo.Dimensions(png).Should().BeNull();
    }

    [Fact]
    public void Ships_the_council_logo_as_an_embedded_resource()
    {
        var logo = CouncilLogo.Png;

        // The resource name is a string, and a rename or a dropped EmbeddedResource entry would
        // silently return null — producing documents with no logo and no error anywhere.
        logo.Should().NotBeNull("the logo is embedded in SCPM.Infrastructure");

        var dimensions = CouncilLogo.Dimensions(logo!);
        dimensions.Should().NotBeNull("the embedded resource must be a readable PNG");

        var (width, height) = dimensions!.Value;

        // The mark is a roundel, so it should be close to square. A wildly different ratio means
        // the uncropped original — which carries ~60% empty background — has been embedded by
        // mistake, and would print as a tiny logo adrift in white space.
        ((double)width / height).Should().BeInRange(0.8, 1.25);

        // Enough resolution to print at 11mm without visible pixellation: 11mm at 300dpi is 130px.
        height.Should().BeGreaterThan(200);
    }

    [Fact]
    public async Task Puts_the_real_logo_into_a_document_exported_through_the_exporter()
    {
        // The path an actual document takes. The tests above drive the builder directly with a
        // generated PNG, which would still pass if the exporter never passed the logo along.
        var bytes = await new CommitteeReportExporter()
            .ExportAsync(Mandate(), SCPM.Application.Common.Interfaces.ReportExportFormat.Docx,
                CancellationToken.None);

        using var stream = new MemoryStream(bytes);
        using var document = WordprocessingDocument.Open(stream, false);

        var footerPart = document.MainDocumentPart!.FooterParts.Single();

        footerPart.ImageParts.Should().ContainSingle();
        footerPart.Footer.Descendants<W.Drawing>().Should().ContainSingle();
    }

    [Fact]
    public void Renders_a_text_only_footer_when_no_logo_is_supplied()
    {
        using var stream = new MemoryStream(Export(logo: null));
        using var document = WordprocessingDocument.Open(stream, false);

        var footerPart = document.MainDocumentPart!.FooterParts.Single();

        // Absent is still a supported state — the builder takes the logo from its caller rather
        // than reaching for the embedded one, so this is genuinely "no logo" and not a fallback.
        footerPart.ImageParts.Should().BeEmpty();
        footerPart.Footer.Descendants<W.Drawing>().Should().BeEmpty();
        footerPart.Footer.InnerText.Should().Contain("[to be assigned]");
    }

    [Fact]
    public void Embeds_the_logo_in_the_footer_when_one_is_supplied()
    {
        using var stream = new MemoryStream(Export(TestPng()));
        using var document = WordprocessingDocument.Open(stream, false);

        var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(document).ToList();
        if (errors.Count > 0)
        {
            Assert.Fail(string.Join("\n", errors.Select(e => $"{e.Description} at {e.Path?.XPath}")));
        }

        var footerPart = document.MainDocumentPart!.FooterParts.Single();

        var imagePart = footerPart.ImageParts.Should().ContainSingle().Subject;
        var drawing = footerPart.Footer.Descendants<W.Drawing>().Should().ContainSingle().Subject;

        // The relationship has to resolve to the part that actually holds the bytes. A drawing
        // pointing at an id that is not in the footer's relationships produces a file Word opens
        // with an empty box where the image should be — and nothing else detects that.
        drawing.Descendants<D.Blip>().Single().Embed!.Value
            .Should().Be(footerPart.GetIdOfPart(imagePart));

        // The document-control text keeps its place beside the logo rather than being displaced
        // by it.
        footerPart.Footer.InnerText.Should().Contain("Project Mandate");
        footerPart.Footer.InnerText.Should().Contain("[to be assigned]");
    }

    [Fact]
    public void Scales_the_logo_from_its_real_aspect_ratio()
    {
        using var stream = new MemoryStream(Export(TestPng()));
        using var document = WordprocessingDocument.Open(stream, false);

        var extent = document.MainDocumentPart!.FooterParts.Single()
            .Footer.Descendants<DW.Extent>().Single();

        // 11mm tall, and three times that wide for a 3:1 image. A hardcoded width would pass a
        // test that only checked the drawing existed, and would print the logo squashed.
        extent.Cy!.Value.Should().Be(ExpectedHeightEmu);
        extent.Cx!.Value.Should().Be(ExpectedHeightEmu * 3);
    }

    [Fact]
    public void Places_the_logo_at_full_strength_rather_than_as_a_faded_watermark()
    {
        using var stream = new MemoryStream(Export(TestPng()));
        using var document = WordprocessingDocument.Open(stream, false);

        var footer = document.MainDocumentPart!.FooterParts.Single().Footer;

        // The council's logo is a roundel with text around its rim. At 11mm that is already at
        // the limit of legibility, and an alpha wash on top of it reads as a smudge rather than
        // as a mark. So: no alphaModFix, and inline rather than floating behind the text.
        footer.Descendants<D.AlphaModulationFixed>().Should().BeEmpty();
        footer.Descendants<DW.Anchor>().Should().BeEmpty();
        footer.Descendants<DW.Inline>().Should().ContainSingle();
    }
}
