using FluentValidation;

namespace SCPM.Application.SBCC.Commands.CreateVariation;

public class CreateVariationCommandValidator : AbstractValidator<CreateVariationCommand>
{
    public CreateVariationCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Description).NotEmpty();
    }
}
