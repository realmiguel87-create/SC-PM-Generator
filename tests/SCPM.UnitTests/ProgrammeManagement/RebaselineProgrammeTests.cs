using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.ProgrammeManagement.Commands.RebaselineProgramme;
using SCPM.Application.ProgrammeManagement.Queries.GetProgrammeAgainstBaseline;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;
using SCPM.Infrastructure.Persistence;
using Xunit;

namespace SCPM.UnitTests.ProgrammeManagement;

/// <summary>
/// Rebaselining, against a real DbContext (EF Core InMemory) so the handler's LINQ actually runs.
///
/// The behaviour worth pinning down here is what survives a rebaseline. `Milestone` is already a
/// temporal table, so the old dates were never at risk of being lost — but "recoverable from a
/// change log" and "answerable as a question" are different things. What these tests protect is
/// the second: that after the programme has been reset, "how far are we from the programme
/// sanctioned in March?" still has an answer, attached to a record naming who sanctioned it and
/// why.
/// </summary>
public class RebaselineProgrammeTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    /// <summary>
    /// The signed-in user. The approver is taken from the caller's identity rather than the
    /// request, so this is what a rebaseline should be attributed to.
    /// </summary>
    private static readonly Guid SignedInUser = Guid.NewGuid();

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
            ForecastCost = 26_500_000m,
        });
        db.SaveChanges();

        return db;
    }

    private static Milestone MilestoneWith(
        string name,
        DateOnly baseline,
        DateOnly forecast,
        DateOnly? actual = null,
        bool isKey = false) => new()
        {
            Id = Guid.NewGuid(),
            ProjectId = ProjectId,
            Name = name,
            BaselineDate = baseline,
            ForecastDate = forecast,
            ActualDate = actual,
            Status = actual.HasValue ? MilestoneStatus.Complete : MilestoneStatus.InProgress,
            IsKeyMilestone = isKey,
        };

    private static async Task<Guid> RebaselineAsync(
        AppDbContext db, string name = "Post-tender programme", string? reason = null)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(SignedInUser);

        var handler = new RebaselineProgrammeCommandHandler(db, currentUser);
        return await handler.Handle(
            new RebaselineProgrammeCommand(
                ProjectId,
                name,
                reason ?? "Tender returns came in three months later than programmed.",
                new DateOnly(2026, 9, 1)),
            CancellationToken.None);
    }

    [Fact]
    public async Task Captures_the_pre_existing_dates_as_revision_1()
    {
        using var db = NewContext();
        db.Milestones.Add(MilestoneWith("Start on site", new(2026, 8, 1), new(2026, 11, 1)));
        await db.SaveChangesAsync();

        await RebaselineAsync(db);

        // The original programme predates this feature, so nothing recorded it as a baseline. It
        // is captured on the way past — otherwise the dates a committee actually sanctioned would
        // survive only in the milestone temporal history, where nothing marks which row was the
        // sanctioned one. Recoverable in principle; useless in practice.
        var original = await db.ProgrammeBaselines
            .Include(b => b.Entries)
            .SingleAsync(b => b.Revision == 1);

        original.Entries.Single().BaselineDate.Should().Be(new DateOnly(2026, 8, 1));
        original.IsCurrent.Should().BeFalse();
    }

    [Fact]
    public async Task Does_not_attribute_the_original_baseline_to_whoever_ran_the_rebaseline()
    {
        using var db = NewContext();
        db.Milestones.Add(MilestoneWith("Start on site", new(2026, 8, 1), new(2026, 11, 1)));
        await db.SaveChangesAsync();

        await RebaselineAsync(db);

        // Whoever approved those original dates did so before this record existed. Naming the
        // person running the rebaseline would attribute a decision they did not take — in a
        // register whose whole purpose is evidencing who decided what.
        var original = await db.ProgrammeBaselines.SingleAsync(b => b.Revision == 1);
        original.ApprovedBy.Should().BeNull();
        original.ApprovedDate.Should().BeNull();
    }

    [Fact]
    public async Task Attributes_the_rebaseline_to_the_signed_in_user()
    {
        using var db = NewContext();
        db.Milestones.Add(MilestoneWith("Start on site", new(2026, 8, 1), new(2026, 11, 1)));
        await db.SaveChangesAsync();

        await RebaselineAsync(db);

        // The approver comes from the caller's identity, not the request. It was a parameter and
        // could not be used: it is an SCPM user id, and a browser has no way to know one — so the
        // field could only ever be filled by typing a GUID, which in a record evidencing who
        // sanctioned a change is worse than leaving it empty.
        var current = await db.ProgrammeBaselines.SingleAsync(b => b.IsCurrent);
        current.ApprovedBy.Should().Be(SignedInUser);
        current.ApprovedDate.Should().Be(new DateOnly(2026, 9, 1));
    }

    [Fact]
    public async Task Records_neither_approver_nor_date_when_no_approval_is_given()
    {
        using var db = NewContext();
        db.Milestones.Add(MilestoneWith("Start on site", new(2026, 8, 1), new(2026, 11, 1)));
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(SignedInUser);

        await new RebaselineProgrammeCommandHandler(db, currentUser).Handle(
            new RebaselineProgrammeCommand(
                ProjectId, "Working revision", "Recorded before the committee has met.", null),
            CancellationToken.None);

        // The two travel together or not at all. An approver with no date reads as authority
        // without being any: it would say someone signed this off while recording no moment at
        // which they did.
        var current = await db.ProgrammeBaselines.SingleAsync(b => b.IsCurrent);
        current.ApprovedBy.Should().BeNull();
        current.ApprovedDate.Should().BeNull();
    }

    [Fact]
    public async Task Moves_the_milestone_baseline_to_the_forecast()
    {
        using var db = NewContext();
        db.Milestones.Add(MilestoneWith("Start on site", new(2026, 8, 1), new(2026, 11, 1)));
        await db.SaveChangesAsync();

        await RebaselineAsync(db);

        var milestone = await db.Milestones.SingleAsync();
        milestone.BaselineDate.Should().Be(new DateOnly(2026, 11, 1));
        // Against the new programme it is on time — which is what rebaselining means. The old
        // slip has not vanished; it has become a question about revision 1.
        milestone.DelayDays.Should().Be(0);
    }

    [Fact]
    public async Task Rebaselines_a_completed_milestone_to_its_actual_date_not_its_forecast()
    {
        using var db = NewContext();
        db.Milestones.Add(MilestoneWith(
            "Planning consent", new(2026, 6, 1), forecast: new(2026, 9, 1), actual: new(2026, 6, 15)));
        await db.SaveChangesAsync();

        await RebaselineAsync(db);

        // Sanctioning the forecast would enshrine a date already disproved by events: the
        // milestone completed on 15 June, so a baseline saying 1 September is simply wrong.
        var milestone = await db.Milestones.SingleAsync();
        milestone.BaselineDate.Should().Be(new DateOnly(2026, 6, 15));
    }

    [Fact]
    public async Task Marks_only_the_newest_baseline_as_current()
    {
        using var db = NewContext();
        db.Milestones.Add(MilestoneWith("Start on site", new(2026, 8, 1), new(2026, 11, 1)));
        await db.SaveChangesAsync();

        await RebaselineAsync(db);

        var milestone = await db.Milestones.SingleAsync();
        milestone.ForecastDate = new DateOnly(2027, 2, 1);
        await db.SaveChangesAsync();

        await RebaselineAsync(db, "Second rebaseline");

        var baselines = await db.ProgrammeBaselines.OrderBy(b => b.Revision).ToListAsync();
        baselines.Select(b => b.Revision).Should().Equal(1, 2, 3);
        baselines.Where(b => b.IsCurrent).Should().ContainSingle()
            .Which.Revision.Should().Be(3);
    }

    [Fact]
    public async Task Refuses_to_rebaseline_a_programme_with_no_milestones()
    {
        using var db = NewContext();

        var act = async () => await RebaselineAsync(db);

        // A baseline with no dates sanctions nothing, and recording one puts an entry in the
        // governance register that reads as a decision having been taken.
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no milestones*");
    }

    [Fact]
    public async Task Answers_slip_against_the_superseded_programme()
    {
        using var db = NewContext();
        db.Milestones.Add(MilestoneWith("Start on site", new(2026, 8, 1), new(2026, 11, 1), isKey: true));
        await db.SaveChangesAsync();

        await RebaselineAsync(db);

        var original = await db.ProgrammeBaselines.SingleAsync(b => b.Revision == 1);
        var result = await new GetProgrammeAgainstBaselineQueryHandler(db)
            .Handle(new GetProgrammeAgainstBaselineQuery(ProjectId, original.Id), CancellationToken.None);

        // This is the whole point. Against the current programme the project is on time; against
        // the one that was sanctioned it is 92 days late, and that is the figure a committee asked
        // about the March programme needs to be given.
        result!.WorstSlipDays.Should().Be(92);
        result.WorstSlipMilestone.Should().Be("Start on site");
        result.Milestones.Single().SlipDays.Should().Be(92);
    }

    [Fact]
    public async Task Reports_no_slip_against_the_current_programme()
    {
        using var db = NewContext();
        db.Milestones.Add(MilestoneWith("Start on site", new(2026, 8, 1), new(2026, 11, 1)));
        await db.SaveChangesAsync();

        await RebaselineAsync(db);

        var result = await new GetProgrammeAgainstBaselineQueryHandler(db)
            .Handle(new GetProgrammeAgainstBaselineQuery(ProjectId, null), CancellationToken.None);

        result!.Baseline.Revision.Should().Be(2);
        result.WorstSlipDays.Should().Be(0);
        result.WorstSlipMilestone.Should().BeNull();
    }

    [Fact]
    public async Task Carries_no_slip_for_a_milestone_added_after_the_baseline()
    {
        using var db = NewContext();
        db.Milestones.Add(MilestoneWith("Start on site", new(2026, 8, 1), new(2026, 11, 1)));
        await db.SaveChangesAsync();

        await RebaselineAsync(db);

        db.Milestones.Add(MilestoneWith("Handover", new(2027, 6, 1), new(2027, 12, 1)));
        await db.SaveChangesAsync();

        var result = await new GetProgrammeAgainstBaselineQueryHandler(db)
            .Handle(new GetProgrammeAgainstBaselineQuery(ProjectId, null), CancellationToken.None);

        var added = result!.Milestones.Single(m => m.Name == "Handover");
        // It was not in the programme being measured. It is neither early nor late against it, and
        // scoring it against a date nobody set would be inventing a figure — one that would then
        // appear in the worst-slip headline as though it meant something.
        added.AddedSinceBaseline.Should().BeTrue();
        added.SlipDays.Should().Be(0);
        added.BaselineDate.Should().BeNull();
        result.WorstSlipDays.Should().Be(0);
    }

    [Fact]
    public async Task Names_a_milestone_that_has_been_dropped_since_the_baseline()
    {
        using var db = NewContext();
        var milestone = MilestoneWith("Start on site", new(2026, 8, 1), new(2026, 11, 1));
        var survivor = MilestoneWith("Handover", new(2027, 6, 1), new(2027, 6, 1));
        db.Milestones.AddRange(milestone, survivor);
        await db.SaveChangesAsync();

        await RebaselineAsync(db);

        milestone.IsDeleted = true;
        await db.SaveChangesAsync();

        var result = await new GetProgrammeAgainstBaselineQueryHandler(db)
            .Handle(new GetProgrammeAgainstBaselineQuery(ProjectId, null), CancellationToken.None);

        // Named, not counted. A milestone quietly disappearing from an approved programme is
        // something a reader has to see; "1 removed" tells them nothing they can act on.
        result!.RemovedSinceBaseline.Should().Equal("Start on site");
    }

    [Fact]
    public async Task Keeps_the_baselined_name_when_a_milestone_is_renamed_afterwards()
    {
        using var db = NewContext();
        var milestone = MilestoneWith("Start on site", new(2026, 8, 1), new(2026, 11, 1));
        db.Milestones.Add(milestone);
        await db.SaveChangesAsync();

        await RebaselineAsync(db);

        milestone.Name = "Construction commencement";
        await db.SaveChangesAsync();

        var result = await new GetProgrammeAgainstBaselineQueryHandler(db)
            .Handle(new GetProgrammeAgainstBaselineQuery(ProjectId, null), CancellationToken.None);

        var row = result!.Milestones.Single();
        // Joining to the live row would silently rewrite a committee-approved document to match
        // today's names. Both are reported: what it was called when sanctioned, and what it is
        // called now.
        row.BaselineName.Should().Be("Start on site");
        row.Name.Should().Be("Construction commencement");
    }

    [Fact]
    public async Task Returns_nothing_for_a_project_that_has_never_been_baselined()
    {
        using var db = NewContext();
        db.Milestones.Add(MilestoneWith("Start on site", new(2026, 8, 1), new(2026, 11, 1)));
        await db.SaveChangesAsync();

        var result = await new GetProgrammeAgainstBaselineQueryHandler(db)
            .Handle(new GetProgrammeAgainstBaselineQuery(ProjectId, null), CancellationToken.None);

        // Null rather than an empty comparison: "no baseline exists" and "nothing has slipped"
        // are different answers, and an empty result presents them as the same one.
        result.Should().BeNull();
    }
}
