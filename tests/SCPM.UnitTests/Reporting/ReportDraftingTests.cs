using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Reporting.Commands.CreateCommitteeReport;
using SCPM.Application.Reporting.Drafting;
using SCPM.Domain.Common;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;
using SCPM.Infrastructure.Persistence;
using Xunit;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// Drafting a report's factual sections from the registers.
///
/// A document that retypes the cost position can disagree with the cost register, and when the
/// two disagree there is nothing to say which is right. These assert that the figures come from
/// the registers — and, just as importantly, that the judgement sections stay empty. A plausible
/// generated paragraph under "What Did Not Work" is worse than a blank one: nobody would check it.
///
/// All fixture data is invented.
/// </summary>
public class ReportDraftingTests
{
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static AppDbContext Database()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.Projects.Add(new Project
        {
            Id = ProjectId,
            Name = "Bridgeview Primary School",
            ProjectRef = "ID00001",
            CurrentRibaStage = 5,
            ApprovedBudget = 412_500m,
            ForecastCost = 431_900m,
        });

        // DelayDays is computed from the dates rather than stored, so the fixture sets dates.
        var baseline = new DateOnly(2026, 6, 1);
        db.Milestones.AddRange(
            new Milestone { Id = Guid.NewGuid(), ProjectId = ProjectId, Name = "Roof watertight", Status = MilestoneStatus.InProgress, BaselineDate = baseline, ForecastDate = baseline.AddDays(12) },
            new Milestone { Id = Guid.NewGuid(), ProjectId = ProjectId, Name = "M&E first fix", Status = MilestoneStatus.NotStarted, BaselineDate = baseline, ForecastDate = baseline },
            // Complete, and was 40 days late. Must not reach an "outstanding" figure.
            new Milestone { Id = Guid.NewGuid(), ProjectId = ProjectId, Name = "Strip out", Status = MilestoneStatus.Complete, BaselineDate = baseline, ForecastDate = baseline.AddDays(40), ActualDate = baseline.AddDays(40) });

        db.Risks.AddRange(
            new Risk { Id = Guid.NewGuid(), ProjectId = ProjectId, Title = "Asbestos in ceiling void", Category = "Construction", Status = RiskStatus.Open, Probability = 4, Impact = 5 },
            new Risk { Id = Guid.NewGuid(), ProjectId = ProjectId, Title = "Supplier lead time", Category = "Programme", Status = RiskStatus.Open, Probability = 2, Impact = 3 },
            new Risk { Id = Guid.NewGuid(), ProjectId = ProjectId, Title = "Closed one", Category = "Cost", Status = RiskStatus.Closed, Probability = 5, Impact = 5 });

        db.Issues.AddRange(
            new Issue { Id = Guid.NewGuid(), ProjectId = ProjectId, Title = "Phasing clash", Status = IssueStatus.Open },
            new Issue { Id = Guid.NewGuid(), ProjectId = ProjectId, Title = "Settled", Status = IssueStatus.Closed });

        db.CompensationEvents.AddRange(
            new CompensationEvent { Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "CE001", Title = "Unforeseen ground", EstimatedValue = 14_000m, Status = CompensationEventStatus.Quoted },
            new CompensationEvent { Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "CE002", Title = "Already in the forecast", EstimatedValue = 90_000m, Status = CompensationEventStatus.Implemented },
            new CompensationEvent { Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "CE003", Title = "Refused", EstimatedValue = 50_000m, Status = CompensationEventStatus.Rejected });

        db.Variations.AddRange(
            new Variation { Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "V001", Description = "Extra sockets", ValueImpact = 5_400m, Status = VariationStatus.Instructed },
            new Variation { Id = Guid.NewGuid(), ProjectId = ProjectId, Reference = "V002", Description = "Settled", ValueImpact = 20_000m, Status = VariationStatus.Agreed });

