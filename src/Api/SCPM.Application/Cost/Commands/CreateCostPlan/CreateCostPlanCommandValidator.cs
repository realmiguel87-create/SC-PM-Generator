using FluentValidation;

namespace SCPM.Application.Cost.Commands.CreateCostPlan;

public class CreateCostPlanCommandValidator : AbstractValidator<CreateCostPlanCommand>
{
    public CreateCostPlanCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A cost plan must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.CostCategory).NotEmpty().MaximumLength(100);
            line.RuleFor(l => l.Amount).GreaterThanOrEqualTo(0);
        });
    }
}
