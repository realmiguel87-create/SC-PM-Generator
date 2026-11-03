using SCPM.Domain.Enums;

namespace SCPM.Domain.Common;

/// <summary>
/// Everything that distinguishes one RIBA stage document from another: the words in its header
/// band, the reference in its footer, the stage it belongs to, and its sections.
/// </summary>
/// <param name="Eyebrow">
/// The small spaced line above the title in the purple band. Names the service and the stage, so
/// a reader who picks up a loose page knows where in the process it sits.
/// </param>
/// <param name="RibaStage">
/// Shown in the facts block. A fixed string per document type rather than the project's current
/// stage: a Project Brief is a Stage 1 document for its whole life, including when it is reread
/// at Stage 5 to check what was asked for.
/// </param>
public record DocumentDefinition(
    string Eyebrow,
    string Title,
    string RibaStage,
    IReadOnlyList<ReportSection> Sections);

/// <summary>
/// The RIBA Stage 0–1 governance pack: the five documents a capital project needs before anything
/// is designed.
///
/// These are designed from the RIBA Plan of Work 2020 and standard public-sector practice, not
/// copied from council templates — the council has none for these. Two consequences, both
/// deliberate:
///
///   The Strategic Business Case follows the HM Treasury Five Case Model (strategic, economic,
///   commercial, financial, management), which the Scottish Capital Investment Manual also uses.
///   An invented structure would have been easier to write and useless to a reviewer who has read
///   a public-sector business case before.
///
///   No document carries a controlled-document number. PD.01.25 on the status report is the
///   council's own reference for its own template; inventing PD.02.25 for a document the council
///   has never issued would put a plausible but false reference on a controlled document. The
///   footer says so, and the number is for the council to assign.
///
/// Every heading carries a prompt. A template whose sections are only headings gets filled in
/// inconsistently by different authors, which is the problem these are meant to solve.
/// </summary>
public static class StageDocuments
{
    private const string Stage0 = "Infrastructure Delivery · RIBA Stage 0 — Strategic Definition";
    private const string Stage1 = "Infrastructure Delivery · RIBA Stage 1 — Preparation and Briefing";

    /// <summary>
    /// Authorises a project to be explored. Deliberately the shortest document in the pack: its
    /// job is to get agreement that a problem is worth spending time on, not to solve it. A
    /// mandate that takes a month to write has defeated its own purpose.
    /// </summary>
    public static readonly DocumentDefinition ProjectMandate = new(
        Stage0,
        "Project Mandate",
        "Stage 0 — Strategic Definition",
        [
            new("need", "Strategic Need",
                "The problem or opportunity in plain terms. What is failing, at risk, or unmet — "
                + "and what happens if nothing is done. Avoid naming a solution here."),
            new("strategic-fit", "Strategic Fit",
                "Which Council Plan outcome, service strategy or statutory duty this supports, and how."),
            new("scope", "Indicative Scope",
                "What is in scope and — just as important — what is explicitly out. Boundaries set "
                + "here are what later change control is measured against."),
            new("budget", "Indicative Budget and Funding",
                "The order of cost expected and where the funding would come from. A range is "
                + "appropriate at this stage; a single figure implies precision nobody has yet."),
            new("constraints", "Constraints and Assumptions",
                "Fixed dates, site or statutory constraints, and the assumptions the estimate rests "
                + "on. An assumption recorded here is a risk register entry later."),
            new("governance", "Governance and Roles",
                "Sponsor, project manager, approving body, and the level of authority being asked for."),
            new("recommendation", "Recommendation",
                "What the approver is being asked to agree, stated as a decision they can take."),
        ]);

