using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

/// <summary>
/// A point-in-time capture of a project's key figures. Phase 1 records the snapshot header plus
/// the metrics that already exist (budget/forecast/stage); later phases extend Metrics as risk,
/// programme and NEC4/SBCC registers come online — see docs/roadmap.md Phase 3/4/6 (Snapshot
/// Comparison Engine). Full entity history is separately available via temporal tables
/// (FOR SYSTEM_TIME AS OF), so a Snapshot is a curated, named point rather than the only way to
/// look back in time.
/// </summary>
public class Snapshot : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public SnapshotType Type { get; set; }
    public string Label { get; set; } = default!;
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public byte RibaStageAtCapture { get; set; }
    public decimal ApprovedBudgetAtCapture { get; set; }
    public decimal ForecastCostAtCapture { get; set; }
}
