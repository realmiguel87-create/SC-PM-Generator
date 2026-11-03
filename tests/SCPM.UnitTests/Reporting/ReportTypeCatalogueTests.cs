using FluentAssertions;
using SCPM.Domain.Common;
using SCPM.Domain.Enums;
using Xunit;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// The report-type catalogue, and the three places that have to agree about it: the enum, the
/// document definitions, and the TypeScript union the web app offers.
///
/// The section list stopped being duplicated in the client for exactly this reason — a list kept
/// in step by hand goes stale, and then the UI cannot create a document the server has supported
/// for months, which is precisely what happened to the whole Stage 0–1 pack between its being
/// built and being reachable. The type list is the same trap in smaller form, and this is the
/// guard rather than the fix: it cannot stop the drift, but it fails the build when it happens.
/// </summary>
public class ReportTypeCatalogueTests
{
    /// <summary>
    /// The web app's type union, found by walking up from the test assembly.
    ///
    /// Returns null when the file is not there — a published test assembly has no repository
    /// around it — so the test skips rather than fails. A test that fails because of where it was
    /// run teaches nothing.
    /// </summary>
    private static string? WebTypesSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName, "src", "Web", "src", "features", "reporting", "types.ts");

            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            directory = directory.Parent;
        }

        return null;
    }

    [Fact]
    public void Every_report_type_the_server_supports_is_offered_by_the_web_app()
    {
        var source = WebTypesSource();
        if (source is null) return;

        var missing = Enum.GetNames<CommitteeReportType>()
            .Where(name => !source.Contains($"\"{name}\""))
            .ToList();

        missing.Should().BeEmpty(
            "the web app cannot ask for a report type it does not list, however well the server "
            + "produces it");
    }

    [Fact]
    public void The_web_app_offers_no_report_type_the_server_does_not_support()
    {
        var source = WebTypesSource();
        if (source is null) return;

        var union = source[source.IndexOf("export type CommitteeReportType", StringComparison.Ordinal)..];
        union = union[..union.IndexOf(';', StringComparison.Ordinal)];

        var offered = System.Text.RegularExpressions.Regex
            .Matches(union, "\"([A-Za-z]+)\"")
            .Select(m => m.Groups[1].Value);

        var names = Enum.GetNames<CommitteeReportType>();

        // The other direction, which is the one that produces a 400 rather than an absence: a
        // type offered in a dropdown and rejected by the server when chosen.
        offered.Should().OnlyContain(o => names.Contains(o));
    }

    [Fact]
    public void Every_riba_stage_document_has_a_definition_and_every_definition_a_type()
    {
        // The three report types that predate the pack and have no stage document: the status
        // report and the committee-paper family.
        var narrativeOnly = new[]
        {
            CommitteeReportType.StatusReport,
            CommitteeReportType.CommitteeReport,
            CommitteeReportType.CabinetReport,
            CommitteeReportType.BoardReport,
            CommitteeReportType.CapitalProgrammeReport,
            CommitteeReportType.DecisionPaper,
        };

        var withDefinitions = StageDocuments.All.Select(d => d.Type).ToList();

        // No type is left without a home: it either has a document definition or is one of the
        // six that deliberately has none.
        Enum.GetValues<CommitteeReportType>()
            .Should().OnlyContain(t => withDefinitions.Contains(t) || narrativeOnly.Contains(t));

        // And nothing appears twice, which a copy-paste into the catalogue would do silently —
        // the document would still render, from whichever entry Find happened to reach first.
        withDefinitions.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Every_document_has_a_title_a_stage_and_guidance_on_every_section()
    {
        foreach (var (type, definition) in StageDocuments.All)
        {
            definition.Title.Should().NotBeNullOrWhiteSpace($"{type} needs a title");
            definition.Eyebrow.Should().NotBeNullOrWhiteSpace($"{type} needs a service line");
            definition.Sections.Should().NotBeEmpty($"{type} needs sections");

            // The gate report alone has no fixed stage: it is used at every boundary.
            if (type != CommitteeReportType.StageGateApproval)
            {
                definition.RibaStage.Should().NotBeNullOrWhiteSpace($"{type} belongs to one stage");
            }

            // Guidance is what separates a template from a list of headings, and it is the part
            // most easily forgotten when a document is added in a hurry.
            definition.Sections.Should().OnlyContain(
                s => !string.IsNullOrWhiteSpace(s.Prompt),
                $"every section of {type} needs guidance");

            definition.Sections.Select(s => s.Key).Should().OnlyHaveUniqueItems(
                $"{type} stores content against section keys, so a duplicate would overwrite");
        }
    }
}