    /// <summary>
    /// The HM Treasury Five Case Model. The five cases are in the Treasury's order and keep their
    /// standard names — a reviewer looks for "Economic Case" and expects options appraisal under
    /// it, and renaming it to something more descriptive would cost more in familiarity than it
    /// gained in clarity.
    /// </summary>
    public static readonly DocumentDefinition StrategicBusinessCase = new(
        Stage0,
        "Strategic Business Case",
        "Stage 0 — Strategic Definition",
        [
            new("strategic-case", "Strategic Case",
                "The case for change: current position, what is driving the need, the objectives "
                + "and the measurable benefits sought. This is the section that justifies doing "
                + "anything at all."),
            new("economic-case", "Economic Case",
                "Options considered — including do-nothing and do-minimum — how each was appraised, "
                + "and why the preferred option offers best value. An appraisal with one option is "
                + "not an appraisal."),
            new("commercial-case", "Commercial Case",
                "Procurement route and contract form (NEC4, SBCC, framework), market capacity, and "
                + "how risk is to be allocated between the council and the supply chain."),
            new("financial-case", "Financial Case",
                "Capital and revenue cost, funding sources, affordability across the capital "
                + "programme, and the whole-life cost consequences for the service budget."),
            new("management-case", "Management Case",
                "How the project will be delivered and controlled: governance, programme, key "
                + "risks, benefits realisation, and how success will be measured after handover."),
            new("recommendation", "Recommendation",
                "The decision sought, and what proceeding commits the council to."),
        ]);

    /// <summary>
    /// What the project must achieve. The document the design is tested against at every later
    /// stage, which is why it is written as requirements rather than as description.
    /// </summary>
    public static readonly DocumentDefinition ProjectBrief = new(
        Stage1,
        "Project Brief",
        "Stage 1 — Preparation and Briefing",
        [
            new("objectives", "Project Objectives",
                "What the completed project must achieve, written so that each objective could be "
                + "tested at handover. \"Improve the kitchen\" cannot be tested; \"serve 180 meals "
                + "within a 45-minute service\" can."),
            new("accommodation", "Accommodation and Spatial Requirements",
                "Spaces required, their areas, occupancy and adjacency needs. A schedule of "
                + "accommodation belongs here or as an appendix."),
            new("site", "Site and Existing Conditions",
                "Location, extent, tenure, existing buildings and known condition. Note surveys "
                + "already held and those still needed."),
            new("quality", "Quality Standards and Specification",
                "Standards the project must meet — statutory, council, and service-specific — and "
                + "the expected quality of finish and durability."),
            new("sustainability", "Sustainability and Net Zero",
                "Energy, carbon and climate-adaptation outcomes required, and the standard or "
                + "rating being targeted."),
            new("operational", "Operational Requirements",
                "How the building will be run, staffed, maintained and cleaned, and what the "
                + "service needs it to do day to day."),
            new("budget-programme", "Budget and Programme",
                "The cost ceiling and the dates the project must meet, including any fixed "
                + "constraint such as a term start or a funding deadline."),
            new("constraints", "Constraints and Exclusions",
                "Decanting, phasing, continuity of service, access restrictions, and what the "
                + "project will not deliver."),
            new("stakeholders", "Stakeholders and Consultation",
                "Who must be consulted, who must approve, and what consultation has already "
                + "informed this brief."),
        ]);

    /// <summary>
    /// Whether and how the brief can be met. Reaches a recommendation: a feasibility study that
    /// sets out options without choosing between them has moved the decision rather than
    /// supported it.
    /// </summary>
    public static readonly DocumentDefinition FeasibilityStudy = new(
        Stage1,
        "Feasibility Study Report",
        "Stage 1 — Preparation and Briefing",
        [
            new("purpose", "Purpose and Scope of Study",
                "What question this study was commissioned to answer, and what it deliberately "
                + "does not cover."),
            new("site-appraisal", "Site Appraisal and Surveys",
                "What the site investigation and surveys found, including anything that changes "
                + "the assumptions in the brief. Note surveys not yet done and their bearing on "
                + "confidence."),
            new("options", "Options Considered",
                "Each option described well enough to be understood on its own, including "
                + "do-nothing and do-minimum."),
            new("appraisal", "Options Appraisal",
                "How the options compare on cost, programme, operational fit, risk and carbon. "
                + "State the criteria and any weighting before the conclusion, not after it."),
            new("preferred", "Preferred Option",
                "The recommended option and the reasoning, including why the others were not "
                + "chosen. A reader should be able to disagree with the conclusion on the evidence "
                + "given."),
            new("cost-programme", "Cost and Programme Implications",
                "Estimated capital cost with its basis and confidence, the programme to completion, "
                + "and the revenue consequences."),
            new("risks", "Risks and Dependencies",
                "What could change the recommendation, and what the project depends on that it "
                + "does not control — consents, grid connections, other projects, funding decisions."),
            new("next-steps", "Recommendation and Next Steps",
                "The decision sought, and what is needed to move to Stage 2."),
        ]);

