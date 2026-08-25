namespace SCPM.Application.Reporting.Dtos;

/// <summary>Activity a comparison of two endpoints cannot see.</summary>
public enum IntervalActivityType
{
    /// <summary>
    /// Raised after the earlier snapshot and gone by the later one — created and deleted, or
    /// created and soft-deleted, entirely inside the window. Absent from both endpoints, so no
    /// comparison of the two can report it.
    /// </summary>
    RaisedAndRemoved,

    /// <summary>
    /// Present and identical at both endpoints, but not identical throughout: it moved and moved
    /// back. A risk escalated and de-escalated, a compensation event quoted and returned to
    /// notified. The endpoints agree, so the comparison correctly reports no change — and the
    /// activity still happened.
    /// </summary>
    ChangedAndReverted,
}

/// <summary>
/// What happened between two snapshots that comparing them cannot reveal.
///
/// The item comparison reads each register at two instants and diffs them. That is the right way
/// to answer "what is different now", and it is structurally blind to anything that both began
/// and ended inside the window. For a monthly committee cycle that is a real blind spot: a risk
/// raised and closed within the month, or a compensation event that spiked and was withdrawn,
/// leaves no trace in a month-on-month comparison.
///
/// This is a pointer, not a second diff. Each entry names the register, the item and what kind of
/// activity occurred, and stops there — because reporting field-level detail for a transient item
/// would mean choosing which of its intermediate versions to show, and there is no principled
/// answer to that. Someone who needs the detail has the register and its history.
/// </summary>
public class SnapshotIntervalActivityDto
{
    public Guid FromSnapshotId { get; set; }
    public string FromLabel { get; set; } = default!;
    public DateTime FromCapturedAt { get; set; }

    public Guid ToSnapshotId { get; set; }
    public string ToLabel { get; set; } = default!;
    public DateTime ToCapturedAt { get; set; }

    public List<IntervalActivityItemDto> Items { get; set; } = [];

    public bool HasActivity => Items.Count > 0;
}

public class IntervalActivityItemDto
{
    /// <summary>Which register this item belongs to — "Risk", "Compensation event", and so on.
    /// A uniform shape across registers is deliberate: this is a list to be scanned, and six
    /// differently-shaped lists would be harder to read than one.</summary>
    public string Register { get; set; } = default!;

    public Guid ItemId { get; set; }

    /// <summary>The item as it would be named in a report — title, or reference and description.
    /// Taken from its last version inside the window, since a removed item has no current row.</summary>
    public string Name { get; set; } = default!;

    public IntervalActivityType ActivityType { get; set; }

    /// <summary>How many distinct versions of the row exist inside the window. Always at least 1;
    /// a higher number is a rough measure of how much churn the item saw.</summary>
    public int VersionCount { get; set; }
}
