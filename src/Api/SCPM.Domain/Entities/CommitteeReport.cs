using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

/// <summary>
/// A report about a project: a committee, cabinet or board paper, a decision paper, or the
/// Infrastructure Delivery status report.
///
/// Its narrative lives in <see cref="Sections"/> rather than in a fixed column per heading, because
/// the headings are a business convention that differs per report type and gets reworded. See
/// <see cref="ReportSections"/>.
///
/// Optionally anchored to a Snapshot so the position it reports reflects a specific point in time
/// rather than live data that keeps moving under the report after it is written. Appendices are
/// the report's own attached DocumentFiles, not a section.
/// </summary>
public class CommitteeReport : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public Guid? SnapshotId { get; set; }
    public Snapshot? Snapshot { get; set; }

    public CommitteeReportType ReportType { get; set; }
    public string Title { get; set; } = default!;

    /// <summary>
    /// The date the committee sits, for a paper going to one. Null on a status report, which is
    /// not written for a meeting.
    /// </summary>
    public DateOnly? MeetingDate { get; set; }

    /// <summary>
    /// The date the report describes the position as at — "Report Date" on the council's status
    /// report template.
    ///
    /// Deliberately not <see cref="BaseEntity.CreatedDate"/>, which records when the row was
    /// written. A status report for the period ending 21 October is routinely typed up on the
    /// 23rd, and printing the typing date on it would misdate the position it reports.
    /// </summary>
    public DateOnly? ReportDate { get; set; }

    public CommitteeReportStatus Status { get; set; } = CommitteeReportStatus.Draft;

    /// <summary>
    /// The report's narrative, one row per section, keyed by <see cref="ReportSections"/>.
    ///
    /// This replaced ten fixed columns — ExecutiveSummary, FinanceCommentary and the rest. Those
    /// worked for exactly one report format. The status report needs six entirely different
    /// sections, and a column per section per format is how a table ends up with forty nullable
    /// text fields of which any given row uses six.
    /// </summary>
    public ICollection<CommitteeReportSectionContent> Sections { get; set; } =
        new List<CommitteeReportSectionContent>();
}

/// <summary>
/// What a report says under one of its headings.
/// </summary>
public class CommitteeReportSectionContent : BaseEntity
{
    public Guid CommitteeReportId { get; set; }
    public CommitteeReport CommitteeReport { get; set; } = default!;

    /// <summary>
    /// The section's stable key, not its heading — see <see cref="ReportSection.Key"/>. Content
    /// keyed by a heading would be orphaned the moment somebody reworded the heading.
    /// </summary>
    public string SectionKey { get; set; } = default!;

    public string Content { get; set; } = default!;
}
