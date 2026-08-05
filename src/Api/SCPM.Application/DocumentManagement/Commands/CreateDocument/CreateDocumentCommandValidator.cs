using FluentValidation;

namespace SCPM.Application.DocumentManagement.Commands.CreateDocument;

public class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RibaStageNumber).InclusiveBetween((byte)0, (byte)7).When(x => x.RibaStageNumber.HasValue);
    }
}
