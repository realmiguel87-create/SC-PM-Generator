using FluentAssertions;
using SCPM.Domain.Common;
using SCPM.Domain.Enums;
using Xunit;

namespace SCPM.UnitTests.Domain;

/// <summary>
/// The status-transition rules, and the guard that enforces them.
///
/// Before these existed the update commands set whatever status they were handed, so the only
/// thing preventing an agreed loss-and-expense claim reverting to Claimed was that the UI had no
/// button for it. In a register that evidences commercial decisions, a status that can silently
/// reverse is worse than one that cannot change at all — the record stops being evidence.
/// </summary>
public class StatusTransitionsTests
{
    [Fact]
    public void Allows_a_move_the_register_permits()
    {
        var act = () => StatusTransitions.EnsureAllowed(
            StatusTransitions.CompensationEvent,
            CompensationEventStatus.Notified,
            CompensationEventStatus.Quoted,
            "CE-001");

        act.Should().NotThrow();
    }

    [Fact]
    public void Rejects_a_move_that_skips_a_step()
    {
        // A CE has to be quoted before it can be accepted: accepting an unquoted event would
        // record agreement to a value nobody has stated.
        var act = () => StatusTransitions.EnsureAllowed(
            StatusTransitions.CompensationEvent,
            CompensationEventStatus.Notified,
            CompensationEventStatus.Accepted,
            "CE-001");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Notified to Accepted*")
            .And.Message.Should().Contain("allowed: Quoted, Rejected",
                "an error that names the permitted moves is actionable; one that only says no is not");
    }

    [Fact]
    public void Rejects_reversing_a_determination()
    {
        // The case this whole guard exists for.
        var act = () => StatusTransitions.EnsureAllowed(
            StatusTransitions.LossAndExpenseClaim,
            LossAndExpenseStatus.Agreed,
            LossAndExpenseStatus.Claimed,
            "LE-001");

        act.Should().Throw<InvalidOperationException>()
            .And.Message.Should().Contain("Agreed is final");
    }

    [Fact]
    public void Rejects_un_paying_a_payment_assessment()
    {
        var act = () => StatusTransitions.EnsureAllowed(
            StatusTransitions.PaymentAssessment,
            PaymentAssessmentStatus.Paid,
            PaymentAssessmentStatus.Certified,
            "PA1");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Allows_re_applying_the_same_status()
    {
        // Correcting the days awarded on an extension of time without moving it through the
        // process is a legitimate correction, and the command carries that figure alongside the
        // status. Rejecting the no-op would block the correction along with the reversal.
        var act = () => StatusTransitions.EnsureAllowed(
            StatusTransitions.ExtensionOfTime,
            ExtensionOfTimeStatus.Awarded,
            ExtensionOfTimeStatus.Awarded,
            "EOT-001");

        act.Should().NotThrow();
    }

    [Fact]
    public void Allows_determining_a_claim_without_a_review_step()
    {
        // Claimed reaches Awarded directly as well as via UnderReview: a straightforward claim
        // does not need a review stage recorded against it just to satisfy the model.
        var act = () => StatusTransitions.EnsureAllowed(
            StatusTransitions.ExtensionOfTime,
            ExtensionOfTimeStatus.Claimed,
            ExtensionOfTimeStatus.Awarded,
            "EOT-001");

        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(EveryRegisterStatus))]
    public void Every_status_of_every_register_has_a_rule(string register, object status)
    {
        // A status missing from its map is treated as terminal by EnsureAllowed, which is a safe
        // default but a silent one: an enum gaining a value would quietly become a dead end that
        // nothing could move out of, with no error to explain why the buttons vanished.
        var found = register switch
        {
            nameof(StatusTransitions.CompensationEvent) =>
                StatusTransitions.CompensationEvent.ContainsKey((CompensationEventStatus)status),
            nameof(StatusTransitions.PaymentAssessment) =>
                StatusTransitions.PaymentAssessment.ContainsKey((PaymentAssessmentStatus)status),
            nameof(StatusTransitions.ChangeRegisterItem) =>
                StatusTransitions.ChangeRegisterItem.ContainsKey((ChangeRegisterStatus)status),
            nameof(StatusTransitions.Variation) =>
                StatusTransitions.Variation.ContainsKey((VariationStatus)status),
            nameof(StatusTransitions.ExtensionOfTime) =>
                StatusTransitions.ExtensionOfTime.ContainsKey((ExtensionOfTimeStatus)status),
            nameof(StatusTransitions.LossAndExpenseClaim) =>
                StatusTransitions.LossAndExpenseClaim.ContainsKey((LossAndExpenseStatus)status),
            _ => throw new ArgumentOutOfRangeException(nameof(register), register, null),
        };

        found.Should().BeTrue($"{register} has no rule for {status}");
    }

    public static TheoryData<string, object> EveryRegisterStatus()
    {
        var data = new TheoryData<string, object>();

        foreach (var status in Enum.GetValues<CompensationEventStatus>())
            data.Add(nameof(StatusTransitions.CompensationEvent), status);
        foreach (var status in Enum.GetValues<PaymentAssessmentStatus>())
            data.Add(nameof(StatusTransitions.PaymentAssessment), status);
        foreach (var status in Enum.GetValues<ChangeRegisterStatus>())
            data.Add(nameof(StatusTransitions.ChangeRegisterItem), status);
        foreach (var status in Enum.GetValues<VariationStatus>())
            data.Add(nameof(StatusTransitions.Variation), status);
        foreach (var status in Enum.GetValues<ExtensionOfTimeStatus>())
            data.Add(nameof(StatusTransitions.ExtensionOfTime), status);
        foreach (var status in Enum.GetValues<LossAndExpenseStatus>())
            data.Add(nameof(StatusTransitions.LossAndExpenseClaim), status);

        return data;
    }
}
