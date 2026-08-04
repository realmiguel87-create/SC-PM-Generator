using FluentValidation;

namespace SCPM.Application.NEC4.Commands.CreateRiskAllocationItem;

public class CreateRiskAllocationItemCommandValidator : AbstractValidator<CreateRiskAllocationItemCommand>
{
    public CreateRiskAllocationItemCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
    }
}
