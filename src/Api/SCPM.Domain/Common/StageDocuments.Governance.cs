using SCPM.Domain.Enums;

namespace SCPM.Domain.Common;

/// <summary>
/// Stage 0–1 governance, strategy and briefing documents, from the client PM's master list.
///
/// Two of these deliberately combine entries on that list, because each pair is one document in
/// practice and two near-identical templates would be worse than one good one:
///
///   Governance Structure and the Delegation of Authority Matrix. A governance structure that
///   does not say who may approve what is a diagram, and a delegation matrix with no structure
///   around it is a table nobody can interpret.
///
///   Sustainability Strategy and Net Zero / Carbon Strategy. Carbon is the measurable part of
///   sustainability, not a separate subject; splitting them produces two documents that each
///   claim the other's content.
///
/// Where the list named a document after a contract form — the NEC Scope, the SBCC Employer's
/// Requirements — the template is for the content the client writes, not for the contract's own
/// published form. The forms themselves are NEC's and SBCC's instruments and are not reproduced
/// here: a document that looked like Contract Data but was not the published form would
/// eventually be executed by somebody.
/// </summary>
public static partial class StageDocuments
{
    public static readonly DocumentDefinition StakeholderEngagementPlan = new(
        Stage0, "Stakeholder Engagement Plan", "Stage 0 — Strategic Definition",
        [
            new("stakeholders", "Stakeholders and Their Interest",
                "Who is affected and who has influence, and what each cares about. Record the "
                + "interest rather than the job title: \"needs the hall available for exams in "
                + "May\" can be planned around, \"Head Teacher\" cannot."),
            new("objectives", "Engagement Objectives",
                "What engagement is meant to achieve — consent, information, requirements, or "
                + "simply warning. Different objectives need different methods and different "
                + "amounts of time."),
            new("approach", "Approach by Group",
                "How each group will be engaged, how often, and by whom. Say which groups are "
                + "being consulted and which informed; conflating the two is how a consultation "
                + "produces anger rather than agreement."),
            new("statutory", "Statutory and Policy Requirements",
                "Consultation the council is required to carry out, and the standard it must "
                + "meet. These are deadlines, not intentions."),
            new("concerns", "Anticipated Concerns",
                "What objections are expected, from whom, and the response to each. An objection "
                + "foreseen is a plan; one that arrives unforeseen is a delay."),
            new("feedback", "Recording and Acting on Feedback",
                "How what is heard will be recorded, who decides what to act on, and how people "
                + "will be told what changed. Engagement that visibly changes nothing teaches "
                + "people not to engage next time."),
            new("responsibilities", "Roles and Responsibilities",
                "Who speaks for the project, who approves communications, and who holds the "
                + "stakeholder register current."),
        ]);

    public static readonly DocumentDefinition GovernanceAndDelegationFramework = new(
        Stage0, "Governance and Delegation Framework", "Stage 0 — Strategic Definition",
        [
            new("structure", "Governance Structure",
                "The bodies involved — project board, programme board, committee — what each "
                + "decides, and how often it meets. Name the body that can stop the project."),
            new("roles", "Roles and Accountabilities",
                "Sponsor, project manager, cost adviser, designers and the client's own officers, "
                + "with what each is accountable for rather than what each is called."),
            new("delegation", "Delegated Authority",
                "Who may approve what, to what value, and what must be escalated. State the "
                + "limits as figures: an authority described in words is an authority argued "
                + "about at the point it matters."),
            new("decisions", "Decision Routes",
                "The route each class of decision takes — change, spend, programme, risk "
                + "acceptance — and how long each route takes. A decision route with no timescale "
                + "is a programme risk nobody has priced."),
            new("escalation", "Escalation",
                "What triggers escalation, to whom, and within what period. Thresholds in "
                + "figures, so a threshold is crossed rather than debated."),
            new("assurance", "Assurance and Review",
                "Gateway reviews, internal audit, and any external assurance the funder requires, "
                + "with when each falls."),
            new("records", "Records of Decisions",
                "Where decisions are recorded, by whom, and how a decision is later evidenced. "
                + "The decision register is the record; minutes are not."),
        ]);

