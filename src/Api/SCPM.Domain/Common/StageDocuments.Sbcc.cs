using SCPM.Domain.Enums;

namespace SCPM.Domain.Common;

/// <summary>
/// SBCC contract administration: the instructions and certificates the Architect or Contract
/// Administrator issues under the Scottish Building Contract.
///
/// As with the NEC set, these are templates for the content the administrator writes and do not
/// reproduce SBCC's published forms. The contract itself is SBCC's instrument.
///
/// The equivalences on the client PM's list are real but not exact, and the guidance says so
/// where the difference matters. A compensation event and a variation are not the same thing: a
/// compensation event carries its own time and money assessment together, where under SBCC a
/// variation is valued and an extension of time and loss and expense are pursued separately and
/// on different tests. Treating them as the same document with different letterheads is how an
/// administrator trained on one form mishandles the other.
/// </summary>
public static partial class StageDocuments
{
    private const string Sbcc = "Infrastructure Delivery · SBCC Contract Administration";

    public static readonly DocumentDefinition SbccArchitectsInstruction = new(
        Sbcc, "Architect's Instruction", "SBCC",
        [
            new("instruction", "Instruction",
                "What the Contractor is instructed to do, precisely enough to be carried out and "
                + "valued, with the drawings and revisions relied on."),
            new("authority", "Power Relied On",
                "The clause empowering the instruction. An instruction outside the contract's "
                + "powers need not be obeyed, and if obeyed may not be payable as a variation."),
            new("variation", "Whether This Is a Variation",
                "Whether the instruction requires a Variation, and if so how it is to be valued. "
                + "An instruction that varies the works without being recognised as a variation "
                + "is work the Contractor will claim for later and with less evidence."),
            new("time-cost", "Expected Effect on Time and Cost",
                "The anticipated effect, and whether the Contractor is expected to notify a "
                + "delay or a loss and expense claim. Under SBCC these follow separate routes "
                + "from the valuation, and each has its own notice requirement."),
            new("compliance", "Compliance",
                "When the instruction is to be complied with, and anything it must precede or "
                + "follow."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Instruction No", "Issued By", "Date Issued"]);

    public static readonly DocumentDefinition SbccVariationOrder = new(
        Sbcc, "Variation Order and Valuation", "SBCC",
        [
            new("variation", "The Variation",
                "What is added, omitted or changed, by reference to the instruction that required "
                + "it."),
            new("valuation-rules", "Basis of Valuation",
                "Which valuation rule applies — contract rates, rates of a similar character "
                + "adjusted, fair rates, or daywork — and why. The rules apply in order and the "
                + "choice is not discretionary; recording the reasoning is what makes the "
                + "valuation defensible."),
            new("measurement", "Measurement and Build-Up",
                "The quantities, rates and the resulting value. Attach the build-up rather than "
                + "summarising it: a figure without its measure cannot be checked."),
            new("preliminaries", "Effect on Preliminaries",
                "Whether the variation changes the Contractor's time-related costs, and how that "
                + "has been valued. Often omitted, and the commonest reason a valued variation "
                + "becomes a loss and expense claim later."),
            new("time", "Effect on Time",
                "Whether the variation is a Relevant Event, and whether an extension of time has "
                + "been applied for. The valuation does not deal with time: a variation valued "
                + "and a programme not extended is a dispute in preparation."),
            new("omissions", "Omissions",
                "Work omitted and its value, and confirmation the omitted work is not being given "
                + "to others — an omission for that purpose is not a valid variation."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Variation No", "Value", "Date Valued"]);

    public static readonly DocumentDefinition SbccExtensionOfTimeAward = new(
        Sbcc, "Extension of Time Award", "SBCC",
        [
            new("application", "Application",
                "What the Contractor applied for, when, and whether the notice and particulars "
                + "required by the contract were given."),
            new("relevant-event", "Relevant Event Relied On",
                "The Relevant Event relied on, and whether the facts fall within it. Only the "
                + "events the contract lists can found an extension, however reasonable the "
                + "delay."),
            new("cause-effect", "Cause and Effect",
                "How the event actually delayed completion, by reference to the programme. An "
                + "event that delayed work not on the critical path delayed the works not at "
                + "all, and the analysis belongs here rather than being assumed."),
            new("concurrency", "Concurrent Delay",
                "Whether a contractor-culpable delay ran concurrently, and how that has been "
                + "treated. State the approach taken: this is the most contested question in "
                + "delay analysis and an unexplained award invites challenge."),
            new("mitigation", "Mitigation",
                "What the Contractor did to prevent or reduce the delay, as the contract requires."),
            new("award", "Award and Reasoning",
                "The extension awarded or refused, the revised Completion Date, and the "
                + "reasoning. A refusal needs reasons as much as an award does."),
            new("loss-expense", "Relationship to Loss and Expense",
                "That an extension of time does not of itself entitle the Contractor to money: "
                + "loss and expense is a separate claim on a separate test, and the two are "
                + "routinely confused."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Extension Awarded", "Revised Completion Date", "Date of Award"]);

    public static readonly DocumentDefinition SbccLossAndExpenseAssessment = new(
        Sbcc, "Loss and Expense Assessment", "SBCC",
        [
            new("application", "Application",
                "What the Contractor claimed, when, and whether the notice and the information "
                + "the contract requires were provided."),
            new("matter", "Relevant Matter Relied On",
                "The Relevant Matter relied on, and whether the facts fall within it. The list "
                + "is shorter than the list of Relevant Events: an event can extend time without "
                + "carrying money."),
            new("deferment", "Regular Progress",
                "How the matter materially affected regular progress of the works. This is the "
                + "test, and a claim that shows cost without showing disruption to progress does "
                + "not meet it."),
            new("heads", "Heads of Claim",
                "Each head claimed — prolongation, disruption, thickening, finance, loss of "
                + "overhead recovery — with what is claimed under each."),
            new("substantiation", "Substantiation",
                "The records relied on for each head. Loss and expense is reimbursement of actual "
                + "loss, not a formula: a claim calculated rather than evidenced is a claim "
                + "capable of being reduced to nothing."),
            new("assessment", "Assessment",
                "What is allowed under each head and what is not, with reasons for each refusal."),
            new("double-counting", "Interaction With Variation Valuations",
                "Whether any element claimed has already been included in a variation valuation. "
                + "Preliminaries recovered twice is the commonest overpayment in a final account."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Amount Assessed", "Assessed By", "Date of Assessment"]);

    public static readonly DocumentDefinition SbccInterimCertificate = new(
        Sbcc, "Interim Certificate", "SBCC",
        [
            new("valuation", "Valuation",
                "The valuation date, the gross valuation, and what it comprises — work properly "
                + "executed, materials on site, materials off site where the contract permits."),
            new("adjustments", "Adjustments",
                "Variations valued, loss and expense ascertained, fluctuations and any other "
                + "adjustment, each identified."),
            new("retention", "Retention",
                "Retention held, the percentage applied, and any part released on practical "
                + "completion of a section."),
            new("deductions", "Deductions and Set-Off",
                "Amounts previously certified, and any deduction. A deduction requires a valid "
                + "pay less notice served in time — a deduction made without one is not a "
                + "deduction, it is a breach."),
            new("notices", "Payment Notices",
                "The due date, the final date for payment, and the dates any payment notice or "
                + "pay less notice was served. These dates are statutory as well as contractual, "
                + "and getting them wrong is how a payment becomes immediately due in full."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Certificate No", "Amount Certified", "Final Date for Payment"]);

    public static readonly DocumentDefinition SbccNonCompletionCertificate = new(
        Sbcc, "Non-Completion Certificate", "SBCC",
        [
            new("position", "The Position",
                "The Completion Date as extended, and confirmation the Contractor has not "
                + "achieved practical completion by it."),
            new("extensions", "Extensions of Time Determined",
                "Every extension awarded to date and the resulting Completion Date, and "
                + "confirmation that all applications received have been determined. Issuing "
                + "this certificate with an undetermined application outstanding is how the "
                + "right to damages is lost."),
            new("works-outstanding", "Works Outstanding",
                "What remains to be completed."),
            new("damages", "Liquidated Damages",
                "The rate, the period, and the notice the contract requires before damages may "
                + "be deducted. The certificate is a precondition to deduction, not the "
                + "deduction itself."),
            new("effect", "Effect of a Later Extension",
                "That a further extension cancels this certificate and a new one must be issued, "
                + "and that damages deducted must then be repaid."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Completion Date", "Issued By", "Date Issued"]);

    public static readonly DocumentDefinition SbccMakingGoodDefectsCertificate = new(
        Sbcc, "Certificate of Making Good Defects", "SBCC",
        [
            new("basis", "Basis of the Certificate",
                "That the rectification period has expired and the defects in the schedule have "
                + "been made good, with both dates."),
            new("schedule", "Schedule of Defects",
                "The schedule delivered to the Contractor, when, and confirmation it was "
                + "delivered within the period the contract allows. A schedule delivered late "
                + "may not be enforceable."),
            new("made-good", "Defects Made Good",
                "What has been rectified and inspected."),
            new("not-made-good", "Defects Not Made Good",
                "Anything outstanding, and how it is to be dealt with — including any deduction "
                + "agreed in place of rectification."),
            new("consequences", "Consequences",
                "Release of the remaining retention and the effect on the Final Certificate."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Rectification Period Ended", "Certified By", "Date Issued"]);

    public static readonly DocumentDefinition SbccFinalCertificate = new(
        Sbcc, "Final Certificate", "SBCC",
        [
            new("timing", "Timing",
                "The event that started the period for issuing the Final Certificate and the "
                + "date it is issued, with confirmation it is within the contract's period."),
            new("account", "Final Account",
                "The adjusted contract sum, reconciling to the figure certified."),
            new("adjustments", "Adjustments",
                "Variations, provisional sum adjustments, loss and expense, fluctuations and "
                + "deductions, each identified and totalled."),
            new("payments", "Payments and Balance",
                "Amounts previously certified, retention released, and the balance due either "
                + "way."),
            new("conclusiveness", "Conclusive Effect",
                "What the Final Certificate is conclusive evidence of, and the period within "
                + "which proceedings must be commenced to prevent that. This is the most "
                + "consequential certificate under the contract: once the period passes, matters "
                + "it covers can no longer be reopened, including by the Employer."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Balance Due", "Certified By", "Date Issued"]);

    public static readonly DocumentDefinition SbccFinalAccountAgreement = new(
        Sbcc, "Final Account Agreement", "SBCC",
        [
            new("account", "The Agreed Account",
                "The contract sum as adjusted and the agreed final figure."),
            new("adjustments", "Adjustments Agreed",
                "Each element of the adjustment and its agreed value."),
            new("settled", "Matters Settled by Agreement",
                "Claims and disputed items settled at a figure rather than ascertained, and the "
                + "basis of each settlement. Where a figure is a compromise, say so — a reader "
                + "later cannot derive a compromise from the contract."),
            new("excluded", "Matters Not Covered",
                "Anything expressly outside this agreement and remaining open, including "
                + "defects, latent or otherwise."),
            new("effect", "Effect of Agreement",
                "What each party gives up by signing, and the relationship to the Final "
                + "Certificate."),
        ],
        StageLabel: "Contract",
        ClosingPanel: ["Agreed Final Sum", "Agreed By", "Date of Agreement"]);

    public static readonly DocumentDefinition SbccDefectsLiabilityReview = new(
        Sbcc, "Defects Liability Review", "SBCC",
        [
            new("period", "Period Reviewed",
                "The rectification period and where this review falls within it."),
            new("reported", "Defects Reported",
                "What has been reported, by whom, and when — including items raised by the "
                + "building's occupiers rather than by inspection."),
            new("rectified", "Rectified",
                "What has been made good and inspected, and anything rectified more than once."),
            new("outstanding", "Outstanding",
                "What remains, with the action in hand and the date expected."),
            new("patterns", "Patterns and Systemic Issues",
                "Whether the defects indicate a systemic problem. Found inside the rectification "
                + "period it is the Contractor's to put right; found after it, it is the "
                + "Employer's."),
            new("schedule", "Readiness for the Schedule of Defects",
                "What will go on the schedule at the end of the period, so it is assembled as "
                + "the period runs rather than in the days before the deadline."),
        ],
        StageLabel: "Contract");

    /// <summary>SBCC contract administration.</summary>
    private static IReadOnlyList<(CommitteeReportType Type, DocumentDefinition Definition)> SbccPack =>
    [
        (CommitteeReportType.SbccArchitectsInstruction, SbccArchitectsInstruction),
        (CommitteeReportType.SbccVariationOrder, SbccVariationOrder),
        (CommitteeReportType.SbccExtensionOfTimeAward, SbccExtensionOfTimeAward),
        (CommitteeReportType.SbccLossAndExpenseAssessment, SbccLossAndExpenseAssessment),
        (CommitteeReportType.SbccInterimCertificate, SbccInterimCertificate),
        (CommitteeReportType.SbccNonCompletionCertificate, SbccNonCompletionCertificate),
        (CommitteeReportType.SbccMakingGoodDefectsCertificate, SbccMakingGoodDefectsCertificate),
        (CommitteeReportType.SbccFinalCertificate, SbccFinalCertificate),
        (CommitteeReportType.SbccFinalAccountAgreement, SbccFinalAccountAgreement),
        (CommitteeReportType.SbccDefectsLiabilityReview, SbccDefectsLiabilityReview),
    ];
}
