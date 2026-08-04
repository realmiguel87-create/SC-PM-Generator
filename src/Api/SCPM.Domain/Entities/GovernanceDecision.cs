using SCPM.Domain.Common;

namespace SCPM.Domain.Entities;

/// <summary>An entry in the project's decision register — a governance record distinct from a
/// Gateway approval (which gates RIBA stage progression). Decisions capture day-to-day
/// governance calls (e.g. "approved change of cladding supplier") that don't gate a stage.</summary>
public class DecisionRegisterEntry : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Guid DecisionOwnerUserId { get; set; }
    public DateOnly DecisionDate { get; set; }
    public string? Rationale { get; set; }
}
