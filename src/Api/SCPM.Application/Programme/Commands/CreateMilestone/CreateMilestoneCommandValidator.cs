using FluentValidation;

namespace SCPM.Application.Programme.Commands.CreateMilestone;

public class CreateMilestoneCommandValidator : AbstractValidator<CreateMilestoneCommand>
{
    public CreateMilestoneCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ForecastDate).GreaterThanOrEqualTo(x => x.BaselineDate);
    }
}