    public static readonly DocumentDefinition FundingApprovalPaper = new(
        Stage0, "Funding Approval Paper", "Stage 0 — Strategic Definition",
        [
            new("request", "Approval Sought",
                "The sum sought, the financial years it falls in, and the authority being asked "
                + "to approve it. State the decision, not the context."),
            new("need", "Case for the Expenditure",
                "What the money buys and what happens without it, in the terms the approving "
                + "body uses. A technical case presented to a finance committee persuades nobody."),
            new("sources", "Funding Sources",
                "Capital allocation, grant, prudential borrowing, receipts or contribution, with "
                + "the conditions attached to each and the date each must be committed or spent "
                + "by. A grant with a spend deadline is a programme constraint."),
            new("profile", "Spend Profile",
                "Expenditure by financial year against the funding available in each. A scheme "
                + "affordable in total and unaffordable in year two is unaffordable."),
            new("revenue", "Revenue Consequences",
                "The ongoing cost to the service budget — running, maintenance, lifecycle, "
                + "staffing — and whose budget carries it. The commonest omission in a capital "
                + "paper, and the one that surfaces after the building opens."),
            new("risks", "Financial Risks",
                "What could change the figure, including inflation, market conditions and the "
                + "confidence in the estimate at this stage."),
            new("recommendation", "Recommendation",
                "What the approving body is asked to agree, and what approval commits the council "
                + "to."),
        ]);

    public static readonly DocumentDefinition ProcurementStrategy = new(
        Stage0, "Procurement Strategy Paper", "Stage 0 — Strategic Definition",
        [
            new("objectives", "Procurement Objectives",
                "What the procurement must achieve beyond a price — programme certainty, design "
                + "quality, community benefit, risk transfer — and their relative priority. "
                + "Everything cannot be the priority."),
            new("options", "Routes Considered",
                "The routes available — framework, open tender, restricted, negotiated, "
                + "competitive dialogue — and how each performs against the objectives above."),
            new("contract-form", "Contract Form",
                "NEC or SBCC, which option or variant, and why. These behave differently: NEC "
                + "runs on early warning and compensation events administered in real time, SBCC "
                + "on instructions, variations and loss and expense. The choice determines how "
                + "the project will be administered for years, so record the reasoning."),
            new("risk-allocation", "Risk Allocation",
                "Which risks the council intends to retain and which to transfer, and what "
                + "transferring each is expected to cost. Risk transferred at any price is risk "
                + "bought back in the tender."),
            new("packaging", "Packaging and Lotting",
                "How the works are divided into contracts or lots, and what that implies for "
                + "interfaces, programme and the local market's ability to bid."),
            new("market", "Market Engagement",
                "What is known about the market's capacity and appetite, and what early "
                + "engagement is proposed."),
            new("compliance", "Regulatory and Policy Compliance",
                "Procurement thresholds, the council's standing orders, community benefit and "
                + "fair work obligations, and how each is satisfied."),
            new("recommendation", "Recommended Strategy",
                "The route, form and packaging recommended, and the authority sought."),
        ]);

    public static readonly DocumentDefinition SustainabilityAndNetZeroStrategy = new(
        Stage0, "Sustainability and Net Zero Strategy", "Stage 0 — Strategic Definition",
        [
            new("targets", "Targets and Standards",
                "The outcomes this project must achieve and the standard or rating being "
                + "targeted, with the council policy or statutory duty each derives from. A "
                + "target without a source is an aspiration."),
            new("operational-carbon", "Operational Carbon and Energy",
                "The energy performance sought, how it will be measured in use, and the heating "
                + "and power strategy. State the metric and the target value, so the building can "
                + "later be tested against it."),
            new("embodied-carbon", "Embodied Carbon",
                "The approach to materials, reuse and retention, and whether an embodied carbon "
                + "assessment is required. Retaining a structure usually beats anything done to a "
                + "new one, and that decision is taken here or not at all."),
            new("adaptation", "Climate Adaptation and Resilience",
                "Overheating, flood risk, water and drainage, and the climate scenario the design "
                + "is to be resilient to."),
            new("biodiversity", "Biodiversity and Landscape",
                "Net gain, habitat, planting and any ecological constraint on the site."),
            new("social", "Social and Economic Sustainability",
                "Community benefit, local employment and fair work commitments, and how they will "
                + "be measured."),
            new("cost", "Cost and Funding Implications",
                "What these outcomes add or save, over capital and whole life, and any grant "
                + "funding contingent on them."),
            new("verification", "Verification",
                "How achievement will be evidenced at handover and in use, and who is "
                + "accountable. A target nobody verifies is a target nobody meets."),
        ]);

