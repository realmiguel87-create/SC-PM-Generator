using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

/// <summary>A risk on the project's risk register. Score is Probability * Impact (1-5 each,
/// giving a 1-25 heatmap range) — computed, not stored, so it never drifts from its inputs.</summary>
public class Risk : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string Category { get; set; } = default!; // e.g. Cost, Programme, Design, Construction, Stakeholder

    public int Probability { get; set; } // 1 (rare) - 5 (almost certain)
    public int Impact { get; set; }      // 1 (negligible) - 5 (severe)

    public RiskStatus Status { get; set; } = RiskStatus.Open;
    public string? MitigationPlan { get; set; }
    public Guid? OwnerUserId { get; set; }

    public int Score => Probability * Impact;
}

/// <summary>An issue — a risk that has materialised, or a problem raised directly.</summary>
public class Issue : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public IssueSeverity Severity { get; set; } = IssueSeverity.Medium;
    public IssueStatus Status { get; set; } = IssueStatus.Open;

    public Guid? OwnerUserId { get; set; }
    public DateOnly RaisedDate { get; set; }
    public DateOnly? ResolvedDate { get; set; }
    public string? ResolutionNotes { get; set; }
}

/// <summary>A potential upside — the register's counterpart to Risk.</summary>
public class Opportunity : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal PotentialValue { get; set; }
    public int Probability { get; set; } // 1-5, consistent scale with Risk for combined reporting

    public OpportunityStatus Status { get; set; } = OpportunityStatus.Identified;
    public Guid? OwnerUserId { get; set; }
}

/// <summary>
/// Escalates a Risk or Issue for a decision above the project team's authority — exactly one
/// of RiskId/IssueId is set. Distinct from a Governance.Gateway (which gates RIBA stage
/// progression): an Escalation is raised ad hoc, whenever risk/issue severity demands it.
/// </summary>
public class Escalation : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public Guid? RiskId { get; set; }
    public Risk? Risk { get; set; }
    public Guid? IssueId { get; set; }
    public Issue? Issue { get; set; }

    public string Reason { get; set; } = default!;
    public EscalationStatus Status { get; set; } = EscalationStatus.Pending;

    public Guid RaisedByUserId { get; set; }
    public DateTime RaisedDate { get; set; } = DateTime.UtcNow;

    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public string? ResolutionNotes { get; set; }
}
