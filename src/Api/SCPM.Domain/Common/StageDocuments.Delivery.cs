using SCPM.Domain.Enums;

namespace SCPM.Domain.Common;

/// <summary>
/// RIBA Stages 2–7: design, construction, handover and use.
///
/// Separated from the Stage 0–1 pack by file only — the catalogue is one list, and
/// <see cref="StageDocuments.All"/> holds all of it. Seventeen documents in one file alongside the
/// others would be a file nobody reads to the end.
///
/// Four of these are not narrative documents. A payment certificate, a practical completion
/// certificate, a change instruction and a health and safety file index are forms: labelled fields
/// certifying something, not prose under headings. They carry a closing panel for what they
/// certify, the same mechanism the gate report uses to record a decision. What they do not yet
/// have is a table of figures, which a payment certificate genuinely wants — recorded as an open
/// item rather than approximated with prose.
/// </summary>
public static partial class StageDocuments
{
    private const string Stage2 = "Infrastructure Delivery · RIBA Stage 2 — Concept Design";
    private const string Stage3 = "Infrastructure Delivery · RIBA Stage 3 — Spatial Coordination";
    private const string Stage4 = "Infrastructure Delivery · RIBA Stage 4 — Technical Design";
    private const string Stage5 = "Infrastructure Delivery · RIBA Stage 5 — Manufacturing and Construction";
    private const string Stage6 = "Infrastructure Delivery · RIBA Stage 6 — Handover";
    private const string Stage7 = "Infrastructure Delivery · RIBA Stage 7 — Use";

    // --- Stage 2: Concept Design ---

    public static readonly DocumentDefinition ConceptDesignReport = new(
        Stage2, "Concept Design Report", "Stage 2 — Concept Design",
        [
            new("design-response", "Design Response to the Brief",
                "How the concept answers each objective in the Project Brief. Where it departs "
                + "from the brief, say so here rather than leaving the reader to notice."),
            new("spatial", "Spatial Arrangement",
                "The arrangement of spaces, their areas against the schedule of accommodation, and "
                + "how people and goods move through the building."),
            new("appearance", "Appearance and Materials",
                "The intended character and the principal materials, with the reasoning — "
                + "durability, maintenance, planning context — rather than only the choice."),
            new("structure-services", "Structural and Services Strategy",
                "The structural approach and the engineering services concept, including plant "
                + "space and where services run."),
            new("sustainability", "Sustainability and Net Zero",
                "How the concept meets the energy, carbon and adaptation outcomes the brief set, "
                + "and the standard being targeted."),
            new("planning", "Planning and Statutory Considerations",
                "What the concept implies for planning, building warrant, roads and any consent "
                + "with a long lead time."),
            new("cost-programme", "Cost and Programme",
                "The concept's cost against the budget, and its effect on the programme. A concept "
                + "that cannot be afforded is not a concept, however good it is."),
            new("options", "Options Discounted",
                "Concepts considered and set aside, and why. This is what stops the same option "
                + "being proposed again at Stage 3 by someone who was not in the room."),
        ]);

    public static readonly DocumentDefinition OutlineCostPlan = new(
        Stage2, "Outline Cost Plan", "Stage 2 — Concept Design",
        [
            new("basis", "Basis of Estimate",
                "What the estimate is built from — rates, benchmarks, measured quantities — and "
                + "the price base date. An estimate without its basis cannot be checked or updated."),
            new("summary", "Cost Summary",
                "The estimate by element or work package, totalling to the figure quoted. Show "
                + "construction cost, fees, and non-works costs separately."),
            new("against-budget", "Position Against Budget",
                "The estimate against the approved budget, and the variance. State plainly whether "
                + "the concept is affordable as drawn."),
            new("exclusions", "Exclusions and Assumptions",
                "What is not in the figure — decant, abnormals, VAT, inflation beyond the base "
                + "date — and the assumptions it rests on. Most cost disputes start here."),
            new("risk-allowance", "Risk and Contingency",
                "Design development risk, construction risk and contingency, each with its basis. "
                + "A single unexplained percentage is not a risk allowance."),
            new("inflation", "Inflation",
                "The allowance to the midpoint of construction, the index used, and the programme "
                + "dates it assumes."),
            new("whole-life", "Whole-Life and Revenue Implications",
                "Running, maintenance and lifecycle cost consequences for the service budget."),
        ]);