    /// <summary>
    /// How the project will be run. The longest document in the pack, and the one most often
    /// written once and never reread — hence sections that correspond to things that actually get
    /// done (change control, reporting cycle) rather than to management theory.
    /// </summary>
    public static readonly DocumentDefinition ProjectExecutionPlan = new(
        Stage1,
        "Project Execution Plan",
        "Stage 1 — Preparation and Briefing",
        [
            new("organisation", "Project Organisation and Roles",
                "Who holds each role — sponsor, project manager, cost adviser, designers, "
                + "principal designer — and what each is accountable for. Name roles and people."),
            new("governance", "Governance and Decision Making",
                "Which body approves what, at which stage gates, and the limits of the project "
                + "manager's own authority. State what must be escalated."),
            new("scope-deliverables", "Scope and Deliverables",
                "What the project will produce at each stage, and who accepts each deliverable."),
            new("programme", "Programme and Key Milestones",
                "The milestones the project will be measured against, including stage gates, "
                + "statutory consents and the baseline against which slip is reported."),
            new("cost-control", "Cost Control and Change Management",
                "How cost is reported and how often; who may approve a change at what value; and "
                + "how contingency is held and released."),
            new("risk", "Risk Management",
                "How risks are identified, scored, owned and reviewed, and the threshold at which "
                + "a risk is escalated."),
            new("procurement", "Procurement and Contract Strategy",
                "Route to market, contract form, how the works are packaged, and the timetable to "
                + "contract award."),
            new("quality", "Quality Management and Inspection",
                "How quality is specified, inspected and signed off, and what happens when work "
                + "does not comply."),
            new("communication", "Communication and Stakeholder Engagement",
                "Reporting cycle, who receives what and when, and how service users and the "
                + "community are kept informed."),
            new("health-safety", "Health, Safety and CDM",
                "CDM duty holders, how the client duties are discharged, and how safety is managed "
                + "on an operational site."),
            new("information", "Information Management",
                "Where project information is held, naming and version control, and what is "
                + "handed over as the asset information at Stage 6."),
            new("close-out", "Handover and Close-Out Strategy",
                "What completion requires, how the asset is handed to the service, and how "
                + "benefits and lessons are captured after handover."),
        ]);

    /// <summary>
    /// The pack, in the order a project meets it. Ordered so a caller listing the available
    /// documents presents them as a sequence rather than alphabetically.
    /// </summary>
    public static readonly IReadOnlyList<(CommitteeReportType Type, DocumentDefinition Definition)> All =
    [
        (CommitteeReportType.ProjectMandate, ProjectMandate),
        (CommitteeReportType.StrategicBusinessCase, StrategicBusinessCase),
        (CommitteeReportType.ProjectBrief, ProjectBrief),
        (CommitteeReportType.FeasibilityStudy, FeasibilityStudy),
        (CommitteeReportType.ProjectExecutionPlan, ProjectExecutionPlan),
    ];

    /// <summary>
    /// The definition for a stage document, or null for a report type that is not one. Null rather
    /// than a throw: callers use this to ask "is this a stage document?", and the status report and
    /// committee paper are legitimate answers of "no".
    /// </summary>
    public static DocumentDefinition? Find(CommitteeReportType type) =>
        All.FirstOrDefault(d => d.Type == type).Definition;
}
