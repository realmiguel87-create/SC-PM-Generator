using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Common;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps CreatedBy/ModifiedBy and writes Audit.ActivityLog + Audit.FieldAudit for every
/// tracked create/update/delete. This runs for every SaveChanges call regardless of which
/// module's handler triggered it, so audit coverage cannot be missed by a future feature.
/// Deletes on soft-deletable entities are converted to an update (IsDeleted = true) —
/// governance-critical rows are never physically removed.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditSaveChangesInterceptor(ICurrentUserService currentUser) => _currentUser = currentUser;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null) return;

        var actorId = _currentUser.UserId;
        var now = DateTime.UtcNow;
        var activityEntries = new List<ActivityLogEntry>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is ActivityLogEntry or FieldAuditEntry)
                continue;

            switch (entry.State)
            {
                case EntityState.Added when entry.Entity is BaseEntity added:
                    added.CreatedBy = actorId ?? added.CreatedBy;
                    added.CreatedDate = now;
                    activityEntries.Add(NewActivity("Create", entry, actorId));
                    break;

                case EntityState.Modified when entry.Entity is SoftDeletableEntity soft && soft.IsDeleted && IsPropertyModified(entry, nameof(SoftDeletableEntity.IsDeleted)):
                    soft.DeletedBy = actorId;
                    soft.DeletedDate = now;
                    activityEntries.Add(NewActivity("Delete", entry, actorId));
                    break;

                case EntityState.Modified when entry.Entity is BaseEntity modified:
                    modified.ModifiedBy = actorId;
                    modified.ModifiedDate = now;
                    var activity = NewActivity("Update", entry, actorId);
                    activity.FieldChanges = BuildFieldAudit(entry, actorId, now);
                    activityEntries.Add(activity);
                    break;

                case EntityState.Deleted:
                    // Physical deletes should not occur for audited entities; log anyway so
                    // an unexpected hard delete is never silently invisible.
                    activityEntries.Add(NewActivity("Delete", entry, actorId));
                    break;
            }
        }

        if (activityEntries.Count > 0)
            context.Set<ActivityLogEntry>().AddRange(activityEntries);
    }

    private static bool IsPropertyModified(EntityEntry entry, string propertyName) =>
        entry.Property(propertyName).IsModified;

    private static ActivityLogEntry NewActivity(string action, EntityEntry entry, Guid? actorId) => new()
    {
        UserId = actorId,
        Action = action,
        EntityType = entry.Entity.GetType().Name,
        EntityId = entry.Entity is BaseEntity be ? be.Id : null,
        OccurredAt = DateTime.UtcNow
    };

    private static List<FieldAuditEntry> BuildFieldAudit(EntityEntry entry, Guid? actorId, DateTime now)
    {
        var changes = new List<FieldAuditEntry>();

        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            var oldValue = property.OriginalValue?.ToString();
            var newValue = property.CurrentValue?.ToString();
            if (oldValue == newValue) continue;

            changes.Add(new FieldAuditEntry
            {
                EntityName = entry.Entity.GetType().Name,
                FieldName = property.Metadata.Name,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedBy = actorId,
                ChangedDate = now
            });
        }

        return changes;
    }
}
