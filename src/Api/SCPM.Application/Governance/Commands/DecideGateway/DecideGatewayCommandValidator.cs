using FluentValidation;

namespace SCPM.Application.Governance.Commands.DecideGateway;

public class DecideGatewayCommandValidator : AbstractValidator<DecideGatewayCommand>
{
    public DecideGatewayCommandValidator()
    {
        RuleFor(x => x.GatewayId).NotEmpty();
        RuleFor(x => x.Decision).IsInEnum();
        RuleFor(x => x.Comments).MaximumLength(4000);
    }
}
