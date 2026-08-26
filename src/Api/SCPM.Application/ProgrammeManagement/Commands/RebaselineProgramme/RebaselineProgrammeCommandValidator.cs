using FluentValidation;

namespace SCPM.Application.ProgrammeManagement.Commands.RebaselineProgramme;

public class RebaselineProgrammeCommandValidator : AbstractValidator<RebaselineProgrammeCommand>
{
    public RebaselineProgrammeCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        // A reason is mandatory and given a floor, not just a NotEmpty. A rebaseline moves the
        // measure a project is judged against; "update" as a justification leaves the register
        // recording that it happened and nothing about why, which is the half a committee needs.
        // Ten characters does not make an explanation good, but it does stop the reflexive
        // one-word entry.
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(10)
            .WithMessage("Give a reason for the rebaseline — this is the record of why the "
                       + "sanctioned programme changed.")
            .MaximumLength(2000);

        // Approver and date travel together: a date with nobody attached, or an approver with no
        // date, is a half-record that reads as authority without being any.
        RuleFor(x => x.ApprovedDate)
            .NotNull()
            .When(x => x.ApprovedBy.HasValue)
            .WithMessage("An approval date is required when an approver is named.");

        RuleFor(x => x.ApprovedBy)
            .NotNull()
            .When(x => x.ApprovedDate.HasValue)
            .WithMessage("An approver is required when an approval date is given.");
    }
}
