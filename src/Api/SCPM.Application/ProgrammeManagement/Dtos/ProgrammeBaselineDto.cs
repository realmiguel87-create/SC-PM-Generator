namespace SCPM.Application.ProgrammeManagement.Dtos;

/// <summary>Summary of one sanctioned programme, for listing them.</summary>
public record ProgrammeBaselineDto(
    Guid Id,
    int Revision,
    string Name,
    string Reason,
    Guid? ApprovedBy,
    DateOnly? ApprovedDate,
    bool IsCurrent,
    DateTime CreatedDate,
    int MilestoneCount);

/// <summary>
/// One milestone measured against a chosen baseline.
///
/// <c>SlipDays</c> is deliberately not read from <c>Milestone.DelayDays</c>: that property
/// measures against whatever the milestone's current baseline date is, which after a rebaseline is
/// the *new* programme. The point of this query is to answer the other question — how far the
/// project has drifted from a programme sanctioned at some earlier point.
/// </summary>
/// <param name="BaselineName">
/// The milestone's name as it stood when the baseline was captured, which may differ from the name
/// it carries today.
/// </param>
/// <param name="AddedSinceBaseline">
/// True when the milestone did not exist in this baseline — added to the programme after it was
/// sanctioned. It has no baseline date to be measured against, so it carries no slip.
/// </param>
public record MilestoneAgainstBaselineDto(
    Guid MilestoneId,
    string Name,
    string BaselineName,
    DateOnly? BaselineDate,
    DateOnly CurrentDate,
    bool CurrentDateIsActual,
    int SlipDays,
    bool IsKeyMilestone,
    bool AddedSinceBaseline);

/// <summary>
/// A whole programme measured against one baseline.
/// </summary>
/// <param name="WorstSlipDays">
/// Largest single slip in days. Not a total: ten milestones one day late is a different programme
/// from one six months late, and summing makes those read alike — the same reasoning as the
/// timeline's delay summary.
/// </param>
/// <param name="RemovedSinceBaseline">
/// Milestones in the baseline that no longer exist on the project — deleted since it was
/// sanctioned. Named rather than counted, because a milestone quietly dropping out of an approved
/// programme is something a reader needs to see, not a number.
/// </param>
public record ProgrammeAgainstBaselineDto(
    ProgrammeBaselineDto Baseline,
    IReadOnlyList<MilestoneAgainstBaselineDto> Milestones,
    int WorstSlipDays,
    string? WorstSlipMilestone,
    IReadOnlyList<string> RemovedSinceBaseline);
