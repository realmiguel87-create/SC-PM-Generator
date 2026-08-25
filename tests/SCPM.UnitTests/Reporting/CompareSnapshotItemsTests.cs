using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Application.Reporting.Queries.CompareSnapshotItems;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;
using SCPM.Infrastructure.Persistence;
using Xunit;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// The diffing logic, with register history supplied by a substitute.
///
/// Splitting it this way is the point of IRegisterHistory: the temporal query and the diff are
/// two different risks. Whether `FOR SYSTEM_TIME AS OF` returns the right rows can only honestly
/// be tested against a real SQL Server (see SCPM.IntegrationTests' RegisterHistoryTests); whether
/// two lists of rows diff correctly needs no database at all, and testing it here means the edge
/// cases below can be written out explicitly instead of contrived through timing.
/// </summary>
public class CompareSnapshotItemsTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid FromSnapshotId = Guid.NewGuid();
    private static readonly Guid ToSnapshotId = Guid.NewGuid();
    private static readonly DateTime FromAt = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ToAt = new(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Baseline = new(2026, 6, 1);

    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.Snapshots.AddRange(
            new Snapshot { Id = FromSnapshotId, ProjectId = ProjectId, Label = "Q1 baseline", CapturedAt = FromAt, Type = SnapshotType.Manual },
            new Snapshot { Id = ToSnapshotId, ProjectId = ProjectId, Label = "Pre-committee", CapturedAt = ToAt, Type = SnapshotType.Manual });
        db.SaveChanges();

        return db;
    }

    private static Risk Risk(Guid id, string title, RiskStatus status, int probability, int impact) => new()
    {
        Id = id,
        ProjectId = ProjectId,
        Title = title,
        Category = "Cost",
        Status = status,
        Probability = probability,
        Impact = impact,
    };

    private static Milestone Milestone(Guid id, string name, DateOnly forecast,
        DateOnly? actual = null, MilestoneStatus status = MilestoneStatus.InProgress) => new()
    {
        Id = id,
        ProjectId = ProjectId,
        Name = name,
        Status = status,
        BaselineDate = Baseline,
        ForecastDate = forecast,
        ActualDate = actual,
    };

    private static async Task<SnapshotItemComparisonDto> CompareAsync(
        AppDbContext db,
        IReadOnlyList<Risk>? risksFrom = null,
        IReadOnlyList<Risk>? risksTo = null,
        IReadOnlyList<Milestone>? milestonesFrom = null,
        IReadOnlyList<Milestone>? milestonesTo = null)
    {
        var history = Substitute.For<IRegisterHistory>();
        history.RisksAsOfAsync(ProjectId, FromAt, Arg.Any<CancellationToken>()).Returns(risksFrom ?? []);
        history.RisksAsOfAsync(ProjectId, ToAt, Arg.Any<CancellationToken>()).Returns(risksTo ?? []);
        history.MilestonesAsOfAsync(ProjectId, FromAt, Arg.Any<CancellationToken>()).Returns(milestonesFrom ?? []);
        history.MilestonesAsOfAsync(ProjectId, ToAt, Arg.Any<CancellationToken>()).Returns(milestonesTo ?? []);

        var handler = new CompareSnapshotItemsQueryHandler(db, history);
        return await handler.Handle(new CompareSnapshotItemsQuery(FromSnapshotId, ToSnapshotId), CancellationToken.None);
    }

    [Fact]
    public async Task Reports_nothing_when_the_registers_are_identical()
    {
        using var db = NewContext();
        var risk = Risk(Guid.NewGuid(), "Ground conditions", RiskStatus.Open, 3, 4);
        var milestone = Milestone(Guid.NewGuid(), "Planning consent", Baseline);

        var result = await CompareAsync(db, [risk], [risk], [milestone], [milestone]);

        // An unchanged register produces empty lists, not every row marked unchanged. A diff
        // nobody can skim is a diff nobody reads.
        result.RiskChanges.Should().BeEmpty();
        result.MilestoneChanges.Should().BeEmpty();
        result.HasChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Distinguishes_a_risk_that_appeared_from_one_that_worsened()
    {
        using var db = NewContext();
        var existingId = Guid.NewGuid();
        var newId = Guid.NewGuid();

        var result = await CompareAsync(db,
            risksFrom: [Risk(existingId, "Ground conditions", RiskStatus.Open, 2, 3)],
            risksTo:
            [
                Risk(existingId, "Ground conditions", RiskStatus.Open, 4, 5),
                Risk(newId, "Contractor insolvency", RiskStatus.Open, 3, 5),
            ]);

        var worsened = result.RiskChanges.Single(c => c.RiskId == existingId);
        worsened.ChangeType.Should().Be(ItemChangeType.Modified);
        worsened.FromScore.Should().Be(6);
        worsened.ToScore.Should().Be(20);
        worsened.ScoreDelta.Should().Be(14);

        var added = result.RiskChanges.Single(c => c.RiskId == newId);
        added.ChangeType.Should().Be(ItemChangeType.Added);
        added.FromScore.Should().BeNull();
        added.ToScore.Should().Be(15);
        // No delta for something that was not there to move — "appeared at 15" is not "rose by 15".
        added.ScoreDelta.Should().BeNull();
    }

    [Fact]
    public async Task Reports_a_risk_present_only_at_the_earlier_point_as_removed()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await CompareAsync(db,
            risksFrom: [Risk(id, "Planning objection", RiskStatus.Open, 4, 4)],
            risksTo: []);

        var removed = result.RiskChanges.Single();
        removed.ChangeType.Should().Be(ItemChangeType.Removed);
        // The title survives from the earlier row: a removed item has no current row to name it,
        // and an unnamed row in a committee paper is worse than a slightly stale name.
        removed.Title.Should().Be("Planning objection");
        removed.FromScore.Should().Be(16);
        removed.ToScore.Should().BeNull();
    }

    [Fact]
    public async Task Treats_a_closed_risk_as_a_change_rather_than_a_removal()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await CompareAsync(db,
            risksFrom: [Risk(id, "Ground conditions", RiskStatus.Open, 3, 4)],
            risksTo: [Risk(id, "Ground conditions", RiskStatus.Closed, 3, 4)]);

        // Closing a risk and deleting it are different events and must not read the same. The row
        // is still there, so this is Modified with a status transition — the version a reader can
        // act on, since a closure is a result and a deletion is a correction.
        var change = result.RiskChanges.Single();
        change.ChangeType.Should().Be(ItemChangeType.Modified);
        change.FromStatus.Should().Be("Open");
        change.ToStatus.Should().Be("Closed");
    }

    [Fact]
    public async Task Ignores_a_title_only_edit()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await CompareAsync(db,
            risksFrom: [Risk(id, "Ground conditions", RiskStatus.Open, 3, 4)],
            risksTo: [Risk(id, "Ground conditions (revised wording)", RiskStatus.Open, 3, 4)]);

        // Renaming a risk is housekeeping. Listing it beside real movements dilutes the ones
        // that matter, which is the failure mode of every diff that reports everything.
        result.RiskChanges.Should().BeEmpty();
    }

    [Fact]
    public async Task Orders_risks_by_the_size_of_the_movement()
    {
        using var db = NewContext();
        var small = Guid.NewGuid();
        var large = Guid.NewGuid();

        var result = await CompareAsync(db,
            risksFrom:
            [
                Risk(small, "Small mover", RiskStatus.Open, 2, 2),
                Risk(large, "Large mover", RiskStatus.Open, 1, 2),
            ],
            risksTo:
            [
                Risk(small, "Small mover", RiskStatus.Open, 2, 3),   // 4 -> 6, delta 2
                Risk(large, "Large mover", RiskStatus.Open, 5, 4),   // 2 -> 20, delta 18
            ]);

        result.RiskChanges.Select(c => c.RiskId).Should().ContainInOrder(large, small);
    }

    [Fact]
    public async Task Reports_a_milestone_slip_in_days_against_baseline()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await CompareAsync(db,
            milestonesFrom: [Milestone(id, "Practical completion", Baseline.AddDays(10))],
            milestonesTo: [Milestone(id, "Practical completion", Baseline.AddDays(45))]);

        var change = result.MilestoneChanges.Single();
        change.ChangeType.Should().Be(ItemChangeType.Modified);
        change.FromDelayDays.Should().Be(10);
        change.ToDelayDays.Should().Be(45);
        change.DelayDaysDelta.Should().Be(35);
    }

    [Fact]
    public async Task Uses_the_actual_date_once_a_milestone_completes()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await CompareAsync(db,
            milestonesFrom: [Milestone(id, "Planning consent", Baseline.AddDays(60))],
            milestonesTo:
            [
                Milestone(id, "Planning consent", Baseline.AddDays(60), Baseline.AddDays(12), MilestoneStatus.Complete),
            ]);

        var change = result.MilestoneChanges.Single();
        change.FromDelayDays.Should().Be(60);
        // Completed 12 days late, so the 60-day forecast is history — recovery, not a slip.
        change.ToDelayDays.Should().Be(12);
        change.DelayDaysDelta.Should().Be(-48);
    }

    [Fact]
    public async Task Refuses_to_compare_snapshots_from_different_projects()
    {
        using var db = NewContext();
        var otherId = Guid.NewGuid();
        db.Snapshots.Add(new Snapshot
        {
            Id = otherId,
            ProjectId = Guid.NewGuid(),
            Label = "Another project",
            CapturedAt = ToAt,
            Type = SnapshotType.Manual,
        });
        await db.SaveChangesAsync();

        var history = Substitute.For<IRegisterHistory>();
        var handler = new CompareSnapshotItemsQueryHandler(db, history);

        var act = () => handler.Handle(new CompareSnapshotItemsQuery(FromSnapshotId, otherId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*different projects*");
    }
}
