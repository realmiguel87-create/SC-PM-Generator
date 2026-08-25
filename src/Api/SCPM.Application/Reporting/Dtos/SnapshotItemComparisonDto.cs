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

    /// <summary>Convenience for a caller deciding whether to render anything at all.</summary>
    public bool HasChanges => RiskChanges.Count > 0 || MilestoneChanges.Count > 0;
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
