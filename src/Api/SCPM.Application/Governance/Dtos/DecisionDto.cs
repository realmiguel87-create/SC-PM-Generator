namespace SCPM.Application.Governance.Dtos;

public class DecisionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateOnly DecisionDate { get; set; }
    public string? Rationale { get; set; }
}
