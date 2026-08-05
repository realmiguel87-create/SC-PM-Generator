using FluentValidation;

namespace SCPM.Application.Reporting.Commands.UpdateCommitteeReport;

public class UpdateCommitteeReportCommandValidator : AbstractValidator<UpdateCommitteeReportCommand>
{
    public UpdateCommitteeReportCommandValidator()
    {
        RuleFor(x => x.CommitteeReportId).NotEmpty();
        RuleFor(x => x.ExecutiveSummary).NotEmpty();
    }
}
