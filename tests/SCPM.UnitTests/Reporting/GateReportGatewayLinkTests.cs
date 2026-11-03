using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Reporting.Commands.CreateCommitteeReport;
using SCPM.Application.Reporting.Queries.GetCommitteeReport;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;
using SCPM.Infrastructure.Persistence;
using Xunit;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// The stage gate approval report reads its decision from the gateway's approval rather than from
/// text typed into the document.
///
/// This is the whole point of the link. A hand-typed copy of an approval can disagree with
/// <see cref="Approval.Decision"/>, and a governance file that contradicts its own register is
/// worse than one that simply refers to it — there is nothing to say which of the two is right.
///
/// All fixture data is invented.
/// </summary>
public class GateReportGatewayLinkTests
{
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid GatewayId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid StageInstanceId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ApproverId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ReportId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static AppDbContext Database(params Approval[] approvals)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.Projects.Add(new Project
        {
            Id = ProjectId,
            Name = "Bridgeview Primary School – Hall Refurbishment",
            ProjectRef = "ID00001",
            ApprovedBudget = 412_500m,
        });

        db.Users.Add(new User
        {
            Id = ApproverId,
            DisplayName = "C. Decider",
            Email = "c.decider@example.invalid",
            EntraObjectId = "00000000-0000-0000-0000-0000000000cd",
        });

        db.RibaStageInstances.Add(new RibaStageInstance
        {
            Id = StageInstanceId,
            ProjectId = ProjectId,
            StageNumber = 2,
        });

        db.Gateways.Add(new Gateway
        {
            Id = GatewayId,
            ProjectId = ProjectId,
            RibaStageInstanceId = StageInstanceId,
            GatewayType = "Stage 2 Gate",
            Status = approvals.Length == 0 ? GatewayStatus.Pending : GatewayStatus.Approved,
        });

        db.CommitteeReports.Add(new CommitteeReport
        {
            Id = ReportId,
            ProjectId = ProjectId,
            GatewayId = GatewayId,
            ReportType = CommitteeReportType.StageGateApproval,
            Title = "Stage Gate Approval Report",
        });

        if (approvals.Length > 0) db.Approvals.AddRange(approvals);

        db.SaveChanges();
        return db;
    }

    private static Approval Decision(ApprovalDecision decision, DateTime at) => new()
    {
        Id = Guid.NewGuid(),
        GatewayId = GatewayId,
        ApproverUserId = ApproverId,
        Decision = decision,
        DecisionDate = at,
    };

    private static async Task<SCPM.Application.Reporting.Dtos.CommitteeReportDto> Read(AppDbContext db) =>
        (await new GetCommitteeReportQueryHandler(db)
            .Handle(new GetCommitteeReportQuery(ReportId), CancellationToken.None))!;

    [Fact]
    public async Task Reads_the_riba_stage_from_the_gateways_stage_instance()
    {
        using var db = Database();
        db.RibaStageDefinitions.Add(new RibaStageDefinition
        {
            StageNumber = 2,
            StageName = "Concept Design",
            Description = "Prepare the concept design.",
        });
        db.SaveChanges();

        var report = await Read(db);

        // Named the way the five Stage 0–1 documents name theirs, so the whole pack reads
        // consistently rather than one document saying "Stage 2" and another "RIBA Stage 2".
        report.RibaStage.Should().Be("Stage 2 — Concept Design");
    }

    [Fact]
    public async Task Falls_back_to_the_stage_number_when_the_definition_is_missing()
    {
        using var db = Database();

        var report = await Read(db);

        // Degrading to a bare number beats failing: the stage definitions are seeded reference
        // data, and a report should still open on a database where seeding has not run.
        report.RibaStage.Should().Be("Stage 2");
    }

    [Fact]
    public async Task Reads_the_decision_from_the_approval()
    {
        using var db = Database(Decision(ApprovalDecision.Approved, new DateTime(2026, 10, 14)));

        var report = await Read(db);

        report.ClosingPanel.Should().HaveCount(3);
        report.ClosingPanel[0].Label.Should().Be("Decision");
        report.ClosingPanel[0].Value.Should().Be("Approved");
        report.ClosingPanel[1].Value.Should().Be("C. Decider");
        report.ClosingPanel[2].Value.Should().Be("14 October 2026");
    }

    [Fact]
    public async Task Writes_the_decision_in_the_readers_words()
    {
        using var db = Database(
            Decision(ApprovalDecision.ApprovedWithConditions, new DateTime(2026, 10, 14)));

        var report = await Read(db);

        // "ApprovedWithConditions" on a council document is a leaked identifier.
        report.ClosingPanel[0].Value.Should().Be("Approved with conditions");
    }

    [Fact]
    public async Task Reports_the_latest_decision_not_the_first()
    {
        using var db = Database(
            Decision(ApprovalDecision.Rejected, new DateTime(2026, 9, 2)),
            Decision(ApprovalDecision.Approved, new DateTime(2026, 10, 14)));

        var report = await Read(db);

        // A gateway rejected and later approved should report the approval. Taking the first
        // would have the document state an outcome the register has since superseded.
        report.ClosingPanel[0].Value.Should().Be("Approved");
        report.ClosingPanel[2].Value.Should().Be("14 October 2026");
    }

    [Fact]
    public async Task Leaves_the_panel_blank_while_no_decision_has_been_taken()
    {
        using var db = Database();

        var report = await Read(db);

        // Blank rather than "Pending". A gate report is normally written to seek a decision, so
        // blank is the honest state and leaves the approver room to record one; the word
        // "Pending" printed in a decision field reads like an outcome.
        report.ClosingPanel.Should().HaveCount(3);
        report.ClosingPanel.Should().OnlyContain(f => f.Value == string.Empty);
    }

    [Fact]
    public async Task Leaves_other_report_types_with_no_panel_and_no_stage()
    {
        using var db = Database();

        var paper = new CommitteeReport
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            ProjectId = ProjectId,
            ReportType = CommitteeReportType.CommitteeReport,
            Title = "Committee Paper",
        };
        db.CommitteeReports.Add(paper);
        db.SaveChanges();

        var report = (await new GetCommitteeReportQueryHandler(db)
            .Handle(new GetCommitteeReportQuery(paper.Id), CancellationToken.None))!;

        report.ClosingPanel.Should().BeEmpty();
        report.RibaStage.Should().BeNull();
    }

    [Theory]
    [InlineData(CommitteeReportType.StageGateApproval, false, false)]
    [InlineData(CommitteeReportType.StageGateApproval, true, true)]
    [InlineData(CommitteeReportType.CommitteeReport, true, false)]
    [InlineData(CommitteeReportType.CommitteeReport, false, true)]
    public void Requires_a_gateway_for_a_gate_report_and_forbids_one_elsewhere(
        CommitteeReportType type, bool withGateway, bool expectedValid)
    {
        var command = new CreateCommitteeReportCommand(
            ProjectId, type, "A report", null, null, null,
            withGateway ? GatewayId : null);

        var result = new CreateCommitteeReportCommandValidator().Validate(command);

        // Without a gateway a gate report is a decision document about no decision, and its
        // stage, approver and outcome have nowhere to come from but someone's typing. With one,
        // a committee paper implies it records that gateway's decision, which it does not.
        result.IsValid.Should().Be(expectedValid);
    }
}
