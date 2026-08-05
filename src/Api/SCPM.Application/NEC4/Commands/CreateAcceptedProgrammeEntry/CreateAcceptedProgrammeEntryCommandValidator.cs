using FluentValidation;

namespace SCPM.Application.NEC4.Commands.CreateAcceptedProgrammeEntry;

public class CreateAcceptedProgrammeEntryCommandValidator : AbstractValidator<CreateAcceptedProgrammeEntryCommand>
{
    public CreateAcceptedProgrammeEntryCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.RevisionNumber).GreaterThan(0);
    }
}
