using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SCPM.Infrastructure.BackgroundJobs;

/// <summary>
/// Registers the Snapshot Engine's recurring jobs (docs/roadmap.md Phase 2), retrying in the
/// background until the Hangfire job-storage database accepts the write.
///
/// This exists because registration is a database write, and the database is not guaranteed to
/// be reachable at the moment the API starts. Previously registration happened inline in
/// Program.cs: first unguarded, which meant an unreachable database killed the process outright,
/// and then wrapped in try/catch, which let the API start but left the recurring jobs
/// unregistered until someone restarted it against a reachable database. Scheduled snapshots
/// would silently not run in the meantime, and nothing would ever recover on its own.
///
/// Both failure modes came from doing the work once, at the one moment least likely to succeed.
/// Azure SQL's serverless tier auto-pauses on inactivity and takes ~30s to resume; a firewall
/// rule can lag an IP change; a network blip is a network blip. All are transient, and all are
/// routine rather than exotic — the first two were hit repeatedly during real local setup.
///
/// So registration retries with exponential backoff instead, and stops as soon as it succeeds.
/// AddOrUpdate is idempotent, so a late registration is indistinguishable from a prompt one:
/// the recurring job definitions end up identical either way, only later.
/// </summary>
public class RecurringJobRegistrationService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(5);

    private readonly IRecurringJobManager _recurringJobs;
    private readonly ILogger<RecurringJobRegistrationService> _logger;

    public RecurringJobRegistrationService(
        IRecurringJobManager recurringJobs,
        ILogger<RecurringJobRegistrationService> logger)
    {
        _recurringJobs = recurringJobs;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield before the first attempt. BackgroundService runs ExecuteAsync synchronously up to
        // its first await, on the host's startup path — so without this, a first attempt against
        // an unreachable database would block the API from serving requests for the length of the
        // SQL connection timeout. That is the exact delay this class exists to avoid.
        await Task.Yield();

        var delay = InitialDelay;
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            attempt++;

            try
            {
                RegisterSnapshotJobs();

                if (attempt == 1)
                {
                    _logger.LogInformation("Registered Hangfire recurring snapshot jobs.");
                }
                else
                {
                    _logger.LogInformation(
                        "Registered Hangfire recurring snapshot jobs on attempt {Attempt}. Scheduled "
                        + "snapshots will now run as configured.", attempt);
                }

                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // The first failure carries the exception; later ones don't. A database that stays
                // down for an hour would otherwise fill the log with identical stack traces, which
                // buries the one that explains what happened rather than illuminating it.
                if (attempt == 1)
                {
                    _logger.LogError(ex,
                        "Could not register Hangfire recurring snapshot jobs — the job storage database "
                        + "was unreachable. The API is serving requests normally and registration will "
                        + "be retried in the background; scheduled snapshots will not run until it "
                        + "succeeds.");
                }
                else
                {
                    _logger.LogWarning(
                        "Hangfire recurring snapshot job registration still failing after {Attempt} "
                        + "attempts ({Message}). Next retry in {Delay}.",
                        attempt, ex.Message, delay);
                }

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return; // Host shutting down mid-backoff — not an error.
                }

                // Exponential up to the cap. The cap matters more than the growth rate: an outage
                // measured in hours shouldn't back off to a retry interval measured in hours too,
                // or recovery is discovered long after it happens.
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxDelay.Ticks));
            }
        }
    }

    /// <summary>
    /// Idempotent — AddOrUpdate replaces an existing definition rather than duplicating it, so
    /// running this on every start (and on every retry) converges on the same three jobs.
    /// </summary>
    private void RegisterSnapshotJobs()
    {
        _recurringJobs.AddOrUpdate<SnapshotJobs>(
            "snapshot-daily", j => j.RunDailySnapshotAsync(CancellationToken.None), Cron.Daily());
        _recurringJobs.AddOrUpdate<SnapshotJobs>(
            "snapshot-weekly", j => j.RunWeeklySnapshotAsync(CancellationToken.None), Cron.Weekly());
        _recurringJobs.AddOrUpdate<SnapshotJobs>(
            "snapshot-monthly", j => j.RunMonthlySnapshotAsync(CancellationToken.None), Cron.Monthly());
    }
}
