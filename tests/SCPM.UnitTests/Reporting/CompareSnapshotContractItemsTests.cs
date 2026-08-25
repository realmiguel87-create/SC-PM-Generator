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
/// Item-level diffing of the NEC4 and SBCC registers — early warnings, compensation events,
/// variations, extensions of time.
///
/// The mechanism was already proven for risks and milestones; what is genuinely new here is one
/// judgement per register about which fields count as a reportable change. Those judgements are
/// what these tests are for. Each is a decision that produces a plausible-looking result when
/// wrong: reporting too much buries the movements that matter, reporting too little loses them.
/// </summary>
public class CompareSnapshotContractItemsTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid FromSnapshotId = Guid.NewGuid();
    private static readonly Guid ToSnapshotId = Guid.NewGuid();
    private static readonly DateTime FromAt = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ToAt = new(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);

    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.Snapshots.AddRange(
            new Snapshot { Id = FromSnapshotId, ProjectId = ProjectId, Label = "Q1", CapturedAt = FromAt, Type = SnapshotType.Manual },
            new Snapshot { Id = ToSnapshotId, ProjectId = ProjectId, Label = "Q2", CapturedAt = ToAt, Type = SnapshotType.Manual });
        db.SaveChanges();

        return db;
    }

    private sealed class Registers
    {
        public List<EarlyWarning> EarlyWarnings { get; init; } = [];
        public List<CompensationEvent> CompensationEvents { get; init; } = [];
        public List<Variation> Variations { get; init; } = [];
        public List<ExtensionOfTime> ExtensionsOfTime { get; init; } = [];
    }

    private static async Task<SnapshotItemComparisonDto> CompareAsync(
        AppDbContext db, Registers before, Registers after)
    {
        var history = Substitute.For<IRegisterHistory>();
        history.RisksAsOfAsync(ProjectId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);
        history.MilestonesAsOfAsync(ProjectId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);

        history.EarlyWarningsAsOfAsync(ProjectId, FromAt, Arg.Any<CancellationToken>()).Returns(before.EarlyWarnings);
        history.EarlyWarningsAsOfAsync(ProjectId, ToAt, Arg.Any<CancellationToken>()).Returns(after.EarlyWarnings);
        history.CompensationEventsAsOfAsync(ProjectId, FromAt, Arg.Any<CancellationToken>()).Returns(before.CompensationEvents);
        history.CompensationEventsAsOfAsync(ProjectId, ToAt, Arg.Any<CancellationToken>()).Returns(after.CompensationEvents);
        history.VariationsAsOfAsync(ProjectId, FromAt, Arg.Any<CancellationToken>()).Returns(before.Variations);
        history.VariationsAsOfAsync(ProjectId, ToAt, Arg.Any<CancellationToken>()).Returns(after.Variations);
        history.ExtensionsOfTimeAsOfAsync(ProjectId, FromAt, Arg.Any<CancellationToken>()).Returns(before.ExtensionsOfTime);
        history.ExtensionsOfTimeAsOfAsync(ProjectId, ToAt, Arg.Any<CancellationToken>()).Returns(after.ExtensionsOfTime);

        var handler = new CompareSnapshotItemsQueryHandler(db, history);
        return await handler.Handle(new CompareSnapshotItemsQuery(FromSnapshotId, ToSnapshotId), CancellationToken.None);
    }

    private static EarlyWarning Warning(Guid id, string title, Nec4RegisterStatus status, string? action = null) => new()
    {
        Id = id,
        ProjectId = ProjectId,
        Title = title,
        Status = status,
        MitigationAction = action,
        RaisedDate = new DateOnly(2026, 1, 15),
    };

    private static CompensationEvent Event(Guid id, string reference, CompensationEventStatus status, decimal value) => new()
    {
        Id = id,
        ProjectId = ProjectId,
        Reference = reference,
        Title = "Ground conditions",
        Status = status,
        EstimatedValue = value,
        NotifiedDate = new DateOnly(2026, 1, 20),
    };

    private static Variation Change(Guid id, string reference, VariationStatus status, decimal value) => new()
    {
        Id = id,
        ProjectId = ProjectId,
        Reference = reference,
        Description = "Additional cladding",
        Status = status,
        ValueImpact = value,
    };

    private static ExtensionOfTime Extension(
        Guid id, string reference, ExtensionOfTimeStatus status, int claimed, int? awarded) => new()
    {
        Id = id,
        ProjectId = ProjectId,
        Reference = reference,
        Reason = "Exceptional weather",
        Status = status,
        DaysClaimed = claimed,
        DaysAwarded = awarded,
    };

    [Fact]
    public async Task Reports_an_early_warning_being_closed()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await CompareAsync(db,
            new Registers { EarlyWarnings = [Warning(id, "Utilities diversion", Nec4RegisterStatus.Open)] },
            new Registers { EarlyWarnings = [Warning(id, "Utilities diversion", Nec4RegisterStatus.Closed)] });

        var change = result.EarlyWarningChanges.Should().ContainSingle().Subject;
        change.ChangeType.Should().Be(ItemChangeType.Modified);
        change.FromStatus.Should().Be("Open");
        change.ToStatus.Should().Be("Closed");
    }

    [Fact]
    public async Task Ignores_an_early_warning_whose_only_change_is_its_mitigation_text()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await CompareAsync(db,
            new Registers { EarlyWarnings = [Warning(id, "Utilities diversion", Nec4RegisterStatus.Open, "Awaiting SP Energy response")] },
            new Registers { EarlyWarnings = [Warning(id, "Utilities diversion", Nec4RegisterStatus.Open, "SP Energy site visit booked for 12 March")] });

        // Mitigation text changes every time the team works the problem. Reporting each wording
        // change would drown the two transitions — raised, closed — that actually matter.
        result.EarlyWarningChanges.Should().BeEmpty();
    }

    [Fact]
    public async Task Reports_a_compensation_event_re_estimated_without_a_status_change()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await CompareAsync(db,
            new Registers { CompensationEvents = [Event(id, "CE-001", CompensationEventStatus.Quoted, 120_000m)] },
            new Registers { CompensationEvents = [Event(id, "CE-001", CompensationEventStatus.Quoted, 310_000m)] });

        // Status and value are tracked separately because either alone tells half the story: a CE
        // can be accepted without its value moving, or re-estimated without changing status.
        var change = result.CompensationEventChanges.Should().ContainSingle().Subject;
        change.ChangeType.Should().Be(ItemChangeType.Modified);
        change.FromStatus.Should().Be(change.ToStatus);
        change.EstimatedValueDelta.Should().Be(190_000m);
    }

    [Fact]
    public async Task Orders_compensation_events_by_the_size_of_the_value_movement()
    {
        using var db = NewContext();
        var small = Guid.NewGuid();
        var large = Guid.NewGuid();

        var result = await CompareAsync(db,
            new Registers
            {
                CompensationEvents =
                [
                    Event(small, "CE-001", CompensationEventStatus.Notified, 10_000m),
                    Event(large, "CE-002", CompensationEventStatus.Notified, 50_000m),
                ],
            },
            new Registers
            {
                CompensationEvents =
                [
                    Event(small, "CE-001", CompensationEventStatus.Quoted, 15_000m),   // +5k
                    Event(large, "CE-002", CompensationEventStatus.Quoted, 400_000m),  // +350k
                ],
            });

        result.CompensationEventChanges.Select(c => c.Reference).Should().ContainInOrder("CE-002", "CE-001");
    }

    [Fact]
    public async Task Reports_a_new_variation_with_no_delta()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await CompareAsync(db,
            new Registers(),
            new Registers { Variations = [Change(id, "VO-014", VariationStatus.Instructed, 85_000m)] });

        var change = result.VariationChanges.Should().ContainSingle().Subject;
        change.ChangeType.Should().Be(ItemChangeType.Added);
        change.ToValueImpact.Should().Be(85_000m);
        change.FromValueImpact.Should().BeNull();
        // "Instructed at £85,000" is not "rose by £85,000".
        change.ValueImpactDelta.Should().BeNull();
    }

    [Fact]
    public async Task Tracks_claimed_and_awarded_extension_days_separately()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await CompareAsync(db,
            new Registers { ExtensionsOfTime = [Extension(id, "EOT-003", ExtensionOfTimeStatus.Claimed, 45, null)] },
            new Registers { ExtensionsOfTime = [Extension(id, "EOT-003", ExtensionOfTimeStatus.Awarded, 45, 21)] });

        var change = result.ExtensionOfTimeChanges.Should().ContainSingle().Subject;
        change.FromDaysClaimed.Should().Be(45);
        change.ToDaysClaimed.Should().Be(45);

        // Undetermined is null, not zero — and the difference between "not yet decided" and
        // "decided, nothing awarded" is the entire substance of an extension-of-time dispute.
        change.FromDaysAwarded.Should().BeNull();
        change.ToDaysAwarded.Should().Be(21);
        change.DaysAwardedDelta.Should().BeNull("there was no award at the earlier point to move from");
    }

    [Fact]
    public async Task Reports_a_revised_claim_even_while_it_remains_undetermined()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();

        var result = await CompareAsync(db,
            new Registers { ExtensionsOfTime = [Extension(id, "EOT-004", ExtensionOfTimeStatus.UnderReview, 30, null)] },
            new Registers { ExtensionsOfTime = [Extension(id, "EOT-004", ExtensionOfTimeStatus.UnderReview, 90, null)] });

        // A claim tripling is the contractor's position moving, not the programme's. It is still
        // worth reporting — a committee that only heard about awards would be surprised later.
        var change = result.ExtensionOfTimeChanges.Should().ContainSingle().Subject;
        change.FromDaysClaimed.Should().Be(30);
        change.ToDaysClaimed.Should().Be(90);
        change.ToDaysAwarded.Should().BeNull();
    }

    [Fact]
    public async Task Reports_nothing_across_every_register_when_nothing_moved()
    {
        using var db = NewContext();
        var registers = new Registers
        {
            EarlyWarnings = [Warning(Guid.NewGuid(), "Utilities diversion", Nec4RegisterStatus.Open)],
            CompensationEvents = [Event(Guid.NewGuid(), "CE-001", CompensationEventStatus.Notified, 10_000m)],
            Variations = [Change(Guid.NewGuid(), "VO-001", VariationStatus.Instructed, 5_000m)],
            ExtensionsOfTime = [Extension(Guid.NewGuid(), "EOT-001", ExtensionOfTimeStatus.Claimed, 10, null)],
        };

        var result = await CompareAsync(db, registers, registers);

        result.HasChanges.Should().BeFalse();
        result.EarlyWarningChanges.Should().BeEmpty();
        result.CompensationEventChanges.Should().BeEmpty();
        result.VariationChanges.Should().BeEmpty();
        result.ExtensionOfTimeChanges.Should().BeEmpty();
    }
}
