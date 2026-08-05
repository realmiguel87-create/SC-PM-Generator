using FluentValidation;

namespace SCPM.Application.SBCC.Commands.CreateLossAndExpenseClaim;

public class CreateLossAndExpenseClaimCommandValidator : AbstractValidator<CreateLossAndExpenseClaimCommand>
{
    public CreateLossAndExpenseClaimCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.ClaimedAmount).GreaterThanOrEqualTo(0);
    }
}