    // --- Stage 3: Spatial Coordination ---

    public static readonly DocumentDefinition SpatialCoordinationReport = new(
        Stage3, "Spatial Coordination Report", "Stage 3 — Spatial Coordination",
        [
            new("coordination", "Coordination Status",
                "What has been coordinated between architecture, structure and services, and what "
                + "has not. Name the clashes still open and who owns each."),
            new("changes", "Changes Since Concept Design",
                "What has changed since Stage 2 and why. A reader who approved the concept should "
                + "be able to see what they are now looking at instead."),
            new("surveys", "Surveys and Site Information",
                "Surveys completed since Stage 2 and what they found, including anything that "
                + "contradicts an earlier assumption."),
            new("services", "Engineering Services Coordination",
                "Plant, risers, distribution routes and the space they occupy, with any conflict "
                + "that is not yet resolved."),
            new("compliance", "Statutory and Standards Compliance",
                "Building warrant, fire strategy, accessibility and the standards the brief set, "
                + "with the position on each."),
            new("cost-programme", "Cost and Programme Position",
                "The effect of coordination on cost and programme since Stage 2."),
            new("outstanding", "Outstanding Matters for Stage 4",
                "What must be resolved before technical design can proceed, and by whom."),
        ]);

    public static readonly DocumentDefinition PlanningApplicationSummary = new(
        Stage3, "Planning Application Summary", "Stage 3 — Spatial Coordination",
        [
            new("proposal", "The Proposal",
                "What is being applied for, in the terms the application uses, and the application "
                + "type — full, outline, listed building, advertisement."),
            new("policy", "Policy Context",
                "The development plan policies the proposal engages and how it accords with them."),
            new("consultation", "Pre-Application and Consultation",
                "Pre-application advice received, community consultation undertaken, and what "
                + "changed as a result."),
            new("documents", "Documents Submitted",
                "The drawings and supporting statements forming the application, by reference and "
                + "revision — so the submission can be reconstructed later."),
            new("issues", "Anticipated Issues",
                "The objections or officer concerns expected, and the response to each."),
            new("programme", "Determination Programme",
                "Validation, consultation period, target determination date, and what the project "
                + "programme assumes about it."),
            new("conditions", "Likely Conditions",
                "Conditions expected and what discharging each would require, so the cost and time "
                + "of compliance are known before consent rather than after."),
        ]);

    public static readonly DocumentDefinition DevelopedCostPlan = new(
        Stage3, "Developed Cost Plan", "Stage 3 — Spatial Coordination",
        [
            new("basis", "Basis of Estimate",
                "What the estimate is measured from — drawing references and revisions — and the "
                + "price base date."),
            new("summary", "Cost Summary",
                "The estimate by element, totalling to the figure quoted, with construction cost, "
                + "fees and non-works costs shown separately."),
            new("movement", "Movement Since the Outline Plan",
                "Every material change since Stage 2, attributed to a cause: design development, "
                + "scope change, market movement, or correction of an error. An unexplained "
                + "movement is the one a reader stops at."),
            new("against-budget", "Position Against Budget",
                "The estimate against the approved budget and the variance, with the action "
                + "proposed if it is adverse."),
            new("exclusions", "Exclusions and Assumptions",
                "What is not in the figure, and what it assumes."),
            new("risk-allowance", "Risk and Contingency",
                "Remaining design risk, construction risk and contingency, each with its basis. "
                + "The design risk allowance should be falling as the design develops."),
            new("value", "Value Engineering",
                "Savings identified, their effect on the brief's objectives, and which are "
                + "recommended. Record what was rejected as well as what was taken."),
            new("whole-life", "Whole-Life and Revenue Implications",
                "Running, maintenance and lifecycle cost consequences, updated."),
        ]);

