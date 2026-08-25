using SCPM.Domain.Enums;

namespace SCPM.Domain.Common;

/// <summary>
/// Which status moves each register permits, and the guard that enforces them.
///
/// These live in the Domain because they are contractual facts, not interface preferences. Before
/// this existed the update commands set whatever status they were handed, so the only thing
/// stopping an agreed loss-and-expense claim being moved back to Claimed was that the UI did not
/// offer a button for it. In a register that evidences commercial decisions, a status that can
/// silently reverse is worse than one that cannot change at all: the record stops being evidence.
///
/// Two rules shape every map below.
///
/// **Terminal states are terminal.** Rejected, Implemented, Paid, Agreed and Awarded accept no
/// further moves. Reversing a determination is a contractual event that belongs in the register as
/// a new entry with its own reasoning — a variation re-instructed, a claim resubmitted — not as an
/// edit that overwrites the original decision and leaves nothing behind to say it happened.
///
/// **A status may always be re-applied to itself.** Correcting the days awarded on an extension of
/// time, or the amount agreed on a claim, without changing where it sits in the process is a
/// legitimate correction, and the commands carry those figures alongside the status. Rejecting a
/// no-op transition would block the correction along with the reversal.
/// </summary>
public static class StatusTransitions
{
    public static readonly IReadOnlyDictionary<CompensationEventStatus, CompensationEventStatus[]> CompensationEvent =
        new Dictionary<CompensationEventStatus, CompensationEventStatus[]>
        {
            // A CE is notified, quoted, then accepted or rejected; an accepted one is implemented.
            [CompensationEventStatus.Notified] = [CompensationEventStatus.Quoted, CompensationEventStatus.Rejected],
            [CompensationEventStatus.Quoted] = [CompensationEventStatus.Accepted, CompensationEventStatus.Rejected],
            [CompensationEventStatus.Accepted] = [CompensationEventStatus.Implemented],
            [CompensationEventStatus.Implemented] = [],
            [CompensationEventStatus.Rejected] = [],
        };

    public static readonly IReadOnlyDictionary<PaymentAssessmentStatus, PaymentAssessmentStatus[]> PaymentAssessment =
        new Dictionary<PaymentAssessmentStatus, PaymentAssessmentStatus[]>
        {
            // Strictly forward: an assessment is certified before it is paid, and a payment that
            // has been made cannot be un-made by editing a row.
            [PaymentAssessmentStatus.Assessed] = [PaymentAssessmentStatus.Certified],
            [PaymentAssessmentStatus.Certified] = [PaymentAssessmentStatus.Paid],
            [PaymentAssessmentStatus.Paid] = [],
        };

    public static readonly IReadOnlyDictionary<ChangeRegisterStatus, ChangeRegisterStatus[]> ChangeRegisterItem =
        new Dictionary<ChangeRegisterStatus, ChangeRegisterStatus[]>
        {
            [ChangeRegisterStatus.Proposed] = [ChangeRegisterStatus.Approved, ChangeRegisterStatus.Rejected],
            [ChangeRegisterStatus.Approved] = [ChangeRegisterStatus.Implemented],
            [ChangeRegisterStatus.Implemented] = [],
            [ChangeRegisterStatus.Rejected] = [],
        };

    public static readonly IReadOnlyDictionary<VariationStatus, VariationStatus[]> Variation =
        new Dictionary<VariationStatus, VariationStatus[]>
        {
            // SBCC variations have no rejected state — an instruction has been issued, and the
            // question is only what it is worth. Agreeing the value settles it.
            [VariationStatus.Instructed] = [VariationStatus.Priced],
            [VariationStatus.Priced] = [VariationStatus.Agreed],
            [VariationStatus.Agreed] = [],
        };

    public static readonly IReadOnlyDictionary<ExtensionOfTimeStatus, ExtensionOfTimeStatus[]> ExtensionOfTime =
        new Dictionary<ExtensionOfTimeStatus, ExtensionOfTimeStatus[]>
        {
            // A claim may be determined straight away or go through review first, which is why
            // Claimed reaches Awarded directly as well as via UnderReview.
            [ExtensionOfTimeStatus.Claimed] =
                [ExtensionOfTimeStatus.UnderReview, ExtensionOfTimeStatus.Awarded, ExtensionOfTimeStatus.Rejected],
            [ExtensionOfTimeStatus.UnderReview] = [ExtensionOfTimeStatus.Awarded, ExtensionOfTimeStatus.Rejected],
            [ExtensionOfTimeStatus.Awarded] = [],
            [ExtensionOfTimeStatus.Rejected] = [],
        };

    public static readonly IReadOnlyDictionary<LossAndExpenseStatus, LossAndExpenseStatus[]> LossAndExpenseClaim =
        new Dictionary<LossAndExpenseStatus, LossAndExpenseStatus[]>
        {
            [LossAndExpenseStatus.Claimed] =
                [LossAndExpenseStatus.UnderReview, LossAndExpenseStatus.Agreed, LossAndExpenseStatus.Rejected],
            [LossAndExpenseStatus.UnderReview] = [LossAndExpenseStatus.Agreed, LossAndExpenseStatus.Rejected],
            [LossAndExpenseStatus.Agreed] = [],
            [LossAndExpenseStatus.Rejected] = [],
        };

    /// <summary>
    /// Throws when a move is not permitted. <see cref="InvalidOperationException"/> specifically:
    /// the API's exception middleware maps it to 409 Conflict and surfaces the message, which is
    /// the right answer for "the record is not in a state where that makes sense" — as opposed to
    /// 400, which would imply the request itself was malformed.
    /// </summary>
    public static void EnsureAllowed<TStatus>(
        IReadOnlyDictionary<TStatus, TStatus[]> transitions,
        TStatus current,
        TStatus next,
        string what) where TStatus : struct, Enum
    {
        if (EqualityComparer<TStatus>.Default.Equals(current, next)) return;

        var allowed = transitions.TryGetValue(current, out var moves) ? moves : [];
        if (allowed.Contains(next)) return;

        var options = allowed.Length == 0
            ? $"{current} is final"
            : $"allowed: {string.Join(", ", allowed)}";

        throw new InvalidOperationException(
            $"Cannot move {what} from {current} to {next} ({options}).");
    }
}
