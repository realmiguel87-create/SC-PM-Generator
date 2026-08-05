namespace SCPM.Application.RiskManagement.Dtos;

public class RiskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string Category { get; set; } = default!;
    public int Probability { get; set; }
    public int Impact { get; set; }
    public int Score { get; set; }
    public string Status { get; set; } = default!;
    public string? MitigationPlan { get; set; }
}

public class IssueDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string Severity { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateOnly RaisedDate { get; set; }
    public DateOnly? ResolvedDate { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class OpportunityDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal PotentialValue { get; set; }
    public int Probability { get; set; }
    public string Status { get; set; } = default!;
}

public class EscalationDto
{
    public Guid Id { get; set; }
    public Guid? RiskId { get; set; }
    public string? RiskTitle { get; set; }
    public Guid? IssueId { get; set; }
    public string? IssueTitle { get; set; }
    public string Reason { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime RaisedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public string? ResolutionNotes { get; set; }
}