    // --- Stage 4: Technical Design ---

    public static readonly DocumentDefinition TechnicalDesignReport = new(
        Stage4, "Technical Design Report", "Stage 4 — Technical Design",
        [
            new("completeness", "Design Completeness",
                "What is complete, what is outstanding, and what is reserved to a specialist "
                + "contractor's design. Name the specialist packages and who coordinates them."),
            new("changes", "Changes Since Spatial Coordination",
                "What has changed since Stage 3 and why."),
            new("warrant", "Building Warrant and Statutory Approvals",
                "Warrant application status, conditions, and any approval still outstanding with "
                + "its effect on the construction programme."),
            new("specification", "Specification and Standards",
                "How the specification meets the brief's quality standards, and any derogation "
                + "with the reason it was accepted."),
            new("buildability", "Buildability and Construction Sequence",
                "How the design is to be built, including temporary works, site constraints and "
                + "anything the sequence depends on."),
            new("cdm", "Health and Safety in Design",
                "Significant residual risks the design leaves to construction, use and eventual "
                + "demolition, and how each has been reduced so far as reasonably practicable."),
            new("cost-programme", "Cost and Programme Position",
                "The effect of technical design on cost and programme, and readiness to tender."),
        ]);

    public static readonly DocumentDefinition TenderReport = new(
        Stage4, "Tender Report and Recommendation", "Stage 4 — Technical Design",
        [
            new("process", "Tender Process",
                "Route to market, framework or OJEU-equivalent procedure used, how the shortlist "
                + "was arrived at, and the dates of each step. This is the section an audit reads "
                + "first."),
            new("returns", "Tenders Received",
                "Who tendered, who declined and why, and the returns as submitted — before any "
                + "adjustment."),
            new("evaluation", "Evaluation",
                "The criteria and weightings as published, and the scores. State the criteria "
                + "before the result: criteria that appear after it read as having been fitted to it."),
            new("qualifications", "Qualifications and Clarifications",
                "Qualifications submitted, clarifications sought, and how each was resolved — "
                + "including any adjustment made to a tender figure and its justification."),
            new("affordability", "Affordability",
                "The recommended tender against the budget, the variance, and the action proposed "
                + "where it is adverse."),
            new("risks", "Risks in the Recommended Tender",
                "What the tender assumes, what it excludes, and the contractual risks accepted by "
                + "proceeding."),
            new("recommendation", "Recommendation",
                "The tenderer recommended, the contract sum, and the authority sought to award."),
        ]);

    public static readonly DocumentDefinition PreConstructionInformation = new(
        Stage4, "Pre-Construction Information", "Stage 4 — Technical Design",
        [
            new("project", "Project Description and Programme",
                "The works, the site, the key dates and the planned construction period."),
            new("client", "Client Duties and Arrangements",
                "The client's arrangements for managing the project, the duty holders appointed, "
                + "and how the client will ensure the CDM duties are met."),
            new("existing", "Existing Site Information",
                "Existing drawings, surveys, services information and condition reports available "
                + "to the contractor, and where they are held."),
            new("hazards", "Significant Hazards",
                "Asbestos, contamination, unstable structures, overhead and buried services, "
                + "traffic, and anything the site's use imposes — a school or a care home in "
                + "operation alongside the works is a hazard, not a constraint."),
            new("design-risks", "Significant Design Risks",
                "Residual risks the design cannot design out, carried forward from the designers' "
                + "risk assessments."),
            new("site-rules", "Site Rules and Constraints",
                "Access, hours, noise, parking, deliveries, permits, and the rules the site's "
                + "continued operation imposes."),
            new("hs-file", "Health and Safety File",
                "What the file is to contain, in what format, and when it must be delivered."),
        ]);