    public static readonly DocumentDefinition DigitalInformationStrategy = new(
        Stage0, "Digital Information Strategy", "Stage 0 — Strategic Definition",
        [
            new("purpose", "Purpose and Asset Outcomes",
                "What the council needs the information for after handover — asset management, "
                + "maintenance, space planning, statutory compliance. Information produced for "
                + "its own sake is a cost; this section is what stops that."),
            new("standards", "Standards",
                "The information standard to be followed and the level of detail required at "
                + "each stage."),
            new("cde", "Common Data Environment",
                "Where project information is held, who has access, and how it moves between the "
                + "shared, published and archived states."),
            new("naming", "Naming and Structuring",
                "The naming convention, classification and status codes, so a file can be "
                + "identified without opening it."),
            new("responsibilities", "Responsibilities",
                "Who produces what, who checks it, and who accepts it on the council's behalf."),
            new("handover", "Information Handover",
                "What is handed over at Stage 6, in what format, and how it is to be loaded into "
                + "the council's asset systems. Name the receiving system: information handed "
                + "over in a format nothing can read has not been handed over."),
            new("security", "Information Security",
                "Classification, access control and anything sensitive about this asset that "
                + "limits what may be shared or published."),
        ]);

    public static readonly DocumentDefinition AssetInformationReview = new(
        Stage0, "Existing Asset Information Review", "Stage 0 — Strategic Definition",
        [
            new("asset", "The Asset",
                "What exists — age, construction, extent, current use and occupancy."),
            new("held", "Information Held",
                "Drawings, surveys, O&M manuals, warranties and compliance records the council "
                + "holds, with their date and where they are. Note what is known to be out of "
                + "date: old information treated as current is worse than none."),
            new("condition", "Condition and Compliance",
                "Known condition, outstanding statutory compliance, and anything on a backlog "
                + "maintenance schedule that this project could resolve."),
            new("hazards", "Known Hazards",
                "Asbestos register status, contamination, structural concerns, and services "
                + "information. Say when the asbestos survey was last done and to what type — a "
                + "management survey is not a refurbishment and demolition survey."),
            new("gaps", "Information Gaps",
                "What is not known and would need to be surveyed, with an indication of cost and "
                + "lead time. A survey with a twelve-week lead time found at Stage 3 is a "
                + "twelve-week delay."),
            new("implications", "Implications for the Project",
                "What the above means for scope, cost, programme and risk."),
        ]);

    // --- Stage 1 ---

    public static readonly DocumentDefinition EmployersRequirements = new(
        Stage1, "Employer's Requirements", "Stage 1 — Preparation and Briefing",
        [
            new("scope", "Scope of the Works",
                "What the contractor is required to provide, written as requirements rather than "
                + "as description. This document is read against what is built."),
            new("performance", "Performance Requirements",
                "The standards the completed works must meet — structural, thermal, acoustic, "
                + "fire, accessibility — stated so each can be tested."),
            new("design-responsibility", "Design Responsibility",
                "Which elements the contractor designs, which are designed for them, and who "
                + "coordinates the two. The commonest source of a dispute is an element both "
                + "parties assumed the other was designing."),
            new("materials", "Materials and Workmanship",
                "Specification standards, prohibited materials, and any requirement about "
                + "durability, maintenance or spares."),
            new("constraints", "Site and Operational Constraints",
                "Access, hours, phasing, decant, continuity of service and anything imposed by "
                + "the building remaining in use."),
            new("information", "Information Requirements",
                "What the contractor must provide, when, and in what format — including at "
                + "handover."),
            new("statutory", "Statutory Requirements",
                "Consents obtained and consents the contractor must obtain, and who discharges "
                + "each planning condition."),
            new("completion", "Completion and Handover Requirements",
                "What completion requires, what commissioning and training must be delivered, "
                + "and what is handed over."),
        ]);

    public static readonly DocumentDefinition NecScope = new(
        Stage1, "Scope (NEC)", "Stage 1 — Preparation and Briefing",
        [
            new("description", "Description of the Works",
                "What the Contractor is to provide. Under NEC the Scope is the definition of the "
                + "works: anything not in it is not included, and anything the Contractor is "
                + "later instructed to do that is not in it is a compensation event. Write it on "
                + "that basis."),
            new("constraints", "Constraints on How the Contractor Provides the Works",
                "Access, working hours, phasing, methods prohibited or required, and anything "
                + "imposed by the site remaining in use. A constraint omitted here is a "
                + "constraint imposed later at the Client's cost."),
            new("specification", "Specification and Standards",
                "The standards the works must meet, by reference where possible."),
            new("contractor-design", "Contractor's Design",
                "Which parts the Contractor designs, the requirements for that design, and the "
                + "procedure for the Project Manager to accept it."),
            new("programme", "Programme Requirements",
                "What the Accepted Programme must show beyond the contract's own requirements, "
                + "and any Key Date or sectional Completion."),
            new("tests", "Tests and Inspections",
                "Tests and inspections required, who does each, when, and what Completion depends "
                + "on. The definition of Completion belongs in the Scope and is frequently the "
                + "thing argued over at the end."),
            new("information", "Information and Handover",
                "What the Contractor provides and when, including information required at "
                + "Completion."),
            new("health-safety", "Health and Safety Requirements",
                "Requirements beyond statutory duties, and the Client's own site rules."),
        ]);

