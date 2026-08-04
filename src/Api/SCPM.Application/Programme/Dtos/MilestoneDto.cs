namespace SCPM.Application.Programme.Dtos;

public class MilestoneDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Status { get; set; } = default!;
    public DateOnly BaselineDate { get; set; }
    public DateOnly ForecastDate { get; set; }
    public DateOnly? ActualDate { get; set; }
    public bool IsKeyMilestone { get; set; }
    public int DelayDays { get; set; }
}
