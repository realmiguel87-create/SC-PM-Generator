using FluentValidation;

namespace SCPM.Application.Reporting.Commands.CreateCommitteeReport;

public class CreateCommitteeReportCommandValidator : AbstractValidator<CreateCommitteeReportCommand>
{
    public CreateCommitteeReportCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ReportType).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}
