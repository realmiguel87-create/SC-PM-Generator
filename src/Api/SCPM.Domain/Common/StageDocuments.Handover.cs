using SCPM.Domain.Enums;

namespace SCPM.Domain.Common;

/// <summary>
/// Stage 6–7 handover and use documents.
///
/// Not here, and for the usual reason: the asset register, warranty register and defects
/// monitoring log are registers, and O&M manuals, as-built information and the asset information
/// model are produced by others in other tools. What those need from this platform is a place to
/// be recorded and an export, not a prose template to be retyped into.
/// </summary>
public static partial class StageDocuments
{
    public static readonly DocumentDefinition HandoverPlan = new(
        Stage6, "Handover Plan", "Stage 6 — Handover",
        [
            new("dates", "Handover Dates and Sequence",
                "Practical completion, access for the service, fit-out, migration and first "
                + "operational use, in order and with dates. Handover is a sequence, not a day, "
                + "and treating it as a day is why buildings open before they work."),
            new("readiness", "Readiness Criteria",
                "What must be true before the building is accepted — commissioning complete, "
                + "statutory certificates in hand, training delivered, systems proven. Stated so "
                + "each can be judged met or not."),
            new("commissioning", "Commissioning and Witnessing",
                "What is to be commissioned, what the client will witness, and when. Seasonal "
                + "commissioning cannot be completed at handover: say what will be proven later "
                + "and who will be there to prove it."),
            new("documentation", "Information and Documentation",
                "What is handed over, in what format, to whom, and by when — O&M manuals, "
                + "as-built information, the health and safety file, warranties and certificates."),
            new("training", "Training and Familiarisation",
                "Who is to be trained, on what, by whom, and when. Training delivered to people "
                + "who leave before occupation is training that must be repeated."),
            new("migration", "Decant and Migration",
                "Moving people, equipment and services in, and anything leaving the old premises."),
            new("aftercare", "Aftercare",
                "Who the service contacts about a defect, during what hours, and the "
                + "arrangements for the first weeks of occupation when most faults appear."),
            new("responsibilities", "Responsibilities",
                "Who leads handover on each side, and the point at which responsibility for the "
                + "building passes."),
        ]);

    public static readonly DocumentDefinition BuildingUserGuide = new(
        Stage6, "Building User Guide", "Stage 6 — Handover",
        [
            new("purpose", "About This Guide",
                "Who it is for and what it covers. This is written for the people who use the "
                + "building, not for the facilities team — plain language, no system names, "
                + "nothing requiring a drawing to understand."),
            new("layout", "Getting Around",
                "Layout, entrances, accessible routes, and where the things people look for are."),
            new("environment", "Heating, Ventilation and Lighting",
                "How the building is meant to be kept comfortable, what users can adjust and "
                + "what they cannot, and what to do if a space is too hot, cold or stuffy. A "
                + "building whose users do not know what they control is a building whose "
                + "windows are propped open against the heating."),
            new("energy", "Using the Building Efficiently",
                "The few behaviours that make the most difference to energy use here, "
                + "specifically — not general advice."),
            new("safety", "Safety and Emergencies",
                "Fire procedure, assembly points, first aid, and anything particular to this "
                + "building's fire strategy that depends on how people behave."),
            new("security", "Access and Security",
                "Entry, out-of-hours arrangements, and who to contact."),
            new("faults", "Reporting a Problem",
                "How to report a fault, what happens next, and what counts as urgent."),
            new("contacts", "Contacts",
                "Who to contact for what, with the detail kept current."),
        ]);

    public static readonly DocumentDefinition OccupancyReadinessReport = new(
        Stage6, "Occupancy Readiness Report", "Stage 6 — Handover",
        [
            new("assessment", "Readiness Assessment",
                "Each readiness criterion from the Handover Plan, and whether it is met. The "
                + "purpose of this report is to answer one question — can the building be "
                + "occupied — so answer it."),
            new("statutory", "Statutory Compliance",
                "Completion certificate, fire safety, gas, electrical and lift certification, "
                + "and anything outstanding. Occupation without these is not a programme "
                + "decision, it is unlawful."),
            new("commissioning", "Commissioning Status",
                "What is commissioned and proven, what is commissioned provisionally, and what "
                + "remains seasonal."),
            new("outstanding", "Outstanding Works and Defects",
                "What remains, whether any of it affects occupation, and the dates for "
                + "completion."),
            new("service-readiness", "Service Readiness",
                "Whether the service is ready — staff trained, equipment in, systems connected, "
                + "cleaning and security arranged. A building ready for a service that is not is "
                + "still not ready."),
            new("risks", "Risks to Occupation",
                "What could prevent or disrupt occupation on the planned date, and the "
                + "contingency."),
            new("recommendation", "Recommendation",
                "Whether occupation is recommended, with or without conditions, and what those "
                + "conditions are."),
        ],
        ClosingPanel: ["Recommendation", "Reported By", "Planned Occupation Date"]);

