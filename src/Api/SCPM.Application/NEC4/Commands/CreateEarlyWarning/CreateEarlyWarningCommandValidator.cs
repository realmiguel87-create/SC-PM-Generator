using FluentValidation;

namespace SCPM.Application.NEC4.Commands.CreateEarlyWarning;

public class CreateEarlyWarningCommandValidator : AbstractValidator<CreateEarlyWarningCommand>
{
    public CreateEarlyWarningCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}
