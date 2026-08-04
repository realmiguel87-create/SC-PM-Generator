using FluentValidation;

namespace SCPM.Application.Projects.Commands.CreateProject;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.ProjectRef).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ApprovedBudget).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetCompletionDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.TargetCompletionDate.HasValue)
            .WithMessage("Target completion date must be on or after the start date.");
    }
}
