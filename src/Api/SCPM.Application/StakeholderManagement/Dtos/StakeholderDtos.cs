namespace SCPM.Application.StakeholderManagement.Dtos;

public class StakeholderEngagementDto
{
    public Guid Id { get; set; }
    public DateOnly EngagementDate { get; set; }
    public string Method { get; set; } = default!;
    public string Summary { get; set; } = default!;
    public string? Outcome { get; set; }
}

public class StakeholderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Organisation { get; set; }
    public string? RoleTitle { get; set; }
    public string? ContactEmail { get; set; }
    public string Influence { get; set; } = default!;
    public string Interest { get; set; } = default!;
    public List<StakeholderEngagementDto> Engagements { get; set; } = new();
}
