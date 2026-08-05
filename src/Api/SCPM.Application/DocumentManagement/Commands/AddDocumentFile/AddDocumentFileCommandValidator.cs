using FluentValidation;

namespace SCPM.Application.DocumentManagement.Commands.AddDocumentFile;

public class AddDocumentFileCommandValidator : AbstractValidator<AddDocumentFileCommand>
{
    public AddDocumentFileCommandValidator()
    {
        RuleFor(x => x.DocumentVersionId).NotEmpty();
        RuleFor(x => x.FileType).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.Content).NotNull();
    }
}
