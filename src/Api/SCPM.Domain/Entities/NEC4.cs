using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

public class EarlyWarning : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateOnly RaisedDate { get; set; }
    public string? MitigationAction { get; set; }
    public Nec4RegisterStatus Status { get; set; } = Nec4RegisterStatus.Open;
    public Guid RaisedByUserId { get; set; }
}

/// <summary>An NEC4 compensation event — the clause reference is free text (e.g. "60.1(1)")
/// rather than a lookup, since the full NEC4 clause taxonomy varies by contract option.</summary>
public class CompensationEvent : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Reference { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? ClauseReference { get; set; }
    public decimal EstimatedValue { get; set; }
    public CompensationEventStatus Status { get; set; } = CompensationEventStatus.Notified;
    public DateOnly NotifiedDate { get; set; }
}

/// <summary>One row of NEC4 Contract Data (Part One = Employer's data, Part Two = Contractor's data).</summary>
public class ContractDataEntry : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public ContractDataPart Part { get; set; }
    public string ClauseReference { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Value { get; set; } = default!;
}

public class RiskAllocationItem : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Description { get; set; } = default!;
    public RiskAllocationParty AllocatedTo { get; set; }
    public string? MitigationOwner { get; set; }
}

/// <summary>A record of each Accepted Programme revision under NEC4 clause 31/32 — the
/// programme itself is tracked via Programme.Milestone; this is the acceptance log.</summary>
public class AcceptedProgrammeEntry : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public int RevisionNumber { get; set; }
    public DateOnly AcceptedDate { get; set; }
    public string? Notes { get; set; }
}

public class PaymentAssessment : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public int AssessmentNumber { get; set; }
    public DateOnly AssessmentDate { get; set; }
    public decimal AmountDue { get; set; }
    public PaymentAssessmentStatus Status { get; set; } = PaymentAssessmentStatus.Assessed;
}

/// <summary>The overall change register — a rollup of accepted compensation events and other
/// contract changes, kept separate from CompensationEvent since not every change originates
/// as a CE (e.g. an employer-instructed scope change agreed outside the CE mechanism).</summary>
public class ChangeRegisterItem : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal ValueImpact { get; set; }
    public int TimeImpactDays { get; set; }
    public ChangeRegisterStatus Status { get; set; } = ChangeRegisterStatus.Proposed;
}
