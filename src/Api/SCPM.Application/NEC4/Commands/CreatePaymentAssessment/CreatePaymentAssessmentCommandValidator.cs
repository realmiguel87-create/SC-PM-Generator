using FluentValidation;

namespace SCPM.Application.NEC4.Commands.CreatePaymentAssessment;

public class CreatePaymentAssessmentCommandValidator : AbstractValidator<CreatePaymentAssessmentCommand>
{
    public CreatePaymentAssessmentCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.AssessmentNumber).GreaterThan(0);
        RuleFor(x => x.AmountDue).GreaterThanOrEqualTo(0);
    }
}
