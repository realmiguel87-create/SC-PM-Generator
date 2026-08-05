using FluentValidation;

namespace SCPM.Application.Governance.Commands.CreateGateway;

public class CreateGatewayCommandValidator : AbstractValidator<CreateGatewayCommand>
{
    public CreateGatewayCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.StageNumber).InclusiveBetween((byte)0, (byte)7);
        RuleFor(x => x.GatewayType).NotEmpty().MaximumLength(50);
    }
}
