namespace SCPM.Application.Governance.Dtos;

public class GatewayDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RibaStageInstanceId { get; set; }
    public byte StageNumber { get; set; }
    public string GatewayType { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateOnly? DueDate { get; set; }
}
