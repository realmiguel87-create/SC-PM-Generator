using FluentValidation;

namespace SCPM.Application.NEC4.Commands.CreateContractDataEntry;

public class CreateContractDataEntryCommandValidator : AbstractValidator<CreateContractDataEntryCommand>
{
    public CreateContractDataEntryCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ClauseReference).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Value).NotEmpty();
    }
}
