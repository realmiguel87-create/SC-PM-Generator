using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

/// <summary>
/// An account of why some part of a milestone's slip happened.
///
/// The programme could say a milestone was 92 days late and nothing could say why. That is the
/// difference between a report a committee can read and one it can act on: "Start on site is three
/// months late" invites the question this record answers, and until now the answer lived in
/// someone's head or a covering email.
///
/// **Days sit here, not on the contractual event.** A compensation event or extension of time has
/// one effect on the contract completion date, but a different effect on each milestone — an event
/// worth 60 days to the contract may account for 60 days of one milestone's slip and none of
/// another's. A single number on the event cannot express that; this record can, because it is
/// about a specific pair.
///
/// **Attribution is deliberately not required to be complete or exclusive.** Real programmes have
/// slip nobody has yet explained, and the honest thing is to report that remainder rather than
/// force it into a cause. See <c>MilestoneDelayAnalysisDto.UnattributedDays</c>, which is the
/// figure this whole record exists to make computable.
/// </summary>
public class MilestoneDelayCause : SoftDeletableEntity
{
    public Guid MilestoneId { get; set; }
    public Milestone Milestone { get; set; } = default!;

    /// <summary>
    /// How many days of *this milestone's* slip this cause accounts for. Always positive: a cause
    /// that recovered time is not a delay cause, and recording one as a negative would let a
    /// programme explain away slip it never made up.
    /// </summary>
    public int DelayDays { get; set; }

    public DelayCauseCategory Category { get; set; }

    /// <summary>
    /// What actually happened, in words. Required even when a contractual event is linked: the
    /// reference tells a reader which claim, not what went wrong, and a register of references
    /// with no narrative is only navigable by someone who already knows the story.
    /// </summary>
    public string Narrative { get; set; } = default!;

    /// <summary>
    /// The extension-of-time claim evidencing this cause, where there is one. Null for a cause
    /// nobody has claimed against — which is common and is not a defect: a contractor's own
    /// resourcing failure is a real delay cause with no claim attached, and the register would be
    /// misleading if it could only record the ones somebody was paid for.
    /// </summary>
    public Guid? ExtensionOfTimeId { get; set; }
    public ExtensionOfTime? ExtensionOfTime { get; set; }

    /// <summary>The compensation event evidencing this cause, where there is one.</summary>
    public Guid? CompensationEventId { get; set; }
    public CompensationEvent? CompensationEvent { get; set; }
}
