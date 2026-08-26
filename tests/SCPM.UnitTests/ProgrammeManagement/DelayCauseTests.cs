using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.ProgrammeManagement.Commands.RecordDelayCause;
using SCPM.Application.ProgrammeManagement.Dtos;
using SCPM.Application.ProgrammeManagement.Queries.GetDelayAnalysis;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;
using SCPM.Infrastructure.Persistence;
using Xunit;

namespace SCPM.UnitTests.ProgrammeManagement;

/// <summary>
/// Delay-cause attribution, and the figure it exists to produce.
///
/// The programme could say a milestone was 92 days late and nothing could say why. The number
/// these tests care most about is <c>UnattributedDays</c> — slip nobody has explained — because it
/// is the one that distinguishes two situations a register otherwise reports identically: a
/// programme three months late with three months accounted for is a managed programme; three
/// months late with a fortnight accounted for is a programme nobody has got to the bottom of.
/// </summary>
public class DelayCauseTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid OtherProjectId = Guid.NewGuid();

    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.Projects.AddRange(
            new Project
            {
                Id = ProjectId,
                ProjectRef = "PRJ-0001",
                Name = "Stirling Community Campus",
                CurrentRibaStage = 5,
                ApprovedBudget = 25_000_000m,
                ForecastCost = 26_500_000m,
            },
            new Project
            {
                Id = OtherProjectId,
                ProjectRef = "PRJ-0002",
                Name = "A different scheme entirely",
                CurrentRibaStage = 2,
                ApprovedBudget = 4_000_000m,
                ForecastCost = 4_000_000m,
            });
        db.SaveChanges();

        return db;
    }

    private static Milestone MilestoneWith(string name, int slipDays, bool isKey = false)
    {
        var baseline = new DateOnly(2026, 8, 1);
        return new Milestone
        {
            Id = Guid.NewGuid(),
            ProjectId = ProjectId,
            Name = name,
            BaselineDate = baseline,
            ForecastDate = baseline.AddDays(slipDays),
            Status = MilestoneStatus.InProgress,
            IsKeyMilestone = isKey,
        };
    }

    private static async Task<Guid> RecordAsync(
        AppDbContext db,
        Guid milestoneId,
        int days,
        DelayCauseCategory category = DelayCauseCategory.Weather,
        Guid? extensionOfTimeId = null,
        Guid? compensationEventId = null)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());

        return await new RecordDelayCauseCommandHandler(db, currentUser).Handle(
            new RecordDelayCauseCommand(
                milestoneId,
                days,
                category,
                "Exceptionally adverse weather stopped the concrete pours for three weeks.",
                extensionOfTimeId,
                compensationEventId),
            CancellationToken.None);
    }

    private static Task<ProjectDelayAnalysisDto> AnalyseAsync(AppDbContext db) =>
        new GetDelayAnalysisQueryHandler(db).Handle(
            new GetDelayAnalysisQuery(ProjectId), CancellationToken.None);

    [Fact]
    public async Task Reports_the_slip_nobody_has_explained()
    {
        using var db = NewContext();
        var milestone = MilestoneWith("Start on site", slipDays: 92);
        db.Milestones.Add(milestone);
        await db.SaveChangesAsync();

        await RecordAsync(db, milestone.Id, 14);

        var analysis = await AnalyseAsync(db);
        var row = analysis.Milestones.Single();

        // This is the whole point. 92 days late, 14 accounted for, 78 that nobody has an
        // explanation for — a distinction invisible in a register that only holds the 92.
        row.SlipDays.Should().Be(92);
        row.AttributedDays.Should().Be(14);
        row.UnattributedDays.Should().Be(78);
        row.OverAttributedDays.Should().Be(0);
    }

    [Fact]
    public async Task Reports_over_attribution_rather_than_absorbing_it()
    {
        using var db = NewContext();
        var milestone = MilestoneWith("Start on site", slipDays: 30);
        db.Milestones.Add(milestone);
        await db.SaveChangesAsync();

        await RecordAsync(db, milestone.Id, 20);
        await RecordAsync(db, milestone.Id, 25, DelayCauseCategory.DesignInformation);

        var analysis = await AnalyseAsync(db);
        var row = analysis.Milestones.Single();

        // 45 days claimed against 30 days lost. Usually double-counting — the same event entered
        // twice, or two causes claiming one period. Clamping it to zero would hide the only signal
        // that says the record disagrees with itself.
        row.OverAttributedDays.Should().Be(15);
        row.UnattributedDays.Should().Be(0);
    }

    [Fact]
    public async Task Treats_a_milestone_running_early_as_having_nothing_to_explain()
    {
        using var db = NewContext();
        var milestone = MilestoneWith("Enabling works", slipDays: -20);
        db.Milestones.Add(milestone);
        await db.SaveChangesAsync();

        var analysis = await AnalyseAsync(db);
        var row = analysis.Milestones.Single();

        // Slip floors at zero before anything is subtracted. Without that, a milestone 20 days
        // early would report 20 unattributed days — a demand for an explanation of good news.
        row.SlipDays.Should().Be(0);
        row.UnattributedDays.Should().Be(0);
    }

    [Fact]
    public async Task Orders_milestones_by_how_much_of_their_slip_is_unexplained()
    {
        using var db = NewContext();
        var explained = MilestoneWith("Explained", slipDays: 60);
        var unexplained = MilestoneWith("Unexplained", slipDays: 40);
        db.Milestones.AddRange(explained, unexplained);
        await db.SaveChangesAsync();

        await RecordAsync(db, explained.Id, 60);

        var analysis = await AnalyseAsync(db);

        // The 40-day milestone leads despite being less late, because none of it is accounted
        // for. A reader scanning this wants the gaps, not the dates.
        analysis.Milestones.First().Name.Should().Be("Unexplained");
    }

    [Fact]
    public async Task Totals_days_by_category_largest_first()
    {
        using var db = NewContext();
        var a = MilestoneWith("A", slipDays: 90);
        var b = MilestoneWith("B", slipDays: 90);
        db.Milestones.AddRange(a, b);
        await db.SaveChangesAsync();

        await RecordAsync(db, a.Id, 10, DelayCauseCategory.Weather);
        await RecordAsync(db, b.Id, 30, DelayCauseCategory.StatutoryApproval);
        await RecordAsync(db, b.Id, 5, DelayCauseCategory.Weather);

        var analysis = await AnalyseAsync(db);

        // What a portfolio review reads: five projects each losing a fortnight to statutory
        // approvals is a process problem, and it is invisible one project at a time.
        analysis.DaysByCategory.First().Category.Should().Be(DelayCauseCategory.StatutoryApproval);
        analysis.DaysByCategory.First().Days.Should().Be(30);
        analysis.DaysByCategory.Single(c => c.Category == DelayCauseCategory.Weather)
            .Days.Should().Be(15);
        analysis.DaysByCategory.Single(c => c.Category == DelayCauseCategory.Weather)
            .CauseCount.Should().Be(2);
    }

    [Fact]
    public async Task Carries_the_contractual_reference_alongside_the_cause()
    {
        using var db = NewContext();
        var milestone = MilestoneWith("Start on site", slipDays: 60);
        var eot = new ExtensionOfTime
        {
            Id = Guid.NewGuid(),
            ProjectId = ProjectId,
            Reference = "EOT-003",
            Reason = "Exceptionally adverse weather",
            DaysClaimed = 45,
            DaysAwarded = 30,
        };
        db.Milestones.Add(milestone);
        db.ExtensionsOfTime.Add(eot);
        await db.SaveChangesAsync();

        await RecordAsync(db, milestone.Id, 30, extensionOfTimeId: eot.Id);

        var analysis = await AnalyseAsync(db);
        var cause = analysis.Milestones.Single().Causes.Single();

        // Carried on the row so a reader can find the claim without a second request.
        cause.Reference.Should().Be("EOT-003");
        cause.ExtensionOfTimeId.Should().Be(eot.Id);
    }

    [Fact]
    public async Task Refuses_a_cause_citing_both_an_extension_of_time_and_a_compensation_event()
    {
        using var db = NewContext();
        var milestone = MilestoneWith("Start on site", slipDays: 60);
        var eot = new ExtensionOfTime
        {
            Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "EOT-003",
            Reason = "Weather", DaysClaimed = 45,
        };
        var ce = new CompensationEvent
        {
            Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "CE-012",
            Title = "Revised roof specification", EstimatedValue = 120_000m,
            NotifiedDate = new DateOnly(2026, 7, 1),
        };
        db.Milestones.Add(milestone);
        db.ExtensionsOfTime.Add(eot);
        db.CompensationEvents.Add(ce);
        await db.SaveChangesAsync();

        var act = async () => await RecordAsync(
            db, milestone.Id, 30, extensionOfTimeId: eot.Id, compensationEventId: ce.Id);

        // One cause, one piece of evidence. Citing both describes two contracts at once, and no
        // single project is administered under two.
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not both*");
    }

    [Fact]
    public async Task Refuses_a_claim_belonging_to_a_different_project()
    {
        using var db = NewContext();
        var milestone = MilestoneWith("Start on site", slipDays: 60);
        var foreign = new ExtensionOfTime
        {
            Id = Guid.NewGuid(),
            ProjectId = OtherProjectId,
            Reference = "EOT-001",
            Reason = "Someone else's weather",
            DaysClaimed = 20,
        };
        db.Milestones.Add(milestone);
        db.ExtensionsOfTime.Add(foreign);
        await db.SaveChangesAsync();

        var act = async () => await RecordAsync(db, milestone.Id, 30, extensionOfTimeId: foreign.Id);

        // The resulting analysis would have looked perfectly well-formed while attributing one
        // project's delay to another's paperwork — the kind of error that survives review
        // precisely because nothing about it looks wrong.
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not belong to this milestone's project*");
    }

    [Fact]
    public async Task Refuses_a_cause_against_a_milestone_that_does_not_exist()
    {
        using var db = NewContext();

        var act = async () => await RecordAsync(db, Guid.NewGuid(), 10);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task Sums_unexplained_days_across_the_project()
    {
        using var db = NewContext();
        var a = MilestoneWith("A", slipDays: 30);
        var b = MilestoneWith("B", slipDays: 40, isKey: true);
        db.Milestones.AddRange(a, b);
        await db.SaveChangesAsync();

        await RecordAsync(db, a.Id, 10);

        var analysis = await AnalyseAsync(db);

        // A total is defensible here in a way it is not for slip itself: this measures how much
        // of the delay is unexplained rather than how late the project is, and unexplained days on
        // unrelated milestones genuinely do add up as a body of work nobody has done.
        analysis.TotalSlipDays.Should().Be(70);
        analysis.TotalAttributedDays.Should().Be(10);
        analysis.TotalUnattributedDays.Should().Be(60);
    }
}
