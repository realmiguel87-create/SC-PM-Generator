using SCPM.Domain.Enums;

namespace SCPM.Domain.Common;

/// <summary>
/// Stage 2–4 design control and procurement documents.
///
/// Several entries on the client PM's list are not here, and the reason is the same each time:
/// the platform already holds the thing, so a template would be a second copy of it that can
/// disagree with the first. The risk register, opportunity register, change register, decision
/// register and value engineering log are registers; what they need is an export in this style,
/// not a document to retype them into. Programme updates are the programme module. A project
/// controls report is the status report under another name.
/// </summary>
public static partial class StageDocuments
{
    public static readonly DocumentDefinition DesignResponsibilityMatrix = new(
        Stage2, "Design Responsibility Matrix", "Stage 2 — Concept Design",
        [
            new("purpose", "Purpose and How to Read This",
                "What the matrix governs and what it does not. It allocates responsibility for "
                + "design, not for construction or for procurement."),
            new("elements", "Elements and Responsibility",
                "Each element of the works against the party responsible for its design, the "
                + "stage at which that responsibility begins, and who coordinates it with the "
                + "rest. The element both parties assumed the other was designing is the "
                + "commonest cause of a dispute, and this document exists to prevent it."),
            new("contractor-design", "Contractor-Designed Portions",
                "Which elements are reserved to a specialist or to the contractor, what "
                + "performance requirements govern each, and who accepts the design."),
            new("interfaces", "Interfaces",
                "Where one party's design meets another's, and who owns the junction. The "
                + "junction, not the elements either side of it, is where coordination fails."),
            new("acceptance", "Review and Acceptance",
                "How each party's design is reviewed and by whom, and what acceptance does and "
                + "does not transfer. Accepting a design is not taking responsibility for it, and "
                + "the document should say so."),
            new("warranties", "Design Warranties and Insurance",
                "Collateral warranties required, from whom, and the professional indemnity cover "
                + "each designer carries."),
        ]);

    public static readonly DocumentDefinition ValueEngineeringReport = new(
        Stage2, "Value Engineering Report", "Stage 2 — Concept Design",
        [
            new("context", "Why This Exercise Was Undertaken",
                "The gap being closed, in figures, and the date and basis of the estimate that "
                + "showed it. Value engineering with no target is cost cutting."),
            new("method", "Method and Participants",
                "How options were generated and appraised, and who took part. An exercise "
                + "conducted without the service that will run the building produces savings it "
                + "cannot live with."),
            new("options", "Options Identified",
                "Each option, its saving, and its effect on the brief's objectives, on whole-life "
                + "cost and on programme. A saving that raises running cost is a transfer, not a "
                + "saving."),
            new("recommended", "Recommended for Adoption",
                "Options recommended, with the total saving and what the project gives up to "
                + "achieve it."),
            new("rejected", "Considered and Rejected",
                "Options set aside and why. This is the section that stops the same idea being "
                + "proposed again at Stage 4 by someone who was not in the room."),
            new("risks", "Risks of the Recommended Options",
                "What could mean the saving is not realised, and what else each change affects."),
            new("decision", "Decision Sought",
                "What the approver is asked to adopt, and what adopting it changes in the brief."),
        ]);

    public static readonly DocumentDefinition UtilitiesDiversionStrategy = new(
        Stage2, "Utilities Diversion Strategy", "Stage 2 — Concept Design",
        [
            new("existing", "Existing Utilities",
                "What is on or under the site, from whose records, and the confidence in those "
                + "records. Statutory undertakers' records are indicative; say so, and say what "
                + "survey has verified them."),
            new("conflicts", "Conflicts With the Proposals",
                "Which apparatus conflicts with the design, and whether each requires diversion, "
                + "protection or abandonment."),
            new("options", "Diversion Options",
                "For each conflict, the options available and their relative cost and programme."),
            new("programme", "Lead Times and Programme",
                "Each undertaker's quotation and delivery lead time, and the latest date each "
                + "diversion must be complete. Utility diversions are among the longest lead "
                + "items on any project and are routinely found too late; state the dates."),
            new("cost", "Cost and Contributions",
                "Estimated cost per diversion, who bears it, and any cost-sharing under the "
                + "relevant legislation."),
            new("consents", "Consents and Wayleaves",
                "Road opening permits, wayleaves and easements required, and their own lead times."),
            new("risks", "Risks",
                "Unrecorded apparatus, refusal, programme slip, and the contingency held."),
        ]);

    // --- Stage 3 ---

