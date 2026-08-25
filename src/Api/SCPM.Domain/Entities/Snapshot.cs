using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

/// <summary>
/// A point-in-time capture of a project's key figures. Full entity history is separately
/// available via temporal tables (FOR SYSTEM_TIME AS OF), so a Snapshot is a curated, named
/// point rather than the only way to look back in time.
///
/// Originally this held project-header figures only (stage, budget, forecast), which meant the
/// Snapshot Comparison Engine could report that a budget had moved but not that the risk
/// position had deteriorated or the programme had slipped — the things a committee report
/// actually turns on. It now also captures register *aggregates*: counts and totals, not the
/// individual rows.
///
/// That boundary is deliberate. Copying every risk, milestone and compensation event into every
/// snapshot of every active project, daily, would grow without limit for a level of detail
/// nothing yet asks for — and the per-row history already exists in the temporal tables if it
/// is ever needed. Aggregates answer "what moved, and by how much" between two dates, which is
/// the question the comparison engine exists to answer. Item-level diffs ("*which* risk moved")
/// remain separate, later work — see docs/roadmap.md.
///
/// Each metric's definition (what counts as "open", where a threshold sits) lives in
/// SnapshotMetrics so capture and any future reporting cannot drift apart on the meaning.
/// </summary>
public class Snapshot : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public SnapshotType Type { get; set; }
    public string Label { get; set; } = default!;
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    // --- Project header ---
    public byte RibaStageAtCapture { get; set; }
    public decimal ApprovedBudgetAtCapture { get; set; }
    public decimal ForecastCostAtCapture { get; set; }

    // --- Risk register ---
    /// <summary>Risks in an Open or Escalated state. Mitigated and Closed are excluded — they
    /// are no longer positions the project is carrying.</summary>
    public int OpenRiskCount { get; set; }

    /// <summary>Open risks scoring at or above the reportable threshold on the 1-25
    /// probability x impact scale (see SnapshotMetrics.HighRiskScoreThreshold).</summary>
    public int HighRiskCount { get; set; }

    /// <summary>Summed score of all open risks. Moves when probability or impact is
    /// re-assessed even if no risk is added or closed, so it catches drift that a count
    /// alone would hide.</summary>
    public int TotalOpenRiskScore { get; set; }

    // --- Issue log ---
    /// <summary>Issues that are Open or InProgress — not yet Resolved or Closed.</summary>
    public int OpenIssueCount { get; set; }

    /// <summary>Open issues at High or Critical severity.</summary>
    public int SevereOpenIssueCount { get; set; }

    // --- Programme ---
    public int MilestoneCount { get; set; }
    public int MilestonesCompleteCount { get; set; }

    /// <summary>Milestones whose actual date (once set) or forecast date is later than
    /// baseline. Derived from the dates rather than from MilestoneStatus, so a milestone that
    /// has slipped but whose status nobody has updated is still counted.</summary>
    public int MilestonesDelayedCount { get; set; }

    /// <summary>Largest single slip in days across all milestones; 0 when nothing has slipped.
    /// A count of delayed milestones cannot distinguish ten one-day slips from one six-month
    /// slip, and for committee reporting that difference is the whole point.</summary>
    public int WorstMilestoneDelayDays { get; set; }

    // --- NEC4 ---
    /// <summary>Early warnings still Open.</summary>
    public int OpenEarlyWarningCount { get; set; }

    /// <summary>Compensation events not yet concluded — Notified, Quoted or Accepted.
    /// Implemented and Rejected are excluded as settled.</summary>
    public int OpenCompensationEventCount { get; set; }

    /// <summary>Summed estimated value of compensation events excluding Rejected ones: the
    /// cost exposure the project carries from CEs, whether or not they are settled.</summary>
    public decimal CompensationEventValue { get; set; }

    // --- SBCC ---
    /// <summary>Variations not yet Agreed — Instructed or Priced.</summary>
    public int OpenVariationCount { get; set; }

    /// <summary>Summed value impact of every variation on the register.</summary>
    public decimal VariationValue { get; set; }

    /// <summary>Total days awarded across all extensions of time. Awarded only — a claim that
    /// has not been determined is not yet a programme fact.</summary>
    public int ExtensionOfTimeDaysAwarded { get; set; }
}
