using SCPM.Domain.Enums;

namespace SCPM.Domain.Common;

/// <summary>
/// NEC4 contract administration: the notices, instructions and certificates the Project Manager
/// and Supervisor issue under the contract.
///
/// These are not NEC's published forms and do not reproduce them. They are templates for the
/// content the administrator writes, so that a notice carries what the contract requires it to
/// carry. The contract itself — the conditions, the Contract Data, the Z clauses — is NEC's
/// instrument and is not reproduced anywhere in this platform.
///
/// Clause numbers are referred to only where they are stable across the main options and are
/// described rather than quoted. Every one of these documents carries a section asking the
/// administrator to state the clause they are acting under, because the executed contract's
/// Z clauses can and do alter the procedure, and a notice issued under the wrong clause may be
/// no notice at all.
///
/// NEC's distinguishing feature is that it is administered in real time and to timescales: an
/// early warning that is not given, a compensation event not notified within the period, a
/// quotation not replied to — each has a consequence under the contract. The guidance says so
/// where it matters, because a template that treats these as paperwork teaches the wrong habit.
/// </summary>
public static partial class StageDocuments
{
    private const string Nec = "Infrastructure Delivery · NEC4 Contract Administration";

    public static readonly DocumentDefinition NecEarlyWarningNotice = new(
        Nec, "Early Warning Notice", "NEC4",
        [
            new("matter", "The Matter",
                "What could increase the Prices, delay Completion or a Key Date, or impair the "
                + "performance of the works in use. Describe the matter, not its blame."),
            new("effect", "Possible Effect",
                "What the effect could be on cost, time and performance, as far as it is known. "
                + "An early warning is given because the effect is not yet known: uncertainty is "
                + "not a reason to delay giving it."),
            new("timing", "Why Now",
                "When the matter became apparent. The contract requires early warning as soon as "
                + "either party becomes aware, and a late warning can affect how a related "
                + "compensation event is assessed."),
            new("proposals", "Proposals for Avoiding or Reducing the Effect",
                "What could be done, by whom. This is the purpose of the notice: the register "
                + "exists to drive a meeting, not to record a grievance."),
            new("meeting", "Early Warning Meeting",
                "Whether an early warning meeting is proposed or required, and who should attend."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Notice No", "Issued By", "Date Issued"]);

    public static readonly DocumentDefinition NecCompensationEventNotification = new(
        Nec, "Compensation Event Notification", "NEC4",
        [
            new("event", "The Event",
                "What has happened, described factually and with its date."),
            new("clause", "Clause Relied On",
                "The compensation event clause relied on, and why this event falls within it. "
                + "Check the executed contract: Z clauses alter the list, and a notification "
                + "under a clause the contract does not contain is not a notification."),
            new("notified-by", "Who Notified and When",
                "Whether this is a Project Manager notification or a Contractor notification, "
                + "and the date the party became aware. The contract's time bars run from "
                + "awareness, and a Contractor notification given late may be barred entirely."),
            new("early-warning", "Early Warning",
                "Whether an early warning was given for this matter. If it should have been and "
                + "was not, the event may be assessed as if it had been — which changes the "
                + "figure, so record the position here rather than discovering it at assessment."),
            new("effect", "Known or Expected Effect",
                "The effect on the Prices and on Completion so far as known, and what is still "
                + "to be established."),
            new("quotation", "Quotation Instruction",
                "Whether a quotation is instructed, the period for submitting it, and any "
                + "assumptions the Project Manager states for the assessment. Stated assumptions "
                + "are how a quotation is made possible for an event whose outcome is unknown."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Event No", "Notified By", "Date Notified"]);

    public static readonly DocumentDefinition NecCompensationEventQuotation = new(
        Nec, "Compensation Event Quotation Assessment", "NEC4",
        [
            new("event", "The Event",
                "The compensation event this quotation relates to, by number and date."),
            new("quotation", "Quotation Submitted",
                "What the Contractor has quoted for the change to the Prices and for any delay "
                + "to Completion or a Key Date, and the date it was submitted."),
            new("basis", "Basis of Assessment",
                "How the quotation is built up — Defined Cost plus Fee, or the Shorter Schedule "
                + "of Cost Components, or rates where agreed — and whether the build-up follows "
                + "the contract's method. A quotation priced on something other than the "
                + "contract's basis cannot be accepted however reasonable the figure looks."),
            new("assumptions", "Assumptions",
                "The assumptions the quotation rests on, including any stated by the Project "
                + "Manager. An assumption later found wrong is itself a compensation event, so "
                + "record them precisely."),
            new("time", "Effect on Completion and Key Dates",
                "The delay to planned Completion shown on the Accepted Programme, and whether "
                + "terminal float or time risk allowance has been used."),
            new("assessment", "Project Manager's Assessment",
                "Whether the quotation is accepted, not accepted with reasons, or to be "
                + "assessed by the Project Manager, and the reasoning. The contract sets a period "
                + "for reply and a failure to reply can mean the quotation is treated as accepted."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Event No", "Assessed By", "Date of Reply"]);

    public static readonly DocumentDefinition NecProjectManagerInstruction = new(
        Nec, "Project Manager's Instruction", "NEC4",
        [
            new("instruction", "Instruction",
                "What the Contractor is instructed to do, precisely enough to be carried out and "
                + "priced, with the drawings and revisions it relies on."),
            new("authority", "Authority for the Instruction",
                "The clause under which it is given. The Project Manager may only instruct what "
                + "the contract allows."),
            new("scope-change", "Change to the Scope",
                "Whether this instruction changes the Scope, and if so what the Scope now says. "
                + "An instruction that changes the Scope is normally a compensation event and "
                + "should be notified as one."),
            new("compensation-event", "Compensation Event",
                "Whether this instruction is notified as a compensation event, and its number. "
                + "Instructing work without notifying the event is how a final account becomes "
                + "a reconstruction exercise."),
            new("urgency", "Urgency and Sequence",
                "When the instructed work is to be carried out and anything it must precede or "
                + "follow."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Instruction No", "Issued By", "Date Issued"]);

    public static readonly DocumentDefinition NecDefectNotification = new(
        Nec, "Defect Notification", "NEC4",
        [
            new("defect", "The Defect",
                "What part of the works does not meet the Scope or the applicable law, where it "
                + "is, and how it was found. A Defect is defined by the Scope, so identify the "
                + "requirement not met rather than describing the work as poor."),
            new("notified-by", "Notified By",
                "Whether notified by the Supervisor or the Contractor, and the date."),
            new("correction", "Correction",
                "What correction is required and the defect correction period that now runs. The "
                + "period starts from notification or from Completion depending on when the "
                + "Defect was found, so state which."),
            new("access", "Access for Correction",
                "What access the Contractor needs, when it can be given, and anything about the "
                + "building's use that constrains it."),
            new("consequences", "If Not Corrected",
                "What follows if the Defect is not corrected within the period — assessment of "
                + "the cost of having others correct it, and the effect on the Defects "
                + "Certificate."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Defect No", "Notified By", "Correction Due By"]);

    public static readonly DocumentDefinition NecSupervisorInspectionRecord = new(
        Nec, "Supervisor's Inspection Record", "NEC4",
        [
            new("inspection", "Inspection Carried Out",
                "What was inspected or tested, where, when, and against which requirement of the "
                + "Scope."),
            new("notice", "Notice Given",
                "Whether the required notice of the test or inspection was given, by whom, and "
                + "when. The contract requires notice before a test, and an untested-but-covered "
                + "element can be required to be uncovered at the Contractor's cost."),
            new("result", "Result",
                "What was found, and whether the work conforms to the Scope."),
            new("defects", "Defects Notified",
                "Any Defect notified as a result, by number."),
            new("retest", "Retest or Further Action",
                "Whether a retest is required, when, and who bears the cost of repeating it."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Record No", "Supervisor", "Date of Inspection"]);

    public static readonly DocumentDefinition NecCompletionCertificate = new(
        Nec, "Completion Certificate", "NEC4",
        [
            new("works", "Works Certified Complete",
                "What is certified complete, and any section or part excluded."),
            new("basis", "Basis of Completion",
                "How the works meet the definition of Completion in the Scope — the work the "
                + "Scope states must be done by Completion, and the Defects that do not prevent "
                + "the Client from using the works. The definition is in the Scope, so quote it "
                + "rather than judging completion by impression."),
            new("defects", "Defects at Completion",
                "Defects notified and not yet corrected, and confirmation that none prevents use."),
            new("consequences", "Consequences",
                "What certification triggers — the defects date, the start of the defect "
                + "correction periods, the Client taking over, and the effect on the retention "
                + "and on delay damages."),
            new("handover", "Handed Over at Completion",
                "What the Scope required the Contractor to provide by Completion and what has "
                + "been provided."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Completion Date", "Certified By", "Defects Date"]);

    public static readonly DocumentDefinition NecDefectsCertificate = new(
        Nec, "Defects Certificate", "NEC4",
        [
            new("basis", "Basis of the Certificate",
                "That the defects date has passed and the last defect correction period has "
                + "ended, with both dates."),
            new("corrected", "Defects Corrected",
                "Defects notified during the defect correction periods and since corrected."),
            new("uncorrected", "Defects Not Corrected",
                "Defects notified and not corrected, listed. The certificate is issued either "
                + "way; what is listed determines the assessment of the cost of correcting them, "
                + "so an omission here is money forgone."),
            new("assessment", "Cost of Uncorrected Defects",
                "The assessed cost of having others correct what remains, and how it was assessed."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Defects Date", "Issued By", "Date Issued"]);

    public static readonly DocumentDefinition NecFinalAssessment = new(
        Nec, "Final Assessment", "NEC4",
        [
            new("basis", "Basis and Timing",
                "Which event triggered the final assessment and the date, and confirmation it "
                + "has been made within the contract's period. If the Project Manager does not "
                + "assess within the period, the Contractor's own assessment can become "
                + "conclusive — so the date matters more than it appears to."),
            new("total", "Total of the Prices",
                "The Prices as adjusted for every implemented compensation event, reconciling to "
                + "the figure assessed."),
            new("compensation-events", "Compensation Events",
                "Each implemented event by number and value, totalling to the adjustment above."),
            new("deductions", "Amounts Due to the Client",
                "Delay damages, the assessed cost of uncorrected Defects, and any other amount "
                + "due to the Client, each with its basis."),
            new("payments", "Payments Made",
                "Amounts previously certified and paid, and the balance now due either way."),
            new("conclusiveness", "Conclusiveness",
                "What the assessment concludes and the period within which it may be challenged. "
                + "After that period it is binding, which is the point of the document."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Amount Due", "Assessed By", "Date of Assessment"]);

    public static readonly DocumentDefinition NecDefectsPeriodReview = new(
        Nec, "Defects Period Review", "NEC4",
        [
            new("period", "Period Reviewed",
                "The period this review covers and where it falls between Completion and the "
                + "defects date."),
            new("notified", "Defects Notified in the Period",
                "What has been notified, by whom, and the correction period running on each."),
            new("corrected", "Defects Corrected",
                "What has been corrected and accepted, and anything corrected more than once."),
            new("outstanding", "Outstanding and Overdue",
                "Defects whose correction period has expired uncorrected, and the action proposed."),
            new("patterns", "Patterns",
                "Whether the defects point at a systemic problem rather than isolated faults. A "
                + "pattern found during the defects period can still be put right by the "
                + "Contractor; found after the defects date, it is the Client's."),
            new("occupier", "Occupier Experience",
                "How the building is performing for those using it, and anything reported that "
                + "has not been raised as a Defect."),
        ],
        StageLabel: "Contract");

    /// <summary>NEC4 contract administration.</summary>
    private static IReadOnlyList<(CommitteeReportType Type, DocumentDefinition Definition)> NecPack =>
    [
        (CommitteeReportType.NecEarlyWarningNotice, NecEarlyWarningNotice),
        (CommitteeReportType.NecCompensationEventNotification, NecCompensationEventNotification),
        (CommitteeReportType.NecCompensationEventQuotation, NecCompensationEventQuotation),
        (CommitteeReportType.NecProjectManagerInstruction, NecProjectManagerInstruction),
        (CommitteeReportType.NecDefectNotification, NecDefectNotification),
        (CommitteeReportType.NecSupervisorInspectionRecord, NecSupervisorInspectionRecord),
        (CommitteeReportType.NecCompletionCertificate, NecCompletionCertificate),
        (CommitteeReportType.NecDefectsCertificate, NecDefectsCertificate),
        (CommitteeReportType.NecFinalAssessment, NecFinalAssessment),
        (CommitteeReportType.NecDefectsPeriodReview, NecDefectsPeriodReview),
    ];
}
