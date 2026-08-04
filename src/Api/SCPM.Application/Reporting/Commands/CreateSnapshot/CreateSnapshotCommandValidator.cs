using FluentValidation;

namespace SCPM.Application.Reporting.Commands.CreateSnapshot;

public class CreateSnapshotCommandValidator : AbstractValidator<CreateSnapshotCommand>
{
    public CreateSnapshotCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
    }
}
