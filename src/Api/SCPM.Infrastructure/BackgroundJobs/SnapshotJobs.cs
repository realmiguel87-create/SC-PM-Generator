namespace SCPM.Infrastructure.BackgroundJobs;

/// <summary>
/// Scheduled snapshot jobs (Phase 2 implements the Snapshot Engine itself — see docs/roadmap.md).
/// Registered with Hangfire's recurring job scheduler at startup, e.g.:
///   RecurringJob.AddOrUpdate&lt;SnapshotJobs&gt;("daily-snapshot", j => j.RunDailySnapshotAsync(), Cron.Daily);
/// </summary>
public class SnapshotJobs
{
    public Task RunDailySnapshotAsync() => Task.CompletedTask;
    public Task RunWeeklySnapshotAsync() => Task.CompletedTask;
    public Task RunMonthlySnapshotAsync() => Task.CompletedTask;
}
