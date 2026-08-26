using SCPM.Domain.Common;

namespace SCPM.Domain.Entities;

/// <summary>
/// A sanctioned set of milestone dates, captured at a point in time.
///
/// Why this exists, given `Milestone` is already a temporal table and its old `BaselineDate`
/// values are recoverable: the temporal history records *that* a date changed and *when*. It does
/// not record that fifteen milestone edits made on the same afternoon were one deliberate
/// rebaseline rather than fifteen unrelated corrections, who sanctioned it, or on what grounds.
/// Those are governance facts, and reconstructing them from a change log means guessing.
///
/// The reporting problem is the sharper one. <see cref="Milestone.DelayDays"/> measures against
/// whatever <see cref="Milestone.BaselineDate"/> currently holds, so the moment a programme is
/// rebaselined every milestone reports zero slip and the timeline turns green — while the project
/// is exactly as late as it was the day before. A committee that sanctioned a programme in March
/// is entitled to ask how far from *that* programme the project now sits, and answering it needs
/// the March dates held as a named, addressable thing rather than inferred from a timestamp.
///
/// So slip stops being a property of a milestone and becomes a question about a pair: this
/// milestone, measured against that baseline.
/// </summary>
public class ProgrammeBaseline : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    /// <summary>
    /// Sequential per project, starting at 1. The original sanctioned programme is always
    /// revision 1, whether it was captured deliberately or backfilled at the first rebaseline.
    /// </summary>
    public int Revision { get; set; }

    /// <summary>What this baseline is called in a committee paper — "Post-tender programme".</summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Why the programme was rebaselined. Required, because a rebaseline without a stated reason
    /// is indistinguishable from quietly moving the goalposts — and this is the field that makes
    /// the record evidence rather than a note.
    /// </summary>
    public string Reason { get; set; } = default!;

    /// <summary>
    /// Who sanctioned it and when — the committee decision or delegated authority behind the
    /// change, as distinct from <see cref="BaseEntity.CreatedBy"/>, which is whoever typed it in.
    /// Nullable because revision 1 may be backfilled from dates that predate this record, where
    /// naming an approver would be inventing one.
    /// </summary>
    public Guid? ApprovedBy { get; set; }
    public DateOnly? ApprovedDate { get; set; }

    /// <summary>
    /// True for the baseline that <see cref="Milestone.BaselineDate"/> currently reflects. Exactly
    /// one per project.
    ///
    /// Stored rather than derived from the highest revision because those two can disagree — a
    /// baseline soft-deleted after being superseded would leave the highest surviving revision
    /// looking current when it is not — and a flag that can be checked is easier to enforce than
    /// an ordering that has to be recomputed everywhere it matters.
    /// </summary>
    public bool IsCurrent { get; set; }

    public ICollection<ProgrammeBaselineEntry> Entries { get; set; } = new List<ProgrammeBaselineEntry>();
}

/// <summary>
/// One milestone's date within a baseline.
/// </summary>
public class ProgrammeBaselineEntry : BaseEntity
{
    public Guid ProgrammeBaselineId { get; set; }
    public ProgrammeBaseline ProgrammeBaseline { get; set; } = default!;

    /// <summary>
    /// The milestone this date was captured from. Kept for joining back to live data, but not
    /// relied on for display — see <see cref="MilestoneName"/>.
    /// </summary>
    public Guid MilestoneId { get; set; }
    public Milestone Milestone { get; set; } = default!;

    /// <summary>
    /// The milestone's name as it stood when the baseline was captured, copied rather than joined.
    ///
    /// A baseline is a historical record of what was sanctioned, so it has to stay readable when
    /// the milestone it came from is later renamed or soft-deleted. Joining to the live row would
    /// silently rewrite a committee-approved document to match today's names — or drop rows
    /// entirely once the soft-delete filter applies — which is precisely the failure this record
    /// exists to prevent.
    /// </summary>
    public string MilestoneName { get; set; } = default!;

    /// <summary>The date this milestone was sanctioned for, under this baseline.</summary>
    public DateOnly BaselineDate { get; set; }
}
