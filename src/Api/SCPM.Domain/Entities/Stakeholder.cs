using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

public class Stakeholder : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string? Organisation { get; set; }
    public string? RoleTitle { get; set; }
    public string? ContactEmail { get; set; }

    public StakeholderInfluence Influence { get; set; } = StakeholderInfluence.Medium;
    public StakeholderInterest Interest { get; set; } = StakeholderInterest.Medium;

    public ICollection<StakeholderEngagement> Engagements { get; set; } = new List<StakeholderEngagement>();
}

/// <summary>A logged touchpoint with a stakeholder — the engagement tracker.</summary>
public class StakeholderEngagement : BaseEntity
{
    public Guid StakeholderId { get; set; }
    public Stakeholder Stakeholder { get; set; } = default!;

    public DateOnly EngagementDate { get; set; }
    public string Method { get; set; } = default!; // e.g. Meeting, Letter, Consultation Event, Email
    public string Summary { get; set; } = default!;
    public string? Outcome { get; set; }
}
