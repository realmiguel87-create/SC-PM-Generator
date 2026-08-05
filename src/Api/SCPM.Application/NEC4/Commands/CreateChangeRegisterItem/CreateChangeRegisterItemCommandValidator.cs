using FluentValidation;

namespace SCPM.Application.NEC4.Commands.CreateChangeRegisterItem;

public class CreateChangeRegisterItemCommandValidator : AbstractValidator<CreateChangeRegisterItemCommand>
{
    public CreateChangeRegisterItemCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}
