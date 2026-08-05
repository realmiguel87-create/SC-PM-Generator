using FluentValidation;

namespace SCPM.Application.SBCC.Commands.CreateExtensionOfTime;

public class CreateExtensionOfTimeCommandValidator : AbstractValidator<CreateExtensionOfTimeCommand>
{
    public CreateExtensionOfTimeCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Reason).NotEmpty();
        RuleFor(x => x.DaysClaimed).GreaterThan(0);
    }
}