        db.SaveChanges();
        return db;
    }

    private static async Task<CommitteeReport> Create(AppDbContext db, CommitteeReportType type)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());

        var id = await new CreateCommitteeReportCommandHandler(db, currentUser).Handle(
            new CreateCommitteeReportCommand(ProjectId, type, "A report", null, null, null),
            CancellationToken.None);

        return await db.CommitteeReports.Include(r => r.Sections).FirstAsync(r => r.Id == id);
    }

    private static ProjectFacts Facts(AppDbContext db) =>
        ProjectFacts.ReadAsync(db, db.Projects.First(p => p.Id == ProjectId), CancellationToken.None)
            .GetAwaiter().GetResult();

    [Fact]
    public void Drafts_only_into_sections_the_document_actually_has()
    {
        // The most valuable assertion here. Section content is keyed by string, and the create
        // handler silently drops a draft whose key the document does not define — so a typo
        // produces a document missing the one section that was supposed to be filled in, with no
        // error anywhere. This catches that for every document at once.
        var facts = Facts(Database());

        foreach (var type in Enum.GetValues<CommitteeReportType>())
        {
            var keys = ReportSections.For(type).Select(s => s.Key).ToHashSet();

            // BeSubsetOf rather than OnlyContain: eleven documents correctly draft nothing, and
            // OnlyContain treats an empty collection as a failure.
            ReportDrafts.For(type, facts).Keys
                .Should().BeSubsetOf(keys,
                    $"every section {type} drafts must be one it defines");
        }
    }

    [Fact]
    public async Task Draws_the_construction_report_from_four_registers()
    {
        using var db = Database();
        var report = await Create(db, CommitteeReportType.ConstructionProgressReport);
        var content = report.Sections.ToDictionary(s => s.SectionKey, s => s.Content);

        content["cost"].Should().Contain("£412,500").And.Contain("£431,900").And.Contain("over budget");

        // Two outstanding, one of them late. The completed milestone's 40 days must not appear:
        // a delay on something already finished is history, not an outstanding position.
        content["programme"].Should().Contain("2 milestone(s) outstanding")
            .And.Contain("1 are behind baseline").And.Contain("12 day(s)");
        content["programme"].Should().NotContain("40");

        content["risks"].Should().Contain("2 open risk(s)").And.Contain("20/25")
            .And.Contain("1 open issue(s)");

        // Open compensation events only: the implemented one is already in the forecast and the
        // rejected one costs nothing, so counting either would overstate what is still moving.
        content["change"].Should().Contain("1 open compensation event(s)").And.Contain("£14,000");
        content["change"].Should().NotContain("£90,000").And.NotContain("£50,000");

        // Variations not yet agreed, for the same reason.
        content["change"].Should().Contain("1 variation(s) not yet agreed").And.Contain("£5,400");
        content["change"].Should().NotContain("£20,000");
    }

    [Fact]
    public async Task Leaves_the_authors_account_of_the_period_empty()
    {
        using var db = Database();
        var report = await Create(db, CommitteeReportType.ConstructionProgressReport);
        var drafted = report.Sections.Select(s => s.SectionKey).ToList();

        // What was built, what is next, what the inspections found, what happened on site. A
        // generated paragraph in any of these reads plausibly enough that nobody would check it.
        drafted.Should().NotContain("progress");
        drafted.Should().NotContain("planned");
        drafted.Should().NotContain("quality");
        drafted.Should().NotContain("safety");
    }

    [Fact]
    public async Task Drafts_nothing_judgemental_into_a_lessons_learned_report()
    {
        using var db = Database();
        var report = await Create(db, CommitteeReportType.LessonsLearnedReport);
        var drafted = report.Sections.Select(s => s.SectionKey).ToList();

        // The arithmetic only. What worked, what did not and what was not anticipated are the
        // entire point of the document; a generated paragraph there would be a lesson nobody
        // learned.
        drafted.Should().BeEquivalentTo(["summary"]);
    }

    [Theory]
    [InlineData(CommitteeReportType.ProjectBrief)]
    [InlineData(CommitteeReportType.ProjectMandate)]
    [InlineData(CommitteeReportType.PreConstructionInformation)]
    [InlineData(CommitteeReportType.PracticalCompletionCertificate)]
    [InlineData(CommitteeReportType.PostOccupancyEvaluation)]
    public async Task Drafts_nothing_into_a_document_whose_content_the_platform_does_not_hold(
        CommitteeReportType type)
    {
        using var db = Database();
        var report = await Create(db, type);

        // These are written before there is anything to report, or their content is not in the
        // platform at all. Drafting into them would be inventing it.
        report.Sections.Should().BeEmpty();
    }

    [Fact]
    public async Task Draws_the_gate_report_from_the_registers_but_not_its_recommendation()
    {
        using var db = Database();

        var stage = new RibaStageInstance { Id = Guid.NewGuid(), ProjectId = ProjectId, StageNumber = 5 };
        var gateway = new Gateway
        {
            Id = Guid.NewGuid(),
            ProjectId = ProjectId,
            RibaStageInstanceId = stage.Id,
            GatewayType = "Stage 5 Gate",
        };
        db.RibaStageInstances.Add(stage);
        db.Gateways.Add(gateway);
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());

        var id = await new CreateCommitteeReportCommandHandler(db, currentUser).Handle(
            new CreateCommitteeReportCommand(
                ProjectId, CommitteeReportType.StageGateApproval, "Stage 5 Gate",
                null, null, null, gateway.Id),
            CancellationToken.None);

        var report = await db.CommitteeReports.Include(r => r.Sections).FirstAsync(r => r.Id == id);
        var drafted = report.Sections.Select(s => s.SectionKey).ToList();

        drafted.Should().BeEquivalentTo(["cost", "programme", "risks"]);

        // The decision sought, the conditions and the recommendation are the substance of a gate
        // report, and none of them is a figure.
        drafted.Should().NotContain("recommendation");
        drafted.Should().NotContain("conditions");
        drafted.Should().NotContain("decision-record");
    }

    [Fact]
    public void Prices_in_sterling_whatever_the_server_is_set_to()
    {
        using var db = Database();

        // "$412,500" on a council report is a defect only ever found by a reader.
        Facts(db).Cost().Should().Contain("£").And.NotContain("$");
    }

    [Fact]
    public void Says_so_plainly_when_a_register_is_empty()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new AppDbContext(options);
        db.Projects.Add(new Project
        {
            Id = ProjectId, Name = "Empty", ProjectRef = "ID00002",
            ApprovedBudget = 100_000m, ForecastCost = 100_000m,
        });
        db.SaveChanges();

        var facts = Facts(db);

        // "0 milestone(s) outstanding, of which 0 are behind baseline." is technically true and
        // reads as a system with nothing to say. A sentence is better than a row of zeroes.
        facts.Programme().Should().Be("No outstanding milestones recorded.");
        facts.Risk().Should().Be("No open risks recorded.");
        facts.Change().Should().Be("No open compensation events or variations recorded.");
        facts.Cost().Should().Contain("on budget");
    }
}
