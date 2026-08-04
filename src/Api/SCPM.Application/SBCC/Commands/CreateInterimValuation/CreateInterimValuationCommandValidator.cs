using FluentValidation;

namespace SCPM.Application.SBCC.Commands.CreateInterimValuation;

public class CreateInterimValuationCommandValidator : AbstractValidator<CreateInterimValuationCommand>
{
    public CreateInterimValuationCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ValuationNumber).GreaterThan(0);
        RuleFor(x => x.GrossValuation).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NetPayment).GreaterThanOrEqualTo(0);
    }
}
