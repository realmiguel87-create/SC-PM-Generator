namespace SCPM.Domain.Entities;

public class ActivityLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Action { get; set; } = default!; // Create, Update, Delete, Approve, Reject, GenerateReport, ExportFile, Login, Logout
    public string EntityType { get; set; } = default!;
    public Guid? EntityId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }

    public ICollection<FieldAuditEntry> FieldChanges { get; set; } = new List<FieldAuditEntry>();
}

public class FieldAuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityLogId { get; set; }
    public string EntityName { get; set; } = default!;
    public string FieldName { get; set; } = default!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? ChangedBy { get; set; }
    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
}
