using FluentValidation;

namespace SCPM.Application.StakeholderManagement.Commands.CreateEngagement;

public class CreateEngagementCommandValidator : AbstractValidator<CreateEngagementCommand>
{
    public CreateEngagementCommandValidator()
    {
        RuleFor(x => x.StakeholderId).NotEmpty();
        RuleFor(x => x.Method).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Summary).NotEmpty();
    }
}
