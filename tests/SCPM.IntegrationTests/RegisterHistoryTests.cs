using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;
using SCPM.Infrastructure.Persistence;
using Xunit;

namespace SCPM.IntegrationTests;

/// <summary>
/// Exercises SqlServerRegisterHistory against real SQL Server temporal tables.
///
/// This is the half of item-level snapshot comparison that cannot be tested any other way. The
/// diffing logic is covered by unit tests with a substituted IRegisterHistory; whether
/// `FOR SYSTEM_TIME AS OF` actually returns the row as it stood at a past instant depends on SQL
/// Server's own versioning behaviour, on the tables genuinely being system-versioned, and on EF
/// Core's global query filters being applied to the *historical* row rather than the current one.
/// A fake proves none of that.
///
/// Every timestamp here comes from SYSUTCDATETIME() on the server, never from the test host's
/// clock. Temporal period columns are populated by SQL Server, so a client-side DateTime.UtcNow
/// would be comparing against a different clock — and any skew between the two would make these
/// tests fail, or worse, pass for the wrong reason.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class RegisterHistoryTests : IAsyncLifetime
{
    private readonly ScpmWebApplicationFactory _factory;
    private Guid _projectId;

    public RegisterHistoryTests(ScpmWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        _projectId = Guid.NewGuid();
        await WithDbAsync(async db =>
        {
            db.Projects.Add(new Project
            {
                Id = _projectId,
                ProjectRef = "HIST-001",
                Name = "Temporal history test project",
                ApprovedBudget = 1_000_000m,
            });
            await db.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private async Task<T> WithHistoryAsync<T>(Func<IRegisterHistory, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<IRegisterHistory>());
    }

    /// <summary>The database's own clock — see the class comment for why this matters.</summary>
    private async Task<DateTime> ServerUtcNowAsync()
    {
        DateTime now = default;
        await WithDbAsync(async db =>
        {
            now = await db.Database
                .SqlQuery<DateTime>($"SELECT SYSUTCDATETIME() AS Value")
                .SingleAsync();
        });
        return now;
    }

    /// <summary>
    /// Temporal period columns are datetime2, so two writes in the same tick would share a
    /// version boundary and "as of between them" would be meaningless. A short real delay keeps
    /// the versions distinct without depending on how fast the machine happens to be.
    /// </summary>
    private static Task SeparateVersionsAsync() => Task.Delay(50);

    [Fact]
    public async Task Returns_a_risk_as_it_stood_before_it_was_re_scored()
    {
        var riskId = Guid.NewGuid();

        await WithDbAsync(async db =>
        {
            db.Risks.Add(new Risk
            {
                Id = riskId,
                ProjectId = _projectId,
                Title = "Ground conditions",
                Category = "Construction",
                Status = RiskStatus.Open,
                Probability = 2,
                Impact = 3,
            });
            await db.SaveChangesAsync();
        });

        await SeparateVersionsAsync();
        var beforeRescore = await ServerUtcNowAsync();
        await SeparateVersionsAsync();

        await WithDbAsync(async db =>
        {
            var risk = await db.Risks.SingleAsync(r => r.Id == riskId);
            risk.Probability = 5;
            risk.Impact = 4;
            await db.SaveChangesAsync();
        });

        await SeparateVersionsAsync();
        var afterRescore = await ServerUtcNowAsync();

        var before = await WithHistoryAsync(h => h.RisksAsOfAsync(_projectId, beforeRescore, CancellationToken.None));
        var after = await WithHistoryAsync(h => h.RisksAsOfAsync(_projectId, afterRescore, CancellationToken.None));

        before.Single().Score.Should().Be(6, "the risk scored 2 x 3 at that point");
        after.Single().Score.Should().Be(20, "and 5 x 4 after the re-score");
    }

    [Fact]
    public async Task Does_not_return_a_risk_that_did_not_exist_yet()
    {
        var beforeAnyRisk = await ServerUtcNowAsync();
        await SeparateVersionsAsync();

        await WithDbAsync(async db =>
        {
            db.Risks.Add(new Risk
            {
                Id = Guid.NewGuid(),
                ProjectId = _projectId,
                Title = "Contractor insolvency",
                Category = "Commercial",
                Status = RiskStatus.Open,
                Probability = 3,
                Impact = 5,
            });
            await db.SaveChangesAsync();
        });

        var atThatTime = await WithHistoryAsync(
            h => h.RisksAsOfAsync(_projectId, beforeAnyRisk, CancellationToken.None));

        // This is what makes an Added change genuinely detectable rather than inferred.
        atThatTime.Should().BeEmpty();
    }

    [Fact]
    public async Task Still_returns_a_risk_that_was_soft_deleted_later()
    {
        var riskId = Guid.NewGuid();

        await WithDbAsync(async db =>
        {
            db.Risks.Add(new Risk
            {
                Id = riskId,
                ProjectId = _projectId,
                Title = "Raised in error",
                Category = "Cost",
                Status = RiskStatus.Open,
                Probability = 1,
                Impact = 1,
            });
            await db.SaveChangesAsync();
        });

        await SeparateVersionsAsync();
        var whileLive = await ServerUtcNowAsync();
        await SeparateVersionsAsync();

        await WithDbAsync(async db =>
        {
            var risk = await db.Risks.SingleAsync(r => r.Id == riskId);
            risk.IsDeleted = true;
            await db.SaveChangesAsync();
        });

        await SeparateVersionsAsync();
        var afterDeletion = await ServerUtcNowAsync();

        var before = await WithHistoryAsync(h => h.RisksAsOfAsync(_projectId, whileLive, CancellationToken.None));
        var after = await WithHistoryAsync(h => h.RisksAsOfAsync(_projectId, afterDeletion, CancellationToken.None));

        // The claim being tested: EF Core's global soft-delete filter is applied to the historical
        // row's own IsDeleted value, not to the current one. A risk deleted last week was not
        // deleted a month ago, and a month-old snapshot must see it exactly as it saw it then.
        // If the filter used the live row instead, this first assertion would find nothing.
        before.Should().ContainSingle().Which.Id.Should().Be(riskId);
        after.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_a_compensation_event_at_its_earlier_estimate()
    {
        var eventId = Guid.NewGuid();

        await WithDbAsync(async db =>
        {
            db.CompensationEvents.Add(new CompensationEvent
            {
                Id = eventId,
                ProjectId = _projectId,
                Reference = "CE-001",
                Title = "Ground conditions",
                Status = CompensationEventStatus.Notified,
                EstimatedValue = 120_000m,
                NotifiedDate = new DateOnly(2026, 1, 20),
            });
            await db.SaveChangesAsync();
        });

        await SeparateVersionsAsync();
        var beforeReEstimate = await ServerUtcNowAsync();
        await SeparateVersionsAsync();

        await WithDbAsync(async db =>
        {
            var compensationEvent = await db.CompensationEvents.SingleAsync(c => c.Id == eventId);
            compensationEvent.Status = CompensationEventStatus.Quoted;
            compensationEvent.EstimatedValue = 310_000m;
            await db.SaveChangesAsync();
        });

        var before = await WithHistoryAsync(
            h => h.CompensationEventsAsOfAsync(_projectId, beforeReEstimate, CancellationToken.None));

        // Confirms the NEC4 tables are genuinely system-versioned too, not only Risk and
        // Milestone — the IsTemporal configuration is per-entity, so covering one entity proves
        // nothing about the others.
        var captured = before.Should().ContainSingle().Subject;
        captured.EstimatedValue.Should().Be(120_000m);
        captured.Status.Should().Be(CompensationEventStatus.Notified);
    }

    [Fact]
    public async Task Finds_versions_of_a_risk_raised_and_removed_between_two_points()
    {
        var start = await ServerUtcNowAsync();
        await SeparateVersionsAsync();

        var riskId = Guid.NewGuid();

        await WithDbAsync(async db =>
        {
            db.Risks.Add(new Risk
            {
                Id = riskId,
                ProjectId = _projectId,
                Title = "Asbestos found in survey",
                Category = "Construction",
                Status = RiskStatus.Open,
                Probability = 4,
                Impact = 4,
            });
            await db.SaveChangesAsync();
        });

        await SeparateVersionsAsync();

        await WithDbAsync(async db =>
        {
            var risk = await db.Risks.SingleAsync(r => r.Id == riskId);
            risk.Status = RiskStatus.Closed;
            await db.SaveChangesAsync();
        });

        await SeparateVersionsAsync();

        await WithDbAsync(async db =>
        {
            var risk = await db.Risks.SingleAsync(r => r.Id == riskId);
            risk.IsDeleted = true;
            await db.SaveChangesAsync();
        });

        await SeparateVersionsAsync();
        var end = await ServerUtcNowAsync();

        var atStart = await WithHistoryAsync(h => h.RisksAsOfAsync(_projectId, start, CancellationToken.None));
        var atEnd = await WithHistoryAsync(h => h.RisksAsOfAsync(_projectId, end, CancellationToken.None));
        var versions = await WithHistoryAsync(h => h.RiskVersionsBetweenAsync(_projectId, start, end, CancellationToken.None));

        // The entire premise of interval activity: invisible at both endpoints, present in the
        // window. If BETWEEN behaved like two AS OF reads this would find nothing.
        atStart.Should().BeEmpty();
        atEnd.Should().BeEmpty();

        var riskVersions = versions.Where(r => r.Id == riskId).ToList();
        riskVersions.Should().HaveCountGreaterThanOrEqualTo(2,
            "the risk was open, then closed, before being removed");
        riskVersions.Select(r => r.Status).Should().Contain([RiskStatus.Open, RiskStatus.Closed]);
    }

    [Fact]
    public async Task Orders_the_window_before_querying_so_a_reversed_range_still_works()
    {
        var start = await ServerUtcNowAsync();
        await SeparateVersionsAsync();

        await WithDbAsync(async db =>
        {
            db.Risks.Add(new Risk
            {
                Id = Guid.NewGuid(),
                ProjectId = _projectId,
                Title = "Reversed range",
                Category = "Cost",
                Status = RiskStatus.Open,
                Probability = 2,
                Impact = 2,
            });
            await db.SaveChangesAsync();
        });

        await SeparateVersionsAsync();
        var end = await ServerUtcNowAsync();

        // SQL Server rejects a BETWEEN whose start is after its end. Comparing a later snapshot
        // against an earlier one is a legitimate thing to ask, so the range is ordered before it
        // reaches the database rather than failing.
        var reversed = await WithHistoryAsync(
            h => h.RiskVersionsBetweenAsync(_projectId, end, start, CancellationToken.None));

        reversed.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Returns_a_milestone_with_its_forecast_date_at_that_time()
    {
        var milestoneId = Guid.NewGuid();
        var baseline = new DateOnly(2026, 6, 1);

        await WithDbAsync(async db =>
        {
            db.Milestones.Add(new Milestone
            {
                Id = milestoneId,
                ProjectId = _projectId,
                Name = "Practical completion",
                Status = MilestoneStatus.InProgress,
                BaselineDate = baseline,
                ForecastDate = baseline.AddDays(10),
            });
            await db.SaveChangesAsync();
        });

        await SeparateVersionsAsync();
        var beforeSlip = await ServerUtcNowAsync();
        await SeparateVersionsAsync();

        await WithDbAsync(async db =>
        {
            var milestone = await db.Milestones.SingleAsync(m => m.Id == milestoneId);
            milestone.ForecastDate = baseline.AddDays(75);
            await db.SaveChangesAsync();
        });

        var before = await WithHistoryAsync(
            h => h.MilestonesAsOfAsync(_projectId, beforeSlip, CancellationToken.None));

        before.Single().DelayDays.Should().Be(10, "the forecast had only slipped 10 days at that point");
    }
}
