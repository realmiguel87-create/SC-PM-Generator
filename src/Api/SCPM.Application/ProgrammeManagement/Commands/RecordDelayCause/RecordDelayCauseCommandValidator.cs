using FluentValidation;

namespace SCPM.Application.ProgrammeManagement.Commands.RecordDelayCause;

public class RecordDelayCauseCommandValidator : AbstractValidator<RecordDelayCauseCommand>
{
    public RecordDelayCauseCommandValidator()
    {
        RuleFor(x => x.MilestoneId).NotEmpty();

        // Positive only. A cause that recovered time is not a delay cause, and admitting negatives
        // would let a programme explain away slip it never made up — attributed days would net off
        // against each other and the unexplained remainder, the one figure this feature exists to
        // produce, would quietly shrink.
        RuleFor(x => x.DelayDays)
            .GreaterThan(0)
            .WithMessage("A delay cause must account for at least one day.");

        // A ceiling on plausibility rather than on the contract. Ten years of delay on a single
        // milestone is a typo — most often a date entered where a day count belongs — and catching
        // it here is cheaper than finding it in a committee paper.
        RuleFor(x => x.DelayDays)
            .LessThanOrEqualTo(3650)
            .WithMessage("That is more than ten years — check the figure.");

        RuleFor(x => x.Category).IsInEnum();

        // Required even when a claim is cited: a reference tells a reader which claim, not what
        // went wrong. Twenty characters, more than the ten asked of a rebaseline reason, because
        // this is meant to be an account rather than a label.
        RuleFor(x => x.Narrative)
            .NotEmpty()
            .MinimumLength(20)
            .WithMessage("Describe what happened — a claim reference alone does not explain the delay.")
            .MaximumLength(2000);
    }
}