    // --- Stage 5: Manufacturing and Construction ---

    public static readonly DocumentDefinition ConstructionProgressReport = new(
        Stage5, "Construction Progress Report", "Stage 5 — Manufacturing and Construction",
        [
            new("progress", "Progress This Period",
                "What was completed, against what was planned. Describe the work, not the "
                + "percentage: \"roof watertight to grid 4\" tells a reader more than \"62%\"."),
            new("planned", "Planned Next Period",
                "What is to be completed before the next report, and what it depends on."),
            new("programme", "Programme Position",
                "Current forecast completion against the contract completion date, the float "
                + "remaining, and the cause of any slip."),
            new("cost", "Cost Position",
                "Certified to date, forecast final account, and movement since the last report."),
            new("change", "Change and Compensation Events",
                "Instructions issued, compensation events or variations notified, and their "
                + "assessed or estimated effect on cost and time."),
            new("quality", "Quality and Inspection",
                "Inspections carried out, non-conformances raised and closed, and anything "
                + "awaiting rectification."),
            new("safety", "Health and Safety",
                "Incidents, near misses, inspections and any enforcement contact."),
            new("risks", "Risks and Issues",
                "What threatens completion, cost or quality, with the action in hand."),
        ]);

    public static readonly DocumentDefinition PaymentCertificate = new(
        Stage5, "Payment Certificate", "Stage 5 — Manufacturing and Construction",
        [
            new("basis", "Basis of Assessment",
                "The contract clause under which this assessment is made, the assessment date, and "
                + "the application or valuation it responds to."),
            new("works", "Value of Work Completed",
                "Measured work, materials on and off site, and the basis for each. Attach the "
                + "valuation build-up rather than summarising it away."),
            new("adjustments", "Adjustments",
                "Variations, compensation events, fluctuations, retention, liquidated damages and "
                + "any set-off, each with its reason."),
            new("notices", "Notices",
                "Any payment notice or pay-less notice associated with this certificate, and the "
                + "dates they were served. A late notice changes what is due."),
        ],
        ClosingPanel: ["Certificate No", "Amount Certified", "Final Date for Payment"]);

    public static readonly DocumentDefinition ChangeInstruction = new(
        Stage5, "Change Instruction", "Stage 5 — Manufacturing and Construction",
        [
            new("instruction", "Instruction",
                "What the contractor is instructed to do, precisely enough to be priced and built "
                + "from. Reference the drawings and revisions it relies on."),
            new("reason", "Reason for Change",
                "Why the change is needed, and its origin — client requirement, design "
                + "development, site condition, statutory requirement, error. The origin decides "
                + "who bears the cost, so recording it late is recording it in dispute."),
            new("contract", "Contractual Basis",
                "The clause under which the instruction is issued, and whether it is a "
                + "compensation event, a variation, or neither."),
            new("cost-time", "Effect on Cost and Time",
                "The assessed or estimated effect on the contract sum and completion date, or the "
                + "basis on which it will be assessed if not yet known."),
            new("risk", "Consequences and Risks",
                "What else this change affects — other packages, warranties, statutory approvals "
                + "— and anything it forecloses."),
        ],
        ClosingPanel: ["Instruction No", "Issued By", "Date Issued"]);

    // --- Stage 6: Handover ---

    public static readonly DocumentDefinition PracticalCompletionCertificate = new(
        Stage6, "Practical Completion Certificate", "Stage 6 — Handover",
        [
            new("works", "The Works Certified",
                "What is certified complete, and any part of the works excluded from this "
                + "certificate."),
            new("defects", "Outstanding Items and Defects",
                "Items outstanding at completion, with the date each is to be rectified. A "
                + "certificate issued over a long list is a certificate that will be argued about."),
            new("consequences", "Consequences of Certification",
                "What certification triggers under the contract — insurance passing to the client, "
                + "retention release, liquidated damages ceasing, the defects period starting."),
            new("handover", "Documents and Keys Handed Over",
                "What has been handed over at completion and what remains outstanding, including "
                + "the health and safety file and the operation and maintenance manuals."),
        ],
        ClosingPanel: ["Date of Practical Completion", "Certified By", "Defects Period Ends"]);