    public static readonly DocumentDefinition DesignFreezeRecord = new(
        Stage3, "Design Freeze Record", "Stage 3 — Spatial Coordination",
        [
            new("scope", "What Is Being Frozen",
                "The elements, drawings and specification clauses frozen by this record, with "
                + "references and revisions. A freeze of \"the design\" freezes nothing."),
            new("rationale", "Why Now",
                "What depends on the freeze — procurement, long lead items, warrant submission — "
                + "and what the consequence of further change would be."),
            new("excluded", "Not Yet Frozen",
                "What remains open, when it will be frozen, and who owns it. The list of "
                + "exceptions is the part of a freeze that matters."),
            new("consultation", "Confirmation From Stakeholders",
                "Who has confirmed the frozen design meets their requirements, and when. A "
                + "freeze without the service's confirmation is a freeze that will thaw."),
            new("change", "Changing a Frozen Element",
                "The route a change must now take, who may authorise it, and the cost and "
                + "programme consequence of doing so."),
        ],
        ClosingPanel: ["Freeze Date", "Authorised By", "Next Review"]);

    public static readonly DocumentDefinition DesignReviewRecord = new(
        Stage3, "Design Review Record", "Stage 3 — Spatial Coordination",
        [
            new("review", "Review Held",
                "What was reviewed, against what, by whom, and when. Name the drawing revisions "
                + "reviewed: a review of an unidentified version cannot be relied on later."),
            new("against-brief", "Position Against the Brief",
                "Whether the design meets each relevant requirement of the Project Brief, and "
                + "where it does not."),
            new("comments", "Comments Raised",
                "Each comment, its originator, and its significance. Distinguish a requirement "
                + "from a preference — a review that does not is a review the design team cannot "
                + "act on."),
            new("responses", "Responses and Actions",
                "The design team's response to each comment, the action agreed, and who owns it."),
            new("outstanding", "Outstanding at Close of Review",
                "Comments not resolved, with an owner and a date, and whether any prevents the "
                + "next stage."),
            new("outcome", "Review Outcome",
                "Whether the design is accepted, accepted with comments, or to be resubmitted."),
        ],
        ClosingPanel: ["Review Date", "Chaired By", "Outcome"]);

    public static readonly DocumentDefinition BuildabilityReview = new(
        Stage3, "Buildability Review", "Stage 3 — Spatial Coordination",
        [
            new("scope", "Scope of Review",
                "What was reviewed and by whom, including any contractor or specialist input."),
            new("sequence", "Construction Sequence",
                "How the works are expected to be built, and whether the design supports that "
                + "sequence."),
            new("access", "Site Access and Logistics",
                "Craneage, deliveries, storage, welfare and site accommodation, and whether the "
                + "site can physically accommodate them alongside its continuing use."),
            new("temporary-works", "Temporary Works",
                "Propping, shoring, scaffolding and edge protection the design implies, and "
                + "anything the permanent design should be changed to simplify."),
            new("tolerances", "Tolerances and Interfaces",
                "Where tolerances between trades or materials are tight enough to cause problems "
                + "on site."),
            new("availability", "Material and Plant Availability",
                "Specified items with long lead times or single sources, and the alternatives."),
            new("findings", "Findings and Recommended Changes",
                "What should change in the design, with the benefit of each change and the cost "
                + "of making it now rather than on site."),
        ]);

    public static readonly DocumentDefinition CdmComplianceStatement = new(
        Stage3, "CDM Compliance Statement", "Stage 3 — Spatial Coordination",
        [
            new("project", "Project and Notification",
                "The project, whether it is notifiable, and the date the F10 was or will be "
                + "submitted."),
            new("duty-holders", "Duty Holders",
                "Client, principal designer, designers, principal contractor and contractors, "
                + "named, with the date each was appointed. An appointment made late is a duty "
                + "undischarged in the meantime."),
            new("client-duties", "How the Client Duties Are Discharged",
                "The arrangements the client has made for managing the project, and how the "
                + "client satisfies itself those arrangements are working — not merely that they "
                + "exist."),
            new("competence", "Skills, Knowledge and Experience",
                "How each appointee's capability was assessed, and on what evidence."),
            new("design-risk", "Design Risk Management",
                "How significant risks are being eliminated or reduced in design, and how "
                + "residual risks are being communicated."),
            new("information", "Pre-Construction Information",
                "What has been provided to designers and contractors, when, and what remains "
                + "outstanding."),
            new("phase-plan", "Construction Phase Plan",
                "The arrangements for its preparation and review before construction begins."),
        ]);

