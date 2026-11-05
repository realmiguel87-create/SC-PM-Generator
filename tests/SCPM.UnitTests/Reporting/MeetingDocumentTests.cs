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
/// Meeting agendas and minutes, restyled from the service's own documents.
///
/// These assert the two things that distinguish a meeting record from every other document in
/// the catalogue — a three-column Ref / Item / Action grid, and an attendance block on minutes
/// only — and the one that keeps the pair honest: an agenda and the minutes of the same meeting
/// come from one item list. In the originals they are separate files whose lists have already
/// begun to diverge.
///
/// All fixture data is invented.
/// </summary>
public class MeetingDocumentTests
{
    private const string Green = "006D5B";

    private static CommitteeReportDto Meeting(CommitteeReportType type)
    {
        var definition = MeetingDocuments.Find(type)!;

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
            ReportDate = new DateOnly(2026, 11, 5),
            ProjectManagerName = "B. Example",
            Sections = [],
        };
    }

    private static byte[] Export(CommitteeReportType type, ReportExportFormat format) =>
        new CommitteeReportExporter()
            .ExportAsync(Meeting(type), format, CancellationToken.None)
            .GetAwaiter().GetResult();

    public static TheoryData<CommitteeReportType> AllMeetings()
    {
        var data = new TheoryData<CommitteeReportType>();
        foreach (var (type, _) in MeetingDocuments.All) data.Add(type);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllMeetings))]
    public void Produces_a_document_word_will_open(CommitteeReportType type)
    {
        using var stream = new MemoryStream(Export(type, ReportExportFormat.Docx));
        using var document = WordprocessingDocument.Open(stream, false);

        var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(document).ToList();
        if (errors.Count > 0)
        {
            Assert.Fail(string.Join("\n", errors.Select(e => $"{e.Description} at {e.Path?.XPath}")));
        }
    }

    [Theory]
    [MemberData(nameof(AllMeetings))]
    public void Sets_out_a_ref_item_and_action_grid(CommitteeReportType type)
    {
        var definition = MeetingDocuments.Find(type)!;

        using var stream = new MemoryStream(Export(type, ReportExportFormat.Docx));
        using var document = WordprocessingDocument.Open(stream, false);
        var text = document.MainDocumentPart!.Document.Body!.InnerText;

        // An action column is what makes a meeting record usable afterwards. A set of minutes
        // without one is a description of a conversation.
        text.Should().Contain("Action");
        text.Should().Contain(definition.IsMinutes ? "Minute" : "Item");

        foreach (var item in definition.Items)
        {
            text.Should().Contain(item.Number);
            text.Should().Contain(item.Title);
        }
    }

    [Fact]
    public void Numbers_minute_entries_beneath_their_item()
    {
        using var stream = new MemoryStream(
            Export(CommitteeReportType.PreStartMeetingMinutes, ReportExportFormat.Docx));
        using var document = WordprocessingDocument.Open(stream, false);
        var text = document.MainDocumentPart!.Document.Body!.InnerText;

        // 2.1, 2.2 under item 2.0 — the service's own convention, and what makes an action
        // referable in the next meeting.
        text.Should().Contain("2.1").And.Contain("2.2").And.Contain("2.6");
    }

    [Fact]
    public void Records_attendance_on_minutes_and_not_on_an_agenda()
    {
        using var minutesStream = new MemoryStream(
            Export(CommitteeReportType.ProgressMeetingMinutes, ReportExportFormat.Docx));
        using var minutes = WordprocessingDocument.Open(minutesStream, false);

        // With the organisation, not only the name. Six months later "WW" means nothing without
        // it, and the service's own minutes contain two people with those initials.
        var minutesText = minutes.MainDocumentPart!.Document.Body!.InnerText;
        minutesText.Should().Contain("Present").And.Contain("Apologies").And.Contain("Organisation");

        using var agendaStream = new MemoryStream(
            Export(CommitteeReportType.ProgressMeetingAgenda, ReportExportFormat.Docx));
        using var agenda = WordprocessingDocument.Open(agendaStream, false);

        // An agenda is written before anyone has attended. Tested on "Organisation", which only
        // the attendance block carries: "Apologies" appears legitimately in item 1.0's own title,
        // so asserting on that word would fail against a correct document.
        agenda.MainDocumentPart!.Document.Body!.InnerText.Should().NotContain("Organisation");
    }

    [Fact]
    public void Draws_an_agenda_and_its_minutes_from_one_item_list()
    {
        // The defect this prevents is in the originals: the Progress agenda numbers thirteen
        // items where the minutes number twelve, so a meeting is minuted against a structure
        // nobody agreed. One list, two renderings.
        foreach (var kind in new[] { "DesignTeamMeeting", "PreStartMeeting", "ProgressMeeting" })
        {
            var agenda = MeetingDocuments.Find(Enum.Parse<CommitteeReportType>(kind + "Agenda"))!;
            var minutes = MeetingDocuments.Find(Enum.Parse<CommitteeReportType>(kind + "Minutes"))!;

            minutes.Items.Should().BeSameAs(agenda.Items, $"{kind} has one item list");
            minutes.Kind.Should().Be(agenda.Kind);
            agenda.IsMinutes.Should().BeFalse();
            minutes.IsMinutes.Should().BeTrue();
        }
    }

    [Theory]
    [MemberData(nameof(AllMeetings))]
    public void Carries_the_house_style(CommitteeReportType type)
    {
        using var stream = new MemoryStream(Export(type, ReportExportFormat.Docx));
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart!.Document.Body!;

        // The purple strip, the logo footer and the serif: a meeting record belongs in the same
        // family as the reports it feeds.
        body.Descendants<W.Table>().First()
            .Descendants<W.Shading>().Single().Fill!.Value.Should().Be("3E2880");

        document.MainDocumentPart.StyleDefinitionsPart!
            .Styles!.Descendants<W.RunFonts>().First()
            .Ascii!.Value.Should().Be("Palatino Linotype");

        document.MainDocumentPart.FooterParts.Single()
            .Footer.Descendants<W.Drawing>().Should().ContainSingle();

        // Item numbers in the brand green, as section numerals are elsewhere.
        body.Descendants<W.Paragraph>()
            .Where(p => p.InnerText == "1.0")
            .Should().OnlyContain(p => p.Descendants<W.Color>().Any(c => c.Val! == Green));
    }

    [Theory]
    [MemberData(nameof(AllMeetings))]
    public void Produces_a_matching_pdf(CommitteeReportType type)
    {
        var pdf = Export(type, ReportExportFormat.Pdf);

        System.Text.Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");

        // Branded, so the logo is embedded — the same check the other branded documents use.
        System.Text.Encoding.Latin1.GetString(pdf).Should().Contain("/XObject");
    }

    [Fact]
    public void Gives_every_item_somewhere_to_write()
    {
        // An item with no points listed — Any Other Business, Date of Next Meeting — still needs
        // a row beneath it, or the document is a list of headings with no room for a meeting.
        var definition = MeetingDocuments.ProgressMeetingMinutes;
        var empty = definition.Items.Where(i => i.Points.Count == 0).ToList();

        empty.Should().NotBeEmpty("some items are discussion rather than a checklist");

        using var stream = new MemoryStream(
            Export(CommitteeReportType.ProgressMeetingMinutes, ReportExportFormat.Docx));
        using var document = WordprocessingDocument.Open(stream, false);

        var rows = document.MainDocumentPart!.Document.Body!
            .Descendants<W.Table>().Last()
            .Elements<W.TableRow>().Count();

        // Header, plus a row per item, plus a row per point, plus one blank row per empty item.
        var expected = 1
            + definition.Items.Count
            + definition.Items.Sum(i => Math.Max(1, i.Points.Count));

        rows.Should().Be(expected);
    }
}