    public static readonly DocumentDefinition CommunicationPlan = new(
        Stage1, "Communication Plan", "Stage 1 — Preparation and Briefing",
        [
            new("audiences", "Audiences",
                "Who needs to hear from the project and what each needs to know. Distinguish "
                + "those who must act on information from those who merely need it."),
            new("reporting", "Reporting Cycle",
                "What report goes to whom, how often, and by when after each period end. A "
                + "reporting cycle with no cut-off produces reports that arrive after the meeting."),
            new("meetings", "Meetings",
                "Standing meetings, who attends, what each decides, and who minutes it."),
            new("public", "Public and Service User Communication",
                "What is communicated to users and the community, by whom, and the approval it "
                + "needs before it goes out."),
            new("escalation", "Escalation and Urgent Communication",
                "How bad news travels, how quickly, and to whom. The plan is tested by the first "
                + "serious incident, so write it for that."),
            new("media", "Media and Elected Members",
                "Who handles press enquiries and how elected members are briefed, particularly "
                + "ahead of anything likely to attract attention."),
            new("records", "Records",
                "Where communications are filed and who holds the record."),
        ]);

    public static readonly DocumentDefinition QualityManagementPlan = new(
        Stage1, "Quality Management Plan", "Stage 1 — Preparation and Briefing",
        [
            new("standards", "Quality Standards",
                "What quality means on this project, by reference to standards and to the brief's "
                + "requirements. \"High quality\" is not a standard."),
            new("design-quality", "Design Quality Control",
                "How design is reviewed, by whom, at what stages, and how a review's outcome is "
                + "recorded and closed out."),
            new("construction-quality", "Construction Quality Control",
                "Inspection and test plans, hold points, witnessed tests, and who may release a "
                + "hold point."),
            new("samples", "Samples and Benchmarks",
                "Samples and benchmark areas required, who approves each, and the lead time. An "
                + "approved benchmark is the only reliable way to settle a question of workmanship."),
            new("nonconformance", "Non-Conformance",
                "How non-conforming work is raised, who decides whether it is accepted, "
                + "rectified or replaced, and how the decision is recorded."),
            new("records", "Quality Records",
                "What records are kept, by whom, and what forms part of the handover information."),
            new("responsibilities", "Responsibilities",
                "Who is accountable for quality on the client side, and how that relates to the "
                + "contractor's own quality system."),
        ]);

    public static readonly DocumentDefinition PlanningStrategy = new(
        Stage1, "Planning Strategy", "Stage 1 — Preparation and Briefing",
        [
            new("status", "Planning Status",
                "The site's designation, any existing permission, and whether permitted "
                + "development applies."),
            new("consents", "Consents Required",
                "Every consent the project needs — planning, listed building, conservation area, "
                + "roads, advertisement, tree works — with the authority for each."),
            new("policy", "Policy Context",
                "The development plan policies engaged and any likely tension with them."),
            new("constraints", "Constraints and Designations",
                "Listed status, conservation area, flood risk, trees, ecology, archaeology — and "
                + "what each requires by way of assessment."),
            new("programme", "Consent Programme",
                "Submission, validation, consultation and determination dates, with the lead time "
                + "for each supporting assessment. Ecological surveys have seasons; a bat survey "
                + "missed in spring costs a year."),
            new("engagement", "Pre-Application and Consultation",
                "Pre-application engagement proposed, and consultation required before submission."),
            new("risks", "Planning Risks",
                "What could delay or refuse consent, and the contingency if it does."),
        ]);

    public static readonly DocumentDefinition FireStrategyBrief = new(
        Stage1, "Fire Strategy Brief", "Stage 1 — Preparation and Briefing",
        [
            new("building", "Building Use and Occupancy",
                "Use, occupancy numbers, who occupies it and their ability to evacuate "
                + "unassisted. A school, a care setting and an office impose different strategies."),
            new("standards", "Standards and Approach",
                "The standard to be met and whether compliance is by the technical handbook or "
                + "by a fire engineered approach. A fire engineered approach needs agreement with "
                + "the verifier early, not at warrant stage."),
            new("escape", "Means of Escape",
                "Escape strategy, travel distances, refuges and evacuation of those who cannot "
                + "use stairs."),
            new("compartmentation", "Compartmentation and Structure",
                "Fire resistance periods, compartment lines and the structural fire protection "
                + "strategy."),
            new("systems", "Fire Safety Systems",
                "Detection, alarm, suppression, smoke control and emergency lighting, with the "
                + "standard each is designed to."),
            new("access", "Access and Facilities for the Fire Service",
                "Vehicle access, hydrants, firefighting shafts and rising mains."),
            new("management", "Fire Safety Management",
                "What the strategy assumes about how the building will be managed in use. A "
                + "strategy that depends on a door being kept shut is a strategy that depends on "
                + "someone keeping it shut."),
        ]);