    public static readonly DocumentDefinition TenderEvaluationCriteria = new(
        Stage3, "Tender Evaluation Criteria", "Stage 3 — Spatial Coordination",
        [
            new("objectives", "What the Evaluation Must Select For",
                "The outcomes the procurement strategy set, restated as what a winning tender "
                + "must demonstrate."),
            new("criteria", "Criteria and Weightings",
                "Each criterion, its weighting, and the sub-criteria beneath it. Published before "
                + "tenders are invited and not changed afterwards — criteria adjusted once "
                + "returns are in is the single most common ground of challenge."),
            new("scoring", "Scoring Methodology",
                "The scale, what each score means in words, and how price and quality scores are "
                + "combined. A scale without descriptors produces scores that cannot be defended."),
            new("price", "Price Evaluation",
                "How the price score is calculated, how abnormally low tenders are handled, and "
                + "how non-compliant pricing is treated."),
            new("panel", "Evaluation Panel and Process",
                "Who evaluates, whether scoring is individual then moderated, and how "
                + "conflicts of interest are declared and managed."),
            new("clarification", "Clarifications",
                "What may be clarified without amending a tender, and the line beyond which a "
                + "clarification becomes a post-tender negotiation."),
            new("record", "Record and Feedback",
                "What is recorded, and the feedback unsuccessful tenderers will receive."),
        ]);

    // --- Stage 4 ---

    public static readonly DocumentDefinition ChangeControlProcedure = new(
        Stage4, "Change Control Procedure", "Stage 4 — Technical Design",
        [
            new("what-is-change", "What Counts as a Change",
                "The baseline each change is measured against — the frozen design, the approved "
                + "budget, the baseline programme — identified by reference and date. Without a "
                + "stated baseline there is no such thing as a change."),
            new("origination", "Raising a Change",
                "Who may raise one, what information is required, and where it is logged."),
            new("assessment", "Assessment",
                "How cost, programme, risk and brief impact are assessed, by whom, and within "
                + "what period. Include the effect on whole-life cost: a change assessed on "
                + "capital alone is assessed on half the facts."),
            new("authority", "Authority to Approve",
                "Who may approve a change at what value, and what must be escalated. Figures, "
                + "not descriptions."),
            new("contract", "Contractual Route",
                "How an approved change is instructed under the contract in use, and what must "
                + "happen before work proceeds. Under NEC a compensation event quotation is "
                + "normally accepted before the work; instructing first and pricing later is how "
                + "a project loses control of its final account."),
            new("records", "Recording and Reporting",
                "Where approved changes are recorded, how the budget and programme are updated, "
                + "and how change is reported."),
            new("rejected", "Rejected and Withdrawn Changes",
                "That these are recorded too, and why: a change rejected and not recorded is a "
                + "change that will be raised again."),
        ]);

    public static readonly DocumentDefinition ConstructionPhasePlanReview = new(
        Stage4, "Construction Phase Plan Review", "Stage 4 — Technical Design",
        [
            new("plan", "Plan Reviewed",
                "The plan's reference, revision and date, and who prepared it."),
            new("adequacy", "Adequacy for the Work",
                "Whether the plan addresses this project's actual hazards, or is a generic "
                + "document with the site name changed. The latter is common and is the point of "
                + "this review."),
            new("site-specific", "Site-Specific Arrangements",
                "How the plan deals with what makes this site particular — occupied premises, "
                + "children or vulnerable users on site, restricted access, live services."),
            new("high-risk", "High-Risk Activities",
                "Whether the plan identifies the significant risks from the pre-construction "
                + "information and says how each is to be controlled."),
            new("interfaces", "Interface With the Client's Operations",
                "Segregation, hours, noise, dust, traffic and emergency arrangements where the "
                + "building remains in use."),
            new("findings", "Findings",
                "What is adequate, what must be amended before work starts, and what is to be "
                + "developed as the work proceeds."),
            new("conclusion", "Conclusion",
                "Whether the client is satisfied the plan is adequate for construction to begin. "
                + "This is a client duty and the answer is yes or not yet."),
        ],
        ClosingPanel: ["Review Date", "Reviewed By", "Outcome"]);

    /// <summary>Stage 2–4 design control and procurement.</summary>
    private static IReadOnlyList<(CommitteeReportType Type, DocumentDefinition Definition)> DesignPack =>
    [
        (CommitteeReportType.DesignResponsibilityMatrix, DesignResponsibilityMatrix),
        (CommitteeReportType.ValueEngineeringReport, ValueEngineeringReport),
        (CommitteeReportType.UtilitiesDiversionStrategy, UtilitiesDiversionStrategy),
        (CommitteeReportType.DesignFreezeRecord, DesignFreezeRecord),
        (CommitteeReportType.DesignReviewRecord, DesignReviewRecord),
        (CommitteeReportType.BuildabilityReview, BuildabilityReview),
        (CommitteeReportType.CdmComplianceStatement, CdmComplianceStatement),
        (CommitteeReportType.TenderEvaluationCriteria, TenderEvaluationCriteria),
        (CommitteeReportType.ChangeControlProcedure, ChangeControlProcedure),
        (CommitteeReportType.ConstructionPhasePlanReview, ConstructionPhasePlanReview),
    ];
}
