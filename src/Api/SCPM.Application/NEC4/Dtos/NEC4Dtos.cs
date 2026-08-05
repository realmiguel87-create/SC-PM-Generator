namespace SCPM.Application.NEC4.Dtos;

public class EarlyWarningDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateOnly RaisedDate { get; set; }
    public string? MitigationAction { get; set; }
    public string Status { get; set; } = default!;
}

public class CompensationEventDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? ClauseReference { get; set; }
    public decimal EstimatedValue { get; set; }
    public string Status { get; set; } = default!;
    public DateOnly NotifiedDate { get; set; }
}

public class ContractDataEntryDto
{
    public Guid Id { get; set; }
    public string Part { get; set; } = default!;
    public string ClauseReference { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Value { get; set; } = default!;
}

public class RiskAllocationItemDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = default!;
    public string AllocatedTo { get; set; } = default!;
    public string? MitigationOwner { get; set; }
}

public class AcceptedProgrammeEntryDto
{
    public Guid Id { get; set; }
    public int RevisionNumber { get; set; }
    public DateOnly AcceptedDate { get; set; }
    public string? Notes { get; set; }
}

public class PaymentAssessmentDto
{
    public Guid Id { get; set; }
    public int AssessmentNumber { get; set; }
    public DateOnly AssessmentDate { get; set; }
    public decimal AmountDue { get; set; }
    public string Status { get; set; } = default!;
}

public class ChangeRegisterItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal ValueImpact { get; set; }
    public int TimeImpactDays { get; set; }
    public string Status { get; set; } = default!;
}
