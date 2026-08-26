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

        // No rule pairing approver and date any more: the approver is no longer supplied by the
        // caller, so the handler sets both together or neither, and the pairing cannot be broken
        // from outside. A validation rule guarding an invariant the type system already holds is
        // a rule that will one day be wrong without anyone noticing.
    }
}
