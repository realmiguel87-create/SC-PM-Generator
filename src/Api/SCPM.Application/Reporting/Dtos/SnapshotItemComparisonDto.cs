namespace SCPM.Application.Reporting.Dtos;

/// <summary>How an individual register item differs between two points in time.</summary>
public enum ItemChangeType
{
    /// <summary>Did not exist at the earlier point; does at the later one.</summary>
    Added,

    /// <summary>Existed at the earlier point; does not at the later one — created and then
    /// deleted, or soft-deleted in between.</summary>
    Removed,

    /// <summary>Present at both points, with at least one tracked field different.</summary>
    Modified,
}

/// <summary>
/// Which items changed between two snapshots, as opposed to how many.
///
/// The aggregate comparison (SnapshotComparisonDto) answers "the open risk count went from 12 to
/// 14". This answers "these two were added, this one was closed, and this one's score went from 6
/// to 20" — which is the version a committee paper can actually be written from.
///
/// Only items that changed appear. An unchanged register produces empty lists, not a list of
/// every row marked unchanged: the point is to be read, and a diff nobody can skim is a diff
/// nobody reads.
///
/// Which registers are covered is not arbitrary. It is exactly the set the aggregate comparison
/// already counts — risks, milestones, early warnings, compensation events, variations and
/// extensions of time — so the two views always answer the same question at two levels of
/// detail. A register that appeared here but not in the aggregates (or the reverse) would invite
/// exactly the "these two numbers disagree, which is right?" problem that SnapshotMetrics exists
/// to prevent.
/// </summary>
public class SnapshotItemComparisonDto
{
    public Guid FromSnapshotId { get; set; }
    public string FromLabel { get; set; } = default!;
    public DateTime FromCapturedAt { get; set; }

    public Guid ToSnapshotId { get; set; }
    public string ToLabel { get; set; } = default!;
    public DateTime ToCapturedAt { get; set; }

    public List<RiskChangeDto> RiskChanges { get; set; } = [];
    public List<MilestoneChangeDto> MilestoneChanges { get; set; } = [];
    public List<EarlyWarningChangeDto> EarlyWarningChanges { get; set; } = [];
    public List<CompensationEventChangeDto> CompensationEventChanges { get; set; } = [];
    public List<VariationChangeDto> VariationChanges { get; set; } = [];
    public List<ExtensionOfTimeChangeDto> ExtensionOfTimeChanges { get; set; } = [];

    /// <summary>Convenience for a caller deciding whether to render anything at all.</summary>
    public bool HasChanges =>
        RiskChanges.Count > 0
        || MilestoneChanges.Count > 0
        || EarlyWarningChanges.Count > 0
        || CompensationEventChanges.Count > 0
        || VariationChanges.Count > 0
        || ExtensionOfTimeChanges.Count > 0;
}

/// <summary>An early warning's movement. Status is the only thing an early warning has to say
/// — it is open or it is closed — so that is the only tracked field.</summary>
public class EarlyWarningChangeDto
{
    public Guid EarlyWarningId { get; set; }
    public string Title { get; set; } = default!;
    public ItemChangeType ChangeType { get; set; }
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
}

/// <summary>
/// A compensation event's movement, in both dimensions a CE moves in: where it is in the
/// NEC4 process, and what it is expected to cost.
/// </summary>
public class CompensationEventChangeDto
{
    public Guid CompensationEventId { get; set; }
    public string Reference { get; set; } = default!;
    public string Title { get; set; } = default!;
    public ItemChangeType ChangeType { get; set; }

    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }

    public decimal? FromEstimatedValue { get; set; }
    public decimal? ToEstimatedValue { get; set; }

    /// <summary>Null unless the event existed at both points.</summary>
    public decimal? EstimatedValueDelta =>
        FromEstimatedValue.HasValue && ToEstimatedValue.HasValue
            ? ToEstimatedValue - FromEstimatedValue
            : null;
}

/// <summary>A variation's movement — status through the SBCC process, and its value impact.</summary>
public class VariationChangeDto
{
    public Guid VariationId { get; set; }
    public string Reference { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ItemChangeType ChangeType { get; set; }

    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }

    public decimal? FromValueImpact { get; set; }
    public decimal? ToValueImpact { get; set; }

    public decimal? ValueImpactDelta =>
        FromValueImpact.HasValue && ToValueImpact.HasValue ? ToValueImpact - FromValueImpact : null;
}

/// <summary>
/// An extension of time's movement. Claimed and awarded days are tracked separately and
/// deliberately: a claim rising is a contractor's position, an award rising is the project's
/// programme actually moving, and reporting them as one number would conflate an argument with
/// a fact.
/// </summary>
public class ExtensionOfTimeChangeDto
{
    public Guid ExtensionOfTimeId { get; set; }
    public string Reference { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public ItemChangeType ChangeType { get; set; }

    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }

    public int? FromDaysClaimed { get; set; }
    public int? ToDaysClaimed { get; set; }

    /// <summary>Null while a claim is undetermined — which is different from an award of zero,
    /// and the difference is the whole substance of an extension-of-time dispute.</summary>
    public int? FromDaysAwarded { get; set; }
    public int? ToDaysAwarded { get; set; }

    public int? DaysAwardedDelta =>
        FromDaysAwarded.HasValue && ToDaysAwarded.HasValue ? ToDaysAwarded - FromDaysAwarded : null;
}

/// <summary>
/// One risk's movement. From* fields are null for an Added risk and To* for a Removed one, which
/// is what distinguishes "appeared at score 20" from "was already there and rose to 20" — a
/// distinction that matters when the question is whether the project is deteriorating.
/// </summary>
public class RiskChangeDto
{
    public Guid RiskId { get; set; }

    /// <summary>The title as at the later point, falling back to the earlier one for a removed
    /// risk. A removed item has no current row to name it, and an unnamed row in a committee
    /// paper is worse than a slightly stale name.</summary>
    public string Title { get; set; } = default!;

    public ItemChangeType ChangeType { get; set; }

    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }

    public int? FromProbability { get; set; }
    public int? ToProbability { get; set; }

    public int? FromImpact { get; set; }
    public int? ToImpact { get; set; }

    public int? FromScore { get; set; }
    public int? ToScore { get; set; }

    /// <summary>Null unless the risk existed at both points — there is no movement to report
    /// for something that was not there to move.</summary>
    public int? ScoreDelta => FromScore.HasValue && ToScore.HasValue ? ToScore - FromScore : null;
}

/// <summary>One milestone's movement, in the terms the programme is actually judged on: dates
/// against baseline, and days of slip.</summary>
public class MilestoneChangeDto
{
    public Guid MilestoneId { get; set; }
    public string Name { get; set; } = default!;
    public ItemChangeType ChangeType { get; set; }

    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }

    public DateOnly? FromForecastDate { get; set; }
    public DateOnly? ToForecastDate { get; set; }

    public DateOnly? FromActualDate { get; set; }
    public DateOnly? ToActualDate { get; set; }

    /// <summary>Days late against baseline at each point, negative when ahead. Uses actual date
    /// once set, forecast otherwise — the same rule SnapshotMetrics applies.</summary>
    public int? FromDelayDays { get; set; }
    public int? ToDelayDays { get; set; }

    /// <summary>How much further late (or, negative, how much recovered). Null unless the
    /// milestone existed at both points.</summary>
    public int? DelayDaysDelta =>
        FromDelayDays.HasValue && ToDelayDays.HasValue ? ToDelayDays - FromDelayDays : null;
}
