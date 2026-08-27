using SCPM.Domain.Enums;

namespace SCPM.Domain.Common;

/// <summary>One narrative section of a report: a stable key, a heading, and a position.</summary>
/// <param name="Key">
/// Stable identifier, never shown to a reader. Headings get reworded — "Cost/Budget Position"
/// today, "Financial Position" after someone's review — and content keyed by its heading would be
/// orphaned by that edit. The key is what content is stored against; the heading is presentation.
/// </param>
public record ReportSection(string Key, string Heading);

/// <summary>
/// Which sections each kind of report has, and what they are called.
///
/// This exists because report formats are a business convention, not a schema. A status report and
/// a committee paper share nothing but a project reference, and the council's own template for
/// each will be reworded by someone eventually. Holding the sections as data means a new report
/// type, or a renamed heading, is an edit here rather than a database migration and a deployment.
///
/// The previous design had a fixed column per section on the report row — ExecutiveSummary,
/// FinanceCommentary, and eight more. That works for exactly one report format. A status report
/// needs six different sections, and adding a column per section per format is how a schema ends
/// up with forty nullable text fields of which any given row uses six.
/// </summary>
public static class ReportSections
{
    /// <summary>
    /// The status report used by Infrastructure Delivery: Programme Governance.
    ///
    /// Headings are taken verbatim from the council's PD.01.25 template, including the slash in
    /// "Schedule/Programme Update" and the lower-case "reporting period". A heading that is nearly
    /// the council's wording is worse than one that matches it, because the difference is the sort
    /// of thing a reader notices and nobody can explain.
    /// </summary>
    public static readonly IReadOnlyList<ReportSection> StatusReport =
    [
        new("key-activities", "Key Activities in previous reporting period"),
        new("planned-activities", "Planned Activities"),
        new("issues", "Issues"),
        new("risks", "Risks"),
        new("schedule-update", "Schedule/Programme Update"),
        new("cost-position", "Cost/Budget Position"),
    ];

    /// <summary>
    /// The longer form used for committee, cabinet, board and capital-programme papers, and for
    /// decision papers. These are the sections the platform has always had, now held as data
    /// rather than as columns.
    /// </summary>
    public static readonly IReadOnlyList<ReportSection> CommitteePaper =
    [
        new("executive-summary", "Executive Summary"),
        new("background", "Background"),
        new("current-position", "Current Position"),
        new("finance-commentary", "Finance"),
        new("programme-commentary", "Programme"),
        new("risk-commentary", "Risk"),
        new("stakeholder-commentary", "Stakeholders"),
        new("sustainability-commentary", "Sustainability"),
        new("equality-commentary", "Equality Impact"),
        new("recommendations", "Recommendations"),
    ];

    public static IReadOnlyList<ReportSection> For(CommitteeReportType type) => type switch
    {
        CommitteeReportType.StatusReport => StatusReport,
        _ => CommitteePaper,
    };

    /// <summary>
    /// Looks up a section by key within a report type, or null when the type has no such section.
    ///
    /// Returns null rather than throwing: content can outlive the definition that created it — a
    /// section removed from a template leaves rows behind, and a report written last year should
    /// still open rather than blow up because its template has since been revised.
    /// </summary>
    public static ReportSection? Find(CommitteeReportType type, string key) =>
        For(type).FirstOrDefault(s => s.Key == key);
}