    public static readonly DocumentDefinition TrainingRecord = new(
        Stage6, "Training and Familiarisation Record", "Stage 6 — Handover",
        [
            new("sessions", "Training Delivered",
                "Each session, its subject, who delivered it, when, and how long it ran."),
            new("attendees", "Who Was Trained",
                "Who attended each session and their role. Record roles as well as names: when "
                + "a person leaves, the question is which role is now untrained."),
            new("systems", "Systems and Equipment Covered",
                "What each session covered, and the level — operation, routine maintenance, or "
                + "fault diagnosis."),
            new("materials", "Materials Provided",
                "Handouts, recordings and quick reference guides issued, and where they are held "
                + "so a later starter can find them."),
            new("outstanding", "Training Outstanding",
                "Who has not been trained, on what, and when it will be arranged — including "
                + "anything that can only be trained in a particular season."),
            new("competence", "Confirmation of Competence",
                "Who has confirmed the trained staff can operate the building, and on what basis."),
        ],
        ClosingPanel: ["Sessions Delivered", "Recorded By", "Date Completed"]);

    // --- Stage 7 ---

    public static readonly DocumentDefinition BenefitsRealisationReview = new(
        Stage7, "Benefits Realisation Review", "Stage 7 — Use",
        [
            new("benefits", "Benefits Claimed",
                "The benefits set out in the business case, as they were stated, with their "
                + "target values and dates. Quote the original: a benefit restated after the "
                + "event is a benefit written to be achieved."),
            new("measurement", "How Each Was Measured",
                "The measure used for each benefit, the baseline it is compared against, and "
                + "when it was taken."),
            new("achieved", "Benefits Achieved",
                "What has been realised, with the figures. Say where a benefit cannot yet be "
                + "measured and when it could be."),
            new("not-achieved", "Benefits Not Achieved",
                "What has not been realised and why. This is the section that makes the review "
                + "worth reading, and the one most often softened into nothing."),
            new("unexpected", "Unanticipated Effects",
                "Benefits and disbenefits nobody forecast, including effects on neighbouring "
                + "services or buildings."),
            new("attribution", "Attribution",
                "How confident the review is that the project caused the change measured, rather "
                + "than something else happening at the same time."),
            new("actions", "Actions to Realise Outstanding Benefits",
                "What would still secure a benefit not yet achieved, who owns it, and by when."),
        ]);

    public static readonly DocumentDefinition BuildingPerformanceReview = new(
        Stage7, "Building Performance Review", "Stage 7 — Use",
        [
            new("period", "Period and Method",
                "The period reviewed, how long after occupation, and where the data comes from — "
                + "meters, the building management system, maintenance records, occupant reports."),
            new("energy", "Energy and Carbon",
                "Measured consumption against the design prediction and against the target set "
                + "in the sustainability strategy. State both: a building can beat its target and "
                + "still use twice what was modelled."),
            new("gap", "Performance Gap",
                "Where measured performance differs from prediction, and what explains it — "
                + "control settings, commissioning, occupancy patterns, or the model's own "
                + "assumptions. A gap is normal; an unexplained one is a finding."),
            new("comfort", "Comfort and Internal Conditions",
                "Temperature, humidity, air quality, daylight and acoustics as measured and as "
                + "reported by occupants, which frequently disagree."),
            new("systems", "Systems in Operation",
                "How the plant is actually running, including anything operating outside its "
                + "design intent or overridden by facilities staff. An override is usually "
                + "evidence that something does not work as designed."),
            new("maintenance", "Maintenance and Reliability",
                "Faults, callouts and anything proving harder to maintain than expected."),
            new("actions", "Actions",
                "Adjustments worth making to this building, with an owner and a date."),
        ]);

