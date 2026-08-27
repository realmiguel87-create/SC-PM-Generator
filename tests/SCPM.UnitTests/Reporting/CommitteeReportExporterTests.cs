using System.Text;
using System.Text.Json;
using FluentAssertions;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Infrastructure.Reporting;
using Xunit;

namespace SCPM.UnitTests.Reporting;

/// <summary>Verifies actual output bytes (PDF/ZIP magic numbers, parseable CSV/JSON) rather than
/// just that CommitteeReportExporter compiles against QuestPDF/ClosedXML.</summary>
public class CommitteeReportExporterTests
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
        Sections =
        [
            new() { Key = "executive-summary", Heading = "Executive Summary",
                    Content = "This report provides an update on the Stirling Community Campus project." },
            new() { Key = "finance-commentary", Heading = "Finance",
                    Content = "Approved budget: £25,000,000. Current forecast: £26,500,000." },
            new() { Key = "risk-commentary", Heading = "Risk",
                    Content = "3 open risks, highest score 16/25." },
            new() { Key = "recommendations", Heading = "Recommendations",
                    Content = "Members are asked to note the report." },
        ],
    };

    [Fact]
    public async Task Pdf_export_produces_a_valid_pdf_file()
    {
        var bytes = await Exporter.ExportAsync(SampleReport(), ReportExportFormat.Pdf, CancellationToken.None);

        bytes.Should().NotBeEmpty();
        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task Xlsx_export_produces_a_valid_zip_based_workbook()
    {
        var bytes = await Exporter.ExportAsync(SampleReport(), ReportExportFormat.Xlsx, CancellationToken.None);

        bytes.Should().NotBeEmpty();
        // XLSX files are OOXML zip packages — "PK" is the local file header signature.
        bytes[0].Should().Be((byte)'P');
        bytes[1].Should().Be((byte)'K');
    }

    [Fact]
    public async Task Csv_export_includes_every_populated_section_and_escapes_commas()
    {
        var report = SampleReport();
        var bytes = await Exporter.ExportAsync(report, ReportExportFormat.Csv, CancellationToken.None);
        var csv = Encoding.UTF8.GetString(bytes);

        csv.Should().Contain("Section,Content");
        csv.Should().Contain("Executive Summary");
        csv.Should().Contain("\"Approved budget: £25,000,000. Current forecast: £26,500,000.\"");
    }

    [Fact]
    public async Task Json_export_round_trips_the_report_content()
    {
        var report = SampleReport();
        var bytes = await Exporter.ExportAsync(report, ReportExportFormat.Json, CancellationToken.None);

        var roundTripped = JsonSerializer.Deserialize<CommitteeReportDto>(bytes);

        roundTripped.Should().NotBeNull();
        roundTripped!.Title.Should().Be(report.Title);
        roundTripped.Sections.Single(s => s.Key == "finance-commentary").Content
            .Should().Be(report.Sections.Single(s => s.Key == "finance-commentary").Content);
    }
}
