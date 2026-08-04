using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Commands.CreateSnapshot;
using SCPM.Domain.Enums;

namespace SCPM.Infrastructure.BackgroundJobs;

/// <summary>
/// Scheduled snapshot jobs, registered with Hangfire's recurring scheduler at startup (see
/// Program.cs). Each run captures one Snapshot per active project via CreateSnapshotCommand —
/// the same command a user-initiated manual snapshot uses (ReportingController) — so scheduled
/// and manual snapshots are identical in shape and go through the same audit trail.
/// Hangfire resolves this class per job execution through the app's DI container, so
/// constructor-injected scoped services (IAppDbContext, ISender) work exactly as they would
/// in a request.
/// </summary>
public class SnapshotJobs
{
    private readonly IAppDbContext _db;
    private readonly ISender _mediator;

    public SnapshotJobs(IAppDbContext db, ISender mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public Task RunDailySnapshotAsync(CancellationToken ct = default) => CaptureAllAsync(SnapshotType.Daily, ct);
    public Task RunWeeklySnapshotAsync(CancellationToken ct = default) => CaptureAllAsync(SnapshotType.Weekly, ct);
    public Task RunMonthlySnapshotAsync(CancellationToken ct = default) => CaptureAllAsync(SnapshotType.Monthly, ct);

    private async Task CaptureAllAsync(SnapshotType type, CancellationToken ct)
    {
        var activeProjectIds = await _db.Projects
            .Where(p => !p.IsDeleted && p.Status == ProjectStatus.Active)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var label = $"{type} snapshot — {DateTime.UtcNow:yyyy-MM-dd}";

        foreach (var projectId in activeProjectIds)
            await _mediator.Send(new CreateSnapshotCommand(projectId, type, label), ct);
    }
}
