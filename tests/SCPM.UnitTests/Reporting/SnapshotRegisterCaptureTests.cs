using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Commands.CreateSnapshot;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;
using SCPM.Infrastructure.Persistence;
using Xunit;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// Exercises the register aggregates a Snapshot captures, against a real DbContext (EF Core
/// InMemory provider) so the LINQ in the handler actually runs rather than being stubbed.
///
/// The point of these tests is the *definitions* — what counts as an open risk, whether an
/// implemented compensation event still carries value, whether a milestone nobody has marked
/// Delayed is nonetheless late. Those are the decisions a future change is most likely to break
/// silently, because every one of them still produces a plausible-looking number when wrong.
/// </summary>
public class SnapshotRegisterCaptureTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly DateOnly Baseline = new(2026, 6, 1);

    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.Projects.Add(new Project
        {
            Id = ProjectId,
            ProjectRef = "PRJ-0001",
            Name = "Stirling Community Campus",
            CurrentRibaStage = 3,
            ApprovedBudget = 25_000_000m,
            ForecastCost = 26_500_000m
        });
        db.SaveChanges();

        return db;
    }

    private static async Task<Snapshot> CaptureAsync(AppDbContext db)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new CreateSnapshotCommandHandler(db, currentUser);
        var id = await handler.Handle(
            new CreateSnapshotCommand(ProjectId, SnapshotType.Manual, "Test capture"), CancellationToken.None);

        return await db.Snapshots.SingleAsync(s => s.Id == id);
    }

    private static Risk RiskWith(RiskStatus status, int probability, int impact) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = ProjectId,
        Title = "Risk",
        Category = "Cost",
        Status = status,
        Probability = probability,
        Impact = impact
    };

    private static Milestone MilestoneWith(DateOnly forecast, DateOnly? actual = null,
        MilestoneStatus status = MilestoneStatus.InProgress) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = ProjectId,
        Name = "Milestone",
        Status = status,
        BaselineDate = Baseline,
        ForecastDate = forecast,
        ActualDate = actual
    };

    [Fact]
    public async Task Captures_zeroes_for_a_project_with_empty_registers()
    {
        using var db = NewContext();

        var snapshot = await CaptureAsync(db);

        // A project with nothing on its registers must record zeroes, not nulls or a crash —
        // Max over an empty milestone set is the specific trap here.
        snapshot.OpenRiskCount.Should().Be(0);
        snapshot.TotalOpenRiskScore.Should().Be(0);
        snapshot.MilestoneCount.Should().Be(0);
        snapshot.WorstMilestoneDelayDays.Should().Be(0);
        snapshot.CompensationEventValue.Should().Be(0m);
        snapshot.ExtensionOfTimeDaysAwarded.Should().Be(0);
    }

    [Fact]
    public async Task Counts_escalated_risks_as_open_and_excludes_mitigated_and_closed()
    {
        using var db = NewContext();
        db.Risks.AddRange(
            RiskWith(RiskStatus.Open, 5, 4),        // score 20 — open, high
            RiskWith(RiskStatus.Escalated, 3, 5),   // score 15 — open, high (at the threshold)
            RiskWith(RiskStatus.Open, 2, 3),        // score 6  — open, not high
            RiskWith(RiskStatus.Mitigated, 5, 5),   // excluded
            RiskWith(RiskStatus.Closed, 5, 5));     // excluded
        await db.SaveChangesAsync();

        var snapshot = await CaptureAsync(db);

        snapshot.OpenRiskCount.Should().Be(3);
        // 15 is at the threshold, not above it — an off-by-one here would read as 1.
        snapshot.HighRiskCount.Should().Be(2);
        snapshot.TotalOpenRiskScore.Should().Be(41, "20 + 15 + 6, excluding the mitigated and closed risks");
    }

    [Fact]
    public async Task Excludes_soft_deleted_rows_via_the_global_query_filter()
    {
        using var db = NewContext();
        var deleted = RiskWith(RiskStatus.Open, 5, 5);
        deleted.IsDeleted = true;
        db.Risks.AddRange(RiskWith(RiskStatus.Open, 2, 2), deleted);
        await db.SaveChangesAsync();

        var snapshot = await CaptureAsync(db);

        // The handler never mentions IsDeleted; this passes because of the global query filter,
        // and would start failing the moment a future query bypassed it with IgnoreQueryFilters.
        snapshot.OpenRiskCount.Should().Be(1);
        snapshot.TotalOpenRiskScore.Should().Be(4);
    }

    [Fact]
    public async Task Treats_a_slipped_milestone_as_delayed_regardless_of_its_status()
    {
        using var db = NewContext();
        db.Milestones.AddRange(
            // Slipped by 30 days but nobody has set the status to Delayed.
            MilestoneWith(Baseline.AddDays(30), status: MilestoneStatus.InProgress),
            // Completed 5 days late — actual date wins over forecast.
            MilestoneWith(Baseline.AddDays(90), Baseline.AddDays(5), MilestoneStatus.Complete),
            // Ahead of baseline: not delayed, and must not drag the worst-delay figure negative.
            MilestoneWith(Baseline.AddDays(-10)));
        await db.SaveChangesAsync();

        var snapshot = await CaptureAsync(db);

        snapshot.MilestoneCount.Should().Be(3);
        snapshot.MilestonesCompleteCount.Should().Be(1);
        snapshot.MilestonesDelayedCount.Should().Be(2);
        snapshot.WorstMilestoneDelayDays.Should().Be(30,
            "the completed milestone's actual date puts it 5 days late, not the 90 its forecast implied");
    }

    [Fact]
    public async Task Reports_zero_worst_delay_when_every_milestone_is_ahead_of_baseline()
    {
        using var db = NewContext();
        db.Milestones.AddRange(
            MilestoneWith(Baseline.AddDays(-10)),
            MilestoneWith(Baseline.AddDays(-3)));
        await db.SaveChangesAsync();

        var snapshot = await CaptureAsync(db);

        snapshot.MilestonesDelayedCount.Should().Be(0);
        // Without the clamp this would be -3, which reads as a delay when it is the opposite.
        snapshot.WorstMilestoneDelayDays.Should().Be(0);
    }

    [Fact]
    public async Task Counts_open_compensation_events_separately_from_the_value_carried()
    {
        using var db = NewContext();
        db.CompensationEvents.AddRange(
            new CompensationEvent { Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "CE-001", Title = "Ground conditions", Status = CompensationEventStatus.Notified, EstimatedValue = 100_000m },
            new CompensationEvent { Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "CE-002", Title = "Design change", Status = CompensationEventStatus.Accepted, EstimatedValue = 50_000m },
            new CompensationEvent { Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "CE-003", Title = "Implemented", Status = CompensationEventStatus.Implemented, EstimatedValue = 25_000m },
            new CompensationEvent { Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "CE-004", Title = "Rejected", Status = CompensationEventStatus.Rejected, EstimatedValue = 999_000m });
        await db.SaveChangesAsync();

        var snapshot = await CaptureAsync(db);

        snapshot.OpenCompensationEventCount.Should().Be(2, "implemented and rejected events are both concluded");
        // The two definitions differ deliberately: an implemented CE is settled but still costs
        // money, so it counts toward value while not counting as open. Only a rejected one is free.
        snapshot.CompensationEventValue.Should().Be(175_000m);
    }

    [Fact]
    public async Task Sums_only_awarded_extension_of_time_days()
    {
        using var db = NewContext();
        db.ExtensionsOfTime.AddRange(
            new ExtensionOfTime { Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "EOT-001", Reason = "Weather", DaysClaimed = 40, DaysAwarded = 21, Status = ExtensionOfTimeStatus.Awarded },
            // Claimed but undetermined — 60 days that are not yet a programme fact.
            new ExtensionOfTime { Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "EOT-002", Reason = "Access", DaysClaimed = 60, DaysAwarded = null, Status = ExtensionOfTimeStatus.UnderReview });
        await db.SaveChangesAsync();

        var snapshot = await CaptureAsync(db);

        snapshot.ExtensionOfTimeDaysAwarded.Should().Be(21);
    }

    [Fact]
    public async Task Ignores_registers_belonging_to_other_projects()
    {
        using var db = NewContext();
        var otherProjectId = Guid.NewGuid();
        db.Projects.Add(new Project { Id = otherProjectId, ProjectRef = "PRJ-0002", Name = "Bridge Refurbishment" });

        var ours = RiskWith(RiskStatus.Open, 2, 2);
        var theirs = RiskWith(RiskStatus.Open, 5, 5);
        theirs.ProjectId = otherProjectId;
        db.Risks.AddRange(ours, theirs);
        await db.SaveChangesAsync();

        var snapshot = await CaptureAsync(db);

        snapshot.OpenRiskCount.Should().Be(1);
        snapshot.TotalOpenRiskScore.Should().Be(4);
    }
}