    public static readonly DocumentDefinition HandoverAndCloseOutReport = new(
        Stage6, "Handover and Close-Out Report", "Stage 6 — Handover",
        [
            new("delivered", "What Was Delivered",
                "The completed asset against the Project Brief's objectives, objective by "
                + "objective. This is where the brief is finally answered."),
            new("cost", "Final Cost Position",
                "Outturn cost against the approved budget, with the movement explained."),
            new("programme", "Final Programme Position",
                "Actual completion against the approved baseline, with the slip explained."),
            new("commissioning", "Commissioning and Testing",
                "What was commissioned, what was witnessed, and any system still to be proven "
                + "under full occupancy."),
            new("training", "Training and Familiarisation",
                "Training delivered to the service and facilities staff, who received it, and "
                + "what remains."),
            new("documentation", "Asset Information and Documentation",
                "O&M manuals, as-built information, warranties, certificates and the health and "
                + "safety file — what was handed over and where it is held."),
            new("defects", "Defects and Aftercare",
                "The defects process, who to contact, and the arrangements for the defects period."),
            new("outstanding", "Outstanding Matters",
                "Anything not closed at handover, with an owner and a date."),
        ]);

    public static readonly DocumentDefinition HealthAndSafetyFileIndex = new(
        Stage6, "Health and Safety File Index", "Stage 6 — Handover",
        [
            new("scope", "Scope and Purpose",
                "The structure the file covers and the work it records. The file is for whoever "
                + "maintains, alters or demolishes this building — write the index for them, not "
                + "for the project team."),
            new("residual-risks", "Residual Hazards",
                "Hazards that remain in the completed building, and where in the file each is "
                + "described. This is the section the file exists for."),
            new("contents", "Contents",
                "The file's sections and what each contains, by document reference and revision."),
            new("asbuilt", "As-Built Information",
                "As-built drawings and models, with references, revisions and where they are held."),
            new("maintenance", "Maintenance and Cleaning",
                "Information on safe maintenance, cleaning and access to plant, and anything "
                + "requiring specialist access equipment."),
            new("location", "Location and Custody",
                "Where the file is held, in what format, and who is responsible for keeping it "
                + "current when the building changes."),
        ],
        ClosingPanel: ["File Reference", "Compiled By", "Date Issued"]);

    // --- Stage 7: Use ---

    public static readonly DocumentDefinition PostOccupancyEvaluation = new(
        Stage7, "Post-Occupancy Evaluation", "Stage 7 — Use",
        [
            new("scope", "Scope and Method",
                "When the evaluation was carried out, how long after occupation, and how — "
                + "surveys, interviews, metered data, walkthroughs."),
            new("objectives", "Performance Against the Brief",
                "Each objective in the Project Brief, and whether the building in use meets it. "
                + "The objectives were written to be testable; this is the test."),
            new("energy", "Energy and Environmental Performance",
                "Measured consumption against the design prediction, and the reasons for any gap. "
                + "A performance gap is normal; an unexplained one is a finding."),
            new("occupant", "Occupant Experience",
                "What users and staff report — comfort, noise, light, usability — and how widely "
                + "each view is held."),
            new("operation", "Operation and Maintenance",
                "How the building is proving to run and maintain, including anything the "
                + "facilities team has had to work around."),
            new("findings", "Findings",
                "What this building teaches, stated so it can be acted on by someone who was not "
                + "involved in it."),
            new("actions", "Recommended Actions",
                "Remedial actions for this building, and changes recommended to the standard brief "
                + "for the next one, each with an owner."),
        ]);

