namespace SCPM.Application.Reporting.Dtos;

public class SnapshotDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Label { get; set; } = default!;
    public DateTime CapturedAt { get; set; }
    public byte RibaStageAtCapture { get; set; }
    public decimal ApprovedBudgetAtCapture { get; set; }
    public decimal ForecastCostAtCapture { get; set; }
}
