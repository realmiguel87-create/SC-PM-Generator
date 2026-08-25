using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Dtos;
using SCPM.Application.Reporting.Queries.GetSnapshotIntervalActivity;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;
using SCPM.Infrastructure.Persistence;
using Xunit;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// The activity an endpoint comparison is structurally blind to.
///
/// These tests are mostly about what the query *doesn't* report. It sits alongside the item
/// comparison and would be actively harmful if it duplicated it: a list that repeats findings
/// already shown elsewhere looks longer and means less, and the reader stops trusting either.
/// So the interesting assertions here are the empty ones.
/// </summary>
public class SnapshotIntervalActivityTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid FromSnapshotId = Guid.NewGuid();
    private static readonly Guid ToSnapshotId = Guid.NewGuid();
    private static readonly DateTime FromAt = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ToAt = new(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);

    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.Snapshots.AddRange(
            new Snapshot { Id = FromSnapshotId, ProjectId = ProjectId, Label = "January", CapturedAt = FromAt, Type = SnapshotType.Monthly },
            new Snapshot { Id = ToSnapshotId, ProjectId = ProjectId, Label = "February", CapturedAt = ToAt, Type = SnapshotType.Monthly });
        db.SaveChanges();

        return db;
    }

    private static Risk Risk(Guid id, RiskStatus status, int probability, int impact, string title = "Ground conditions") => new()
    {
        Id = id,
        ProjectId = ProjectId,
        Title = title,
        Category = "Construction",
        Status = status,
        Probability = probability,
        Impact = impact,
    };

    private static async Task<SnapshotIntervalActivityDto> ActivityAsync(
        AppDbContext db,
        IReadOnlyList<Risk> atStart,
        IReadOnlyList<Risk> atEnd,
        IReadOnlyList<Risk> versionsInWindow)
    {
        var history = Substitute.For<IRegisterHistory>();
        history.RisksAsOfAsync(ProjectId, FromAt, Arg.Any<CancellationToken>()).Returns(atStart);
        history.RisksAsOfAsync(ProjectId, ToAt, Arg.Any<CancellationToken>()).Returns(atEnd);
        history.RiskVersionsBetweenAsync(ProjectId, FromAt, ToAt, Arg.Any<CancellationToken>()).Returns(versionsInWindow);

        var handler = new GetSnapshotIntervalActivityQueryHandler(db, history);
        return await handler.Handle(
            new GetSnapshotIntervalActivityQuery(FromSnapshotId, ToSnapshotId), CancellationToken.None);
    }

    [Fact]
    public async Task Finds_a_risk_raised_and_removed_inside_the_window()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await ActivityAsync(db,
            atStart: [],
            atEnd: [],
            versionsInWindow:
            [
                Risk(id, RiskStatus.Open, 4, 4, "Asbestos found in survey"),
                Risk(id, RiskStatus.Closed, 4, 4, "Asbestos found in survey"),
            ]);

        // The whole reason this query exists: absent at both endpoints, so no comparison of the
        // two could ever have reported it.
        var item = result.Items.Should().ContainSingle().Subject;
        item.ActivityType.Should().Be(IntervalActivityType.RaisedAndRemoved);
        item.Register.Should().Be("Risk");
        item.Name.Should().Be("Asbestos found in survey");
        item.VersionCount.Should().Be(2);
    }

    [Fact]
    public async Task Finds_a_risk_that_moved_and_moved_back()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();
        var settled = Risk(id, RiskStatus.Open, 2, 3);

        var result = await ActivityAsync(db,
            atStart: [settled],
            atEnd: [settled],
            versionsInWindow:
            [
                settled,
                Risk(id, RiskStatus.Escalated, 5, 5),  // spiked mid-period
                settled,                                // and came back down
            ]);

        // Identical at both endpoints, so the comparison correctly reports no change — and the
        // escalation still happened, which a committee reading a month-on-month diff would
        // otherwise never learn about.
        var item = result.Items.Should().ContainSingle().Subject;
        item.ActivityType.Should().Be(IntervalActivityType.ChangedAndReverted);
        item.VersionCount.Should().Be(3);
    }

    [Fact]
    public async Task Ignores_a_risk_the_endpoint_comparison_already_reports()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await ActivityAsync(db,
            atStart: [Risk(id, RiskStatus.Open, 2, 3)],
            atEnd: [Risk(id, RiskStatus.Open, 5, 5)],
            versionsInWindow:
            [
                Risk(id, RiskStatus.Open, 2, 3),
                Risk(id, RiskStatus.Open, 5, 5),
            ]);

        // Duplicating the comparison would make this list look longer and mean less.
        result.Items.Should().BeEmpty();
        result.HasActivity.Should().BeFalse();
    }

    [Fact]
    public async Task Ignores_an_item_added_within_the_window_and_still_present_at_the_end()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await ActivityAsync(db,
            atStart: [],
            atEnd: [Risk(id, RiskStatus.Open, 3, 3)],
            versionsInWindow: [Risk(id, RiskStatus.Open, 3, 3)]);

        // Present at one endpoint, so the comparison reports it as Added. Not this query's job.
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Ignores_a_risk_that_did_not_change_at_all()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();
        var unchanged = Risk(id, RiskStatus.Open, 3, 3);

        var result = await ActivityAsync(db,
            atStart: [unchanged],
            atEnd: [unchanged],
            versionsInWindow: [unchanged]);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Ignores_a_title_only_edit_that_reverted()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();
        var original = Risk(id, RiskStatus.Open, 3, 3, "Ground conditions");

        var result = await ActivityAsync(db,
            atStart: [original],
            atEnd: [original],
            versionsInWindow:
            [
                original,
                Risk(id, RiskStatus.Open, 3, 3, "Ground conditions (draft wording)"),
                original,
            ]);

        // Consistency with the comparison matters more than completeness here: both use
        // RegisterChangeRules, so a rename is housekeeping in both, and neither reports it.
        // If this query used its own definition the two views would disagree.
        result.Items.Should().BeEmpty();
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

        var handler = new GetSnapshotIntervalActivityQueryHandler(db, Substitute.For<IRegisterHistory>());

        var act = () => handler.Handle(
            new GetSnapshotIntervalActivityQuery(FromSnapshotId, otherId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*different projects*");
    }
}