    public static readonly DocumentDefinition MaintenanceStrategy = new(
        Stage7, "Maintenance Strategy", "Stage 7 — Use",
        [
            new("scope", "Scope and Assets Covered",
                "What this strategy covers, by asset or system, and what is covered elsewhere."),
            new("regimes", "Maintenance Regimes",
                "Planned preventive, reactive and condition-based maintenance, and which applies "
                + "to what. Say why: maintaining everything preventively costs more than it saves."),
            new("statutory", "Statutory Maintenance and Inspection",
                "Inspections and tests required by law, their frequency, and who holds the "
                + "certificates. These are not discretionary and should be listed separately "
                + "from everything else for that reason."),
            new("delivery", "How Maintenance Is Delivered",
                "In-house, contracted, or mixed, and the arrangements in place."),
            new("response", "Response Times and Priorities",
                "What counts as urgent, the response expected, and who decides priority when "
                + "several things fail at once."),
            new("lifecycle", "Lifecycle and Replacement",
                "Expected lives of major components, the replacement profile, and the budget "
                + "provision. A lifecycle plan with no budget behind it is a list of future "
                + "emergencies."),
            new("information", "Information and Records",
                "Where asset information, O&M manuals and maintenance records are held, and how "
                + "they are kept current as the building changes."),
        ]);

    public static readonly DocumentDefinition AssetManagementPlan = new(
        Stage7, "Asset Management Plan", "Stage 7 — Use",
        [
            new("asset", "The Asset",
                "What it is, what it is for, who uses it, and its place in the council's estate."),
            new("condition", "Condition and Performance",
                "Current condition, energy and operational performance, and how each is assessed "
                + "and how often."),
            new("service-fit", "Fit With Service Needs",
                "How well the building serves the service now, and what is known about how that "
                + "demand may change."),
            new("investment", "Planned Investment",
                "Lifecycle replacement, improvement and compliance work planned, with indicative "
                + "cost and timing."),
            new("sustainability", "Sustainability and Net Zero",
                "The asset's position against the council's net zero commitments and what would "
                + "be required to improve it."),
            new("risks", "Risks to the Asset",
                "Condition, obsolescence, statutory change, climate and demand risks, with "
                + "owners."),
            new("options", "Future Options",
                "Retain, invest, repurpose or dispose, and what would trigger reconsidering. An "
                + "asset plan that assumes permanent retention is not a plan."),
            new("review", "Review",
                "When this plan is next reviewed and who owns it."),
        ]);

    public static readonly DocumentDefinition ContractCloseOutReport = new(
        Stage7, "Contract Close-Out Report", "Stage 7 — Use",
        [
            new("contract", "The Contract",
                "The form of contract, the parties, the contract sum and the final account, and "
                + "the key dates from award to final certificate or final assessment."),
            new("administration", "How the Contract Was Administered",
                "What worked and what did not in the administration — notices given on time or "
                + "late, assessments made within periods or not, records kept or reconstructed. "
                + "Under either form, most of what goes wrong financially goes wrong here rather "
                + "than on site."),
            new("change", "Change and Its Causes",
                "The volume and value of change, and what caused it, grouped by origin — client "
                + "requirement, design development, site conditions, statutory. The distribution "
                + "is what the next project needs to know."),
            new("claims", "Claims and Disputes",
                "What was claimed, how it was resolved, and what it cost in time and fees to "
                + "resolve."),
            new("performance", "Supplier Performance",
                "How the contractor and consultants performed, recorded so that it can be used "
                + "in future selection — which requires it to be evidenced, not characterised."),
            new("obligations", "Continuing Obligations",
                "Warranties, collateral warranties, latent defects provisions and insurance "
                + "obligations that outlive the contract, with their expiry dates and where the "
                + "documents are held."),
            new("recommendations", "Recommendations",
                "What the council should do differently in its contracts and their "
                + "administration, with an owner for each."),
        ],
        ClosingPanel: ["Final Account Sum", "Reported By", "Date of Close-Out"]);

    /// <summary>Stage 6–7 handover and use.</summary>
    private static IReadOnlyList<(CommitteeReportType Type, DocumentDefinition Definition)> HandoverPack =>
    [
        (CommitteeReportType.HandoverPlan, HandoverPlan),
        (CommitteeReportType.BuildingUserGuide, BuildingUserGuide),
        (CommitteeReportType.OccupancyReadinessReport, OccupancyReadinessReport),
        (CommitteeReportType.TrainingRecord, TrainingRecord),
        (CommitteeReportType.BenefitsRealisationReview, BenefitsRealisationReview),
        (CommitteeReportType.BuildingPerformanceReview, BuildingPerformanceReview),
        (CommitteeReportType.MaintenanceStrategy, MaintenanceStrategy),
        (CommitteeReportType.AssetManagementPlan, AssetManagementPlan),
        (CommitteeReportType.ContractCloseOutReport, ContractCloseOutReport),
    ];
}
