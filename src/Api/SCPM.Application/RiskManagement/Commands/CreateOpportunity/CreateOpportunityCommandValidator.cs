using FluentValidation;

namespace SCPM.Application.RiskManagement.Commands.CreateOpportunity;

public class CreateOpportunityCommandValidator : AbstractValidator<CreateOpportunityCommand>
{
    public CreateOpportunityCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PotentialValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Probability).InclusiveBetween(1, 5);
    }
}
