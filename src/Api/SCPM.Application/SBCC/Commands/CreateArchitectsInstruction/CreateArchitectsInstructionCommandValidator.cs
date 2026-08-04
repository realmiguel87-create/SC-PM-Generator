using FluentValidation;

namespace SCPM.Application.SBCC.Commands.CreateArchitectsInstruction;

public class CreateArchitectsInstructionCommandValidator : AbstractValidator<CreateArchitectsInstructionCommand>
{
    public CreateArchitectsInstructionCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.InstructionNumber).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty();
    }
}
