using SCPM.Domain.Enums;

namespace SCPM.Application.ProgrammeManagement.Dtos;

/// <summary>One recorded account of why part of a milestone slipped.</summary>
/// <param name="Reference">
/// The contractual reference (an extension-of-time or compensation-event reference) where the
/// cause is evidenced by one, otherwise null. Carried so a reader can find the claim without a
/// second request.
/// </param>
public record MilestoneDelayCauseDto(
    Guid Id,
    int DelayDays,
    DelayCauseCategory Category,
    string Narrative,
    Guid? ExtensionOfTimeId,
    Guid? CompensationEventId,
    string? Reference);

/// <summary>
/// A milestone's slip set against what has been said to account for it.
/// </summary>
/// <param name="UnattributedDays">
/// Slip nobody has explained: <c>SlipDays</c> less the days attributed, floored at zero.
///
/// This is the figure the whole feature exists to produce, and the one no register previously
/// held. A programme three months late with three months accounted for is a managed programme; one
/// three months late with a fortnight accounted for is a programme nobody has got to the bottom
/// of. Both read identically as "92 days late".
/// </param>
/// <param name="OverAttributedDays">
/// Days attributed beyond the slip, where causes sum to more than the milestone actually lost.
///
/// Reported rather than clamped away. It usually means double-counting — the same event entered
/// twice, or two causes claiming the same period — and occasionally means time was recovered
/// elsewhere and the attributions were never revised. Either way it is a discrepancy in the
/// record, and silently absorbing it would hide the one signal that says so.
/// </param>
public record MilestoneDelayAnalysisDto(
    Guid MilestoneId,
    string Name,
    bool IsKeyMilestone,
    int SlipDays,
    int AttributedDays,
    int UnattributedDays,
    int OverAttributedDays,
    IReadOnlyList<MilestoneDelayCauseDto> Causes);

/// <summary>
/// Delay causes across a whole project, with the totals a committee paper needs.
/// </summary>
/// <param name="DaysByCategory">
/// Attributed days grouped by cause, largest first. What a portfolio review looks at: five
/// projects each losing a fortnight to statutory approvals is a process problem, and it is
/// invisible one project at a time.
/// </param>
/// <param name="TotalUnattributedDays">
/// Summed across milestones. A total is defensible here in a way it is not for slip itself,
/// because this measures how much of the programme's delay is unexplained rather than how late the
/// project is — unexplained days on unrelated milestones genuinely do add up as a body of work
/// nobody has done.
/// </param>
public record ProjectDelayAnalysisDto(
    IReadOnlyList<MilestoneDelayAnalysisDto> Milestones,
    IReadOnlyList<DelayCategoryTotalDto> DaysByCategory,
    int TotalSlipDays,
    int TotalAttributedDays,
    int TotalUnattributedDays);

public record DelayCategoryTotalDto(DelayCauseCategory Category, int Days, int CauseCount);
