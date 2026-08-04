using FluentValidation;

namespace SCPM.Application.StakeholderManagement.Commands.CreateStakeholder;

public class CreateStakeholderCommandValidator : AbstractValidator<CreateStakeholderCommand>
{
    public CreateStakeholderCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
    }
}
