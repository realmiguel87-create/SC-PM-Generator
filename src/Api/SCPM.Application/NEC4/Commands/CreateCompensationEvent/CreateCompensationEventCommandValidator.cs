using FluentValidation;

namespace SCPM.Application.NEC4.Commands.CreateCompensationEvent;

public class CreateCompensationEventCommandValidator : AbstractValidator<CreateCompensationEventCommand>
{
    public CreateCompensationEventCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EstimatedValue).GreaterThanOrEqualTo(0);
    }
}
