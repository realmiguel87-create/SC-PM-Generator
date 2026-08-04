using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

/// <summary>
/// A committee/cabinet/board report or decision paper, following the standard structure from
/// the spec: Executive Summary, Background, Current Position, Finance, Programme, Risk,
/// Stakeholders, Sustainability, Equality Impact, Recommendations, (Appendices — the report's
/// own attached DocumentFiles serve as appendices, not a separate field). Optionally anchored
/// to a Snapshot so "Current Position" reflects a specific point in time rather than live data
/// that keeps moving under the report after it's written.
/// </summary>
public class CommitteeReport : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public Guid? SnapshotId { get; set; }
    public Snapshot? Snapshot { get; set; }

    public CommitteeReportType ReportType { get; set; }
    public string Title { get; set; } = default!;
    public DateOnly? MeetingDate { get; set; }
    public CommitteeReportStatus Status { get; set; } = CommitteeReportStatus.Draft;

    public string ExecutiveSummary { get; set; } = default!;
    public string? Background { get; set; }
    public string? CurrentPosition { get; set; }
    public string? FinanceCommentary { get; set; }
    public string? ProgrammeCommentary { get; set; }
    public string? RiskCommentary { get; set; }
    public string? StakeholderCommentary { get; set; }
    public string? SustainabilityCommentary { get; set; }
    public string? EqualityImpactCommentary { get; set; }
    public string? Recommendations { get; set; }
}