    public static readonly DocumentDefinition GroundInvestigationBrief = new(
        Stage1, "Ground Investigation Brief", "Stage 1 — Preparation and Briefing",
        [
            new("purpose", "Purpose of the Investigation",
                "What decisions the investigation is to inform — foundation design, "
                + "contamination liability, drainage, earthworks. An investigation without a "
                + "stated purpose returns data nobody uses."),
            new("existing", "Existing Information",
                "Previous investigations, geological records, historical land use and anything "
                + "known about made ground or former uses."),
            new("scope", "Scope of Works",
                "Boreholes, trial pits, probing, sampling and testing proposed, with locations "
                + "and depths and the reason for each."),
            new("contamination", "Contamination and Environmental Testing",
                "What is to be tested for, against which assessment criteria, and the receptors "
                + "of concern."),
            new("constraints", "Site Constraints",
                "Access, services, occupied areas, working hours, reinstatement and anything "
                + "limiting where or when investigation can take place."),
            new("deliverables", "Deliverables",
                "The factual and interpretative reports required, and who they are to be "
                + "addressed to — a report addressed to the designer alone cannot be relied on "
                + "by the contractor."),
            new("programme", "Programme",
                "When the investigation must be complete to inform the next design stage, "
                + "allowing for laboratory turnaround."),
        ]);

    public static readonly DocumentDefinition InformationRequirements = new(
        Stage1, "Information Requirements", "Stage 1 — Preparation and Briefing",
        [
            new("purpose", "Why the Information Is Needed",
                "The questions the council needs the asset information to answer once the "
                + "building is in use. Every requirement below should trace back to one of them."),
            new("asset-information", "Asset Information Required",
                "What is needed about each asset — identity, location, specification, warranty, "
                + "maintenance regime — and to what level of detail."),
            new("stage-deliverables", "Information Required at Each Stage",
                "What is due at the end of each RIBA stage, so information is produced as the "
                + "work proceeds rather than assembled afterwards from memory."),
            new("format", "Format and Structure",
                "File formats, data schemas, classification and naming, and the system the "
                + "information must load into."),
            new("responsibility", "Who Provides What",
                "Each requirement against the party responsible for it, and who checks it."),
            new("acceptance", "Acceptance Criteria",
                "How information is checked and what makes it acceptable. Information accepted "
                + "without being checked is information nobody can rely on."),
            new("handover", "Handover",
                "When information is handed over, in what structure, and how it is verified as "
                + "complete before completion is certified."),
        ]);

    /// <summary>Stage 0–1 governance and strategy, in the order the list sets them out.</summary>
    private static IReadOnlyList<(CommitteeReportType Type, DocumentDefinition Definition)> GovernancePack =>
    [
        (CommitteeReportType.StakeholderEngagementPlan, StakeholderEngagementPlan),
        (CommitteeReportType.GovernanceAndDelegationFramework, GovernanceAndDelegationFramework),
        (CommitteeReportType.FundingApprovalPaper, FundingApprovalPaper),
        (CommitteeReportType.ProcurementStrategy, ProcurementStrategy),
        (CommitteeReportType.SustainabilityAndNetZeroStrategy, SustainabilityAndNetZeroStrategy),
        (CommitteeReportType.DigitalInformationStrategy, DigitalInformationStrategy),
        (CommitteeReportType.AssetInformationReview, AssetInformationReview),
        (CommitteeReportType.EmployersRequirements, EmployersRequirements),
        (CommitteeReportType.NecScope, NecScope),
        (CommitteeReportType.CommunicationPlan, CommunicationPlan),
        (CommitteeReportType.QualityManagementPlan, QualityManagementPlan),
        (CommitteeReportType.PlanningStrategy, PlanningStrategy),
        (CommitteeReportType.FireStrategyBrief, FireStrategyBrief),
        (CommitteeReportType.GroundInvestigationBrief, GroundInvestigationBrief),
        (CommitteeReportType.InformationRequirements, InformationRequirements),
    ];
}
