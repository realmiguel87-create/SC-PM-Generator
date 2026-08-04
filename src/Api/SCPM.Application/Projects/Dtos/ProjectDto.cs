namespace SCPM.Application.Projects.Dtos;

public class ProjectListItemDto
{
    public Guid Id { get; set; }
    public string ProjectRef { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Status { get; set; } = default!;
    public byte CurrentRibaStage { get; set; }
    public string CurrentRibaStageName { get; set; } = default!;
    public decimal ApprovedBudget { get; set; }
    public decimal ForecastCost { get; set; }
    public string? ProgrammeName { get; set; }
}

public class ProjectDetailDto : ProjectListItemDto
{
    public string? Description { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }
    public List<RibaStageInstanceDto> RibaStages { get; set; } = new();
}

public class RibaStageInstanceDto
{
    public Guid Id { get; set; }
    public byte StageNumber { get; set; }
    public string StageName { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateOnly? PlannedStartDate { get; set; }
    public DateOnly? PlannedEndDate { get; set; }
    public DateOnly? ActualStartDate { get; set; }
    public DateOnly? ActualEndDate { get; set; }
}
