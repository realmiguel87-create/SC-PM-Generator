using FluentValidation;

namespace SCPM.Application.Governance.Commands.CreateDecision;

public class CreateDecisionCommandValidator : AbstractValidator<CreateDecisionCommand>
{
    public CreateDecisionCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
    }
}
