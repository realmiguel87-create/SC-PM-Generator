using SCPM.Domain.Enums;

namespace SCPM.Application.Reporting.Drafting;

/// <summary>
/// Which sections of which documents are drafted from the registers, and which are left empty.
///
/// The whole of the judgement lives here, in one list, so it can be reviewed as a list. The rule
/// it follows has not changed since the first report type:
///
///   A figure the platform already holds is drafted. Retyping the cost position into a document
///   is how that document comes to disagree with the cost register, and when the two disagree
///   there is nothing to say which is right.
///
///   A judgement is never drafted. What happened this period, which issues matter, whether a
///   tender should be accepted, what a project taught — generated prose there would produce
///   something nobody should trust unread, and which reads plausibly enough that nobody would.
///
/// Most documents appear here with one or two entries, and eleven appear not at all. A Project
/// Brief, a Pre-Construction Information pack and a Practical Completion Certificate have no
/// section whose content the platform knows; drafting anything into them would be inventing it.
/// </summary>
public static class ReportDrafts
{
    public static IReadOnlyDictionary<string, string> For(CommitteeReportType type, ProjectFacts facts) =>
        type switch
        {
            // The council's status report. Only the two sections that are wholly figures: key
            // activities, planned activities and issues are the author's account of the period.
            CommitteeReportType.StatusReport => new Dictionary<string, string>
            {
                ["schedule-update"] = facts.Programme(),
                ["cost-position"] = facts.Cost(),
            },

            // The gate report. Scope, conditions, the recommendation and the decision are the
            // whole substance of the document and none of them is a figure.
            CommitteeReportType.StageGateApproval => new Dictionary<string, string>
            {
                ["cost"] = facts.Cost(),
                ["programme"] = facts.Programme(),
                ["risks"] = facts.RisksAndIssues(),
            },

            // The monthly construction report, which is the document most often written by
            // retyping four registers. Progress, quality and safety stay with the author.
            CommitteeReportType.ConstructionProgressReport => new Dictionary<string, string>
            {
                ["programme"] = facts.Programme(),
                ["cost"] = facts.Cost(),
                ["change"] = facts.Change(),
                ["risks"] = facts.RisksAndIssues(),
            },

            CommitteeReportType.OutlineCostPlan or CommitteeReportType.DevelopedCostPlan =>
                new Dictionary<string, string> { ["against-budget"] = facts.Cost() },

            CommitteeReportType.ConceptDesignReport
                or CommitteeReportType.SpatialCoordinationReport
                or CommitteeReportType.TechnicalDesignReport =>
                new Dictionary<string, string>
                {
                    ["cost-programme"] = $"{facts.Cost()} {facts.Programme()}",
                },

            // Affordability is the figure; the evaluation, the qualifications and the
            // recommendation are the document.
            CommitteeReportType.TenderReport =>
                new Dictionary<string, string> { ["affordability"] = facts.Cost() },

            CommitteeReportType.HandoverAndCloseOutReport => new Dictionary<string, string>
            {
                ["cost"] = facts.Cost(),
                ["programme"] = facts.Programme(),
            },

            CommitteeReportType.FinalAccountStatement => new Dictionary<string, string>
            {
                ["adjustments"] = facts.Change(),
                ["against-budget"] = facts.Cost(),
            },

            // Only the arithmetic. What worked, what did not and what was not anticipated are the
            // entire point of the document, and a generated paragraph in any of them would be a
            // lesson nobody learned.
            CommitteeReportType.LessonsLearnedReport =>
                new Dictionary<string, string> { ["summary"] = facts.Summary() },

            // The committee-paper family, which predates the stage documents and keeps its
            // longstanding set.
            CommitteeReportType.CommitteeReport
                or CommitteeReportType.CabinetReport
                or CommitteeReportType.BoardReport
                or CommitteeReportType.CapitalProgrammeReport
                or CommitteeReportType.DecisionPaper =>
                new Dictionary<string, string>
                {
                    ["executive-summary"] =
                        $"This report provides an update on {facts.ProjectName} ({facts.ProjectRef}), "
                        + $"currently at RIBA Stage {facts.CurrentRibaStage}.",
                    ["finance-commentary"] = facts.Cost(),
                    ["programme-commentary"] = facts.Programme(),
                    ["risk-commentary"] = facts.Risk(),
                    ["stakeholder-commentary"] = $"{facts.Stakeholders} stakeholder(s) recorded on the register.",
                },

            // Everything else: the Stage 0–1 pack, which is written before there is anything to
            // report; the planning and pre-construction packs, whose content is not in the
            // platform; the certificates, whose figures are the certificate itself and belong in
            // a valuation table this does not yet draw; and the post-occupancy evaluation, whose
            // measured data the platform does not hold.
            _ => new Dictionary<string, string>(),
        };
}
