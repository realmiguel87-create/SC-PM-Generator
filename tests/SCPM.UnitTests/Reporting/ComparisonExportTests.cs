using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using FluentAssertions;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Application.Reporting.Export;
using SCPM.Infrastructure.Reporting;
using Xunit;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// Exporting a snapshot comparison: the document that gets built, and the six files it renders to.
///
/// The building and the rendering are tested separately on purpose. What each column says and how
/// a null reads are decisions, and they are checked against the model rather than by generating a
/// PDF and hoping. The renderers get the same treatment DOCX and PPTX got in Phase 17 — re-opened
/// and schema-validated, because a file that unzips is not the same thing as a file Word opens.
/// </summary>
public class ComparisonExportTests
{
    private static readonly TabularDocumentExporter Exporter = new();

    private static SnapshotComparisonDto Summary() => new()
    {
        FromSnapshotId = Guid.NewGuid(),
        FromLabel = "January",
        FromCapturedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc),
        ToSnapshotId = Guid.NewGuid(),
        ToLabel = "February",
        ToCapturedAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc),
        FromRibaStage = 3,
        ToRibaStage = 4,
        FromApprovedBudget = 25_000_000m,
        ToApprovedBudget = 25_000_000m,
        FromForecastCost = 26_500_000m,
        ToForecastCost = 27_250_000m,
        FromOpenRiskCount = 12,
        ToOpenRiskCount = 14,
        FromCompensationEventValue = 120_000m,
        ToCompensationEventValue = 310_000m,
    };

    private static SnapshotItemComparisonDto Items() => new()
    {
        RiskChanges =
        [
            new RiskChangeDto
            {
                RiskId = Guid.NewGuid(),
                Title = "Ground conditions",
                ChangeType = ItemChangeType.Modified,
                FromStatus = "Open",
                ToStatus = "Escalated",
                FromScore = 6,
                ToScore = 20,
            },
        ],
        ExtensionOfTimeChanges =
        [
            new ExtensionOfTimeChangeDto
            {
                ExtensionOfTimeId = Guid.NewGuid(),
                Reference = "EOT-003",
                Reason = "Exceptional weather",
                ChangeType = ItemChangeType.Modified,
                FromStatus = "Claimed",
                ToStatus = "Awarded",
                FromDaysClaimed = 45,
                ToDaysClaimed = 45,
                FromDaysAwarded = null,
                ToDaysAwarded = 21,
            },
        ],
    };

    private static SnapshotIntervalActivityDto Interval() => new()
    {
        Items =
        [
            new IntervalActivityItemDto
            {
                Register = "Risk",
                ItemId = Guid.NewGuid(),
                Name = "Asbestos found in survey",
                ActivityType = IntervalActivityType.RaisedAndRemoved,
                VersionCount = 2,
            },
        ],
    };

    /// <summary>
    /// Reports validation errors as plain text. FluentAssertions' collection formatter throws
    /// while rendering ValidationErrorInfo, which turns a legible schema complaint into an
    /// unrelated ArgumentOutOfRangeException and hides the actual problem.
    /// </summary>
    private static void AssertValid(IEnumerable<ValidationErrorInfo> errors)
    {
        var described = errors.Select(e => $"{e.Description} at {e.Path?.XPath}").ToList();
        if (described.Count > 0) Assert.Fail(string.Join("\n", described));
    }

    private static ExportDocument Document() =>
        ComparisonExportBuilder.Build(Summary(), Items(), Interval());

    [Fact]
    public void Document_includes_every_section_even_when_empty()
    {
        var document = ComparisonExportBuilder.Build(
            Summary(), new SnapshotItemComparisonDto(), new SnapshotIntervalActivityDto());

        // An omitted section reads as an oversight; "No milestone changed between these two
        // points" is an answer. A reader looking for movement has to be able to tell the two
        // apart, which they cannot if the section simply is not there.
        document.Tables.Should().HaveCount(8);
        document.Tables.Where(t => t.Rows.Count == 0)
            .Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t.EmptyMessage));
    }

    [Fact]
    public void Document_states_the_delta_convention_once_at_the_top()
    {
        var document = Document();

        // Every delta in the document is To minus From, including where up is bad. A reader who
        // assumes positive means improvement misreads the risk and compensation-event rows, so
        // the convention is stated rather than left to be inferred.
        document.MetaLines.Should().Contain(line =>
            line.Contains("To minus From") && line.Contains("not the same as improved"));
    }

    [Fact]
    public void Undetermined_extension_days_read_as_undetermined_rather_than_zero()
    {
        var document = Document();

        var eot = document.Tables.Single(t => t.Heading.StartsWith("Extension of time"));
        var row = eot.Rows.Single();

        // A claim with no decision yet is not a decision of zero days, and in a contractual
        // document that difference is the whole point.
        row.Should().Contain(cell => cell.Contains("Undetermined"));
    }

    [Fact]
    public void A_movement_of_zero_reads_differently_from_no_movement_at_all()
    {
        var document = Document();
        var summary = document.Tables.First();

        var budget = summary.Rows.Single(r => r[0] == "Approved budget");
        budget[^1].Should().Be("No change", "the budget held steady");

        var forecast = summary.Rows.Single(r => r[0] == "Forecast cost");
        forecast[^1].Should().StartWith("+", "the forecast rose");
    }

    [Fact]
    public async Task Every_format_produces_output()
    {
        var document = Document();

        foreach (var format in Enum.GetValues<ReportExportFormat>())
        {
            var bytes = await Exporter.ExportAsync(document, format, CancellationToken.None);
            bytes.Should().NotBeEmpty($"{format} export should produce bytes");
        }
    }

    [Fact]
    public async Task Docx_export_is_schema_valid_and_contains_the_tables()
    {
        var bytes = await Exporter.ExportAsync(Document(), ReportExportFormat.Docx, CancellationToken.None);

        using var stream = new MemoryStream(bytes);
        using var wordDocument = WordprocessingDocument.Open(stream, false);

        AssertValid(new OpenXmlValidator(FileFormatVersions.Office2019).Validate(wordDocument));

        var text = wordDocument.MainDocumentPart!.Document.Body!.InnerText;
        text.Should().Contain("Snapshot Comparison");
        text.Should().Contain("Headline movements");
        text.Should().Contain("Ground conditions");
        text.Should().Contain("Asbestos found in survey");
    }

    [Fact]
    public async Task Pptx_export_is_schema_valid_with_a_slide_per_section()
    {
        var document = Document();
        var bytes = await Exporter.ExportAsync(document, ReportExportFormat.Pptx, CancellationToken.None);

        using var stream = new MemoryStream(bytes);
        using var presentation = PresentationDocument.Open(stream, false);

        AssertValid(new OpenXmlValidator(FileFormatVersions.Office2019).Validate(presentation));

        // Title slide plus one per table.
        presentation.PresentationPart!.SlideParts.Should().HaveCount(document.Tables.Count + 1);
    }

    [Fact]
    public async Task Xlsx_export_puts_each_table_on_its_own_sheet()
    {
        var document = Document();
        var bytes = await Exporter.ExportAsync(document, ReportExportFormat.Xlsx, CancellationToken.None);

        using var stream = new MemoryStream(bytes);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);

        // One table per sheet, because a spreadsheet is opened to be sorted and filtered, and
        // that only works with a single header row per sheet.
        workbook.Worksheets.Count.Should().Be(document.Tables.Count);

        // Sheet names are capped at 31 characters by Excel and must be unique, or the file opens
        // as corrupt rather than with an oddly-named tab.
        workbook.Worksheets.Select(w => w.Name).Should().OnlyHaveUniqueItems();
        workbook.Worksheets.Should().OnlyContain(w => w.Name.Length <= 31);
    }

    [Fact]
    public async Task Csv_export_escapes_the_commas_in_currency_values()
    {
        var bytes = await Exporter.ExportAsync(Document(), ReportExportFormat.Csv, CancellationToken.None);
        var csv = Encoding.UTF8.GetString(bytes);

        // Currency is formatted with thousands separators, so unescaped it would split one cell
        // into three and shift every later column — the classic silent CSV corruption.
        csv.Should().Contain("\"£25,000,000\"");
        csv.Should().Contain("Headline movements");
    }

    [Fact]
    public async Task Json_export_keeps_the_structure_rather_than_flattening_it()
    {
        var bytes = await Exporter.ExportAsync(Document(), ReportExportFormat.Json, CancellationToken.None);

        using var parsed = JsonDocument.Parse(bytes);
        var tables = parsed.RootElement.GetProperty("Tables");

        tables.GetArrayLength().Should().Be(8);
        tables[0].GetProperty("Columns").GetArrayLength().Should().Be(4);
    }
}