    public static readonly DocumentDefinition FinalAccountStatement = new(
        Stage7, "Final Account Statement", "Stage 7 — Use",
        [
            new("summary", "Final Account Summary",
                "Contract sum, adjustments, and the final account total, reconciling to the figure "
                + "agreed."),
            new("adjustments", "Adjustments to the Contract Sum",
                "Variations, compensation events, provisional sum adjustments, fluctuations and "
                + "loss and expense, each with its reference and value."),
            new("claims", "Claims and Their Settlement",
                "Claims made, how each was assessed, and the basis on which it was settled or "
                + "rejected. Where a settlement was a compromise, say so — a reader reconstructing "
                + "the figure later cannot derive it from the contract alone."),
            new("damages", "Liquidated Damages and Retention",
                "Damages applied or waived, retention released, and the dates."),
            new("against-budget", "Outturn Against Budget",
                "The final account against the approved budget, with the movement attributed."),
            new("outstanding", "Outstanding Matters",
                "Anything unresolved at final account, including disputes and any provision "
                + "carried."),
        ]);

    public static readonly DocumentDefinition LessonsLearnedReport = new(
        Stage7, "Lessons Learned Report", "Stage 7 — Use",
        [
            new("summary", "Project Summary",
                "What the project was, what it cost, how long it took, and how those compare with "
                + "what was approved at the outset."),
            new("worked", "What Worked",
                "Decisions, arrangements and people that made the difference, described specifically "
                + "enough to be repeated."),
            new("did-not", "What Did Not Work",
                "Written plainly and without attributing blame to individuals. A lessons report "
                + "that is careful about feelings teaches nothing, and a project that learns "
                + "nothing repeats this one."),
            new("surprises", "What Was Not Anticipated",
                "Risks that materialised and were not on the register, and why they were missed."),
            new("estimating", "Estimating and Programme Accuracy",
                "Where the early estimates and programme proved accurate and where they did not, "
                + "with the figures — this is the section that improves the next business case."),
            new("recommendations", "Recommendations",
                "Changes recommended to the council's process, standards or templates, each with "
                + "an owner and a route to being adopted. A recommendation with no owner is an "
                + "observation."),
        ]);

    /// <summary>
    /// Stages 2–7, in the order a project meets them.
    ///
    /// A property rather than a field: static field initialisation order across the files of a
    /// partial type follows compilation order, so the catalogue in the other file could read this
    /// before it was assigned. A property is evaluated when asked for, which makes the order
    /// irrelevant. It is read once, through All's Lazy.
    /// </summary>
    private static IReadOnlyList<(CommitteeReportType Type, DocumentDefinition Definition)> Delivery =>
    [
        (CommitteeReportType.ConceptDesignReport, ConceptDesignReport),
        (CommitteeReportType.OutlineCostPlan, OutlineCostPlan),
        (CommitteeReportType.SpatialCoordinationReport, SpatialCoordinationReport),
        (CommitteeReportType.PlanningApplicationSummary, PlanningApplicationSummary),
        (CommitteeReportType.DevelopedCostPlan, DevelopedCostPlan),
        (CommitteeReportType.TechnicalDesignReport, TechnicalDesignReport),
        (CommitteeReportType.TenderReport, TenderReport),
        (CommitteeReportType.PreConstructionInformation, PreConstructionInformation),
        (CommitteeReportType.ConstructionProgressReport, ConstructionProgressReport),
        (CommitteeReportType.PaymentCertificate, PaymentCertificate),
        (CommitteeReportType.ChangeInstruction, ChangeInstruction),
        (CommitteeReportType.PracticalCompletionCertificate, PracticalCompletionCertificate),
        (CommitteeReportType.HandoverAndCloseOutReport, HandoverAndCloseOutReport),
        (CommitteeReportType.HealthAndSafetyFileIndex, HealthAndSafetyFileIndex),
        (CommitteeReportType.PostOccupancyEvaluation, PostOccupancyEvaluation),
        (CommitteeReportType.FinalAccountStatement, FinalAccountStatement),
        (CommitteeReportType.LessonsLearnedReport, LessonsLearnedReport),
    ];
}
