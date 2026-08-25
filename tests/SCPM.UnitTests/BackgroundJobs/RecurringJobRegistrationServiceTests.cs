using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SCPM.Infrastructure.BackgroundJobs;
using Xunit;

namespace SCPM.UnitTests.BackgroundJobs;

/// <summary>
/// Covers the behaviour that matters about RecurringJobRegistrationService: that it registers the
/// three snapshot jobs, that a database that is down when the API starts does not lose them
/// permanently, and that it gives up cleanly on shutdown rather than retrying into a stopped host.
///
/// These assert against IRecurringJobManager — the interface the service actually depends on —
/// rather than Hangfire's static RecurringJob facade, which is precisely why the service takes
/// the interface. The old inline Program.cs code could not be tested at all.
/// </summary>
public class RecurringJobRegistrationServiceTests
{
    private static RecurringJobRegistrationService CreateSut(IRecurringJobManager manager) =>
        new(manager, NullLogger<RecurringJobRegistrationService>.Instance);

    [Fact]
    public async Task Registers_all_three_snapshot_jobs_when_storage_is_reachable()
    {
        var manager = Substitute.For<IRecurringJobManager>();

        await CreateSut(manager).StartAsync(CancellationToken.None);
        await WaitForRegistrationAsync(manager);

        // Asserting on the job ids, not just the call count: the three schedules are separate
        // recurring jobs and a copy-paste slip that registered "snapshot-daily" three times would
        // otherwise pass.
        var registeredIds = manager.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IRecurringJobManager.AddOrUpdate))
            .Select(c => (string)c.GetArguments()[0]!)
            .ToList();

        registeredIds.Should().BeEquivalentTo("snapshot-daily", "snapshot-weekly", "snapshot-monthly");
    }

    [Fact]
    public async Task Retries_until_storage_accepts_the_write()
    {
        var manager = Substitute.For<IRecurringJobManager>();
        var attempts = 0;

        // Fails the first time, as an unreachable database would, then succeeds — the scenario
        // that used to require a manual restart before scheduled snapshots would ever run.
        manager
            .When(m => m.AddOrUpdate(
                Arg.Any<string>(), Arg.Any<Job>(), Arg.Any<string>(), Arg.Any<RecurringJobOptions>()))
            .Do(_ =>
            {
                // One throw covers the whole first pass: the service abandons an attempt on the
                // first failure, so only the first of the three AddOrUpdate calls is reached.
                if (Interlocked.Increment(ref attempts) == 1)
                    throw new InvalidOperationException("job storage unreachable");
            });

        var sut = CreateSut(manager);
        await sut.StartAsync(CancellationToken.None);

        // The first backoff is 5s, so this waits through a real delay rather than mocking time.
        await WaitForRegistrationAsync(manager, timeout: TimeSpan.FromSeconds(20));

        var registeredIds = manager.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IRecurringJobManager.AddOrUpdate))
            .Select(c => (string)c.GetArguments()[0]!)
            .Distinct()
            .ToList();

        registeredIds.Should().BeEquivalentTo("snapshot-daily", "snapshot-weekly", "snapshot-monthly");
    }

    [Fact]
    public async Task Stops_retrying_when_the_host_shuts_down()
    {
        var manager = Substitute.For<IRecurringJobManager>();
        manager
            .When(m => m.AddOrUpdate(
                Arg.Any<string>(), Arg.Any<Job>(), Arg.Any<string>(), Arg.Any<RecurringJobOptions>()))
            .Throw(new InvalidOperationException("job storage unreachable"));

        var sut = CreateSut(manager);
        await sut.StartAsync(CancellationToken.None);

        // StopAsync must return promptly — a service that swallowed the cancellation and kept
        // sleeping through its backoff would hang shutdown until the host's timeout killed it.
        var stop = sut.StopAsync(CancellationToken.None);
        var completed = await Task.WhenAny(stop, Task.Delay(TimeSpan.FromSeconds(10)));

        completed.Should().BeSameAs(stop, "shutdown should not wait out the retry backoff");
        sut.ExecuteTask!.IsCompleted.Should().BeTrue();
    }

    /// <summary>
    /// Polls rather than sleeping a fixed span: the service runs on its own task, so the only
    /// honest signal that it is done is the calls actually landing on the manager.
    /// </summary>
    private static async Task WaitForRegistrationAsync(
        IRecurringJobManager manager, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));

        while (DateTime.UtcNow < deadline)
        {
            var distinctIds = manager.ReceivedCalls()
                .Where(c => c.GetMethodInfo().Name == nameof(IRecurringJobManager.AddOrUpdate))
                .Select(c => (string)c.GetArguments()[0]!)
                .Distinct()
                .Count();

            if (distinctIds >= 3) return;

            await Task.Delay(50);
        }
    }
}
