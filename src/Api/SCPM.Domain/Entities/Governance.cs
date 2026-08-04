using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

public class Gateway : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;
    public Guid RibaStageInstanceId { get; set; }
    public RibaStageInstance RibaStageInstance { get; set; } = default!;

    public string GatewayType { get; set; } = default!;
    public GatewayStatus Status { get; set; } = GatewayStatus.Pending;
    public DateOnly? DueDate { get; set; }

    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}

public class Approval : SoftDeletableEntity
{
    public Guid GatewayId { get; set; }
    public Gateway Gateway { get; set; } = default!;
    public Guid ApproverUserId { get; set; }

    public ApprovalDecision Decision { get; set; }
    public string? Comments { get; set; }
    public DateTime DecisionDate { get; set; } = DateTime.UtcNow;
}
