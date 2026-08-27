using FluentValidation;

namespace SCPM.Application.Reporting.Commands.UpdateCommitteeReport;

public class UpdateCommitteeReportCommandValidator : AbstractValidator<UpdateCommitteeReportCommand>
{
    public UpdateCommitteeReportCommandValidator()
    {
        RuleFor(x => x.CommitteeReportId).NotEmpty();

        RuleForEach(x => x.Sections).ChildRules(section =>
        {
            section.RuleFor(s => s.Key).NotEmpty();
            section.RuleFor(s => s.Content).MaximumLength(20_000);
        });

        // There is no longer a rule requiring an executive summary. It was enforced here when
        // every report had one; a status report has no such section, so the rule would have made
        // every status report unsaveable. Whether a particular section must be filled in before a
        // report goes anywhere is a question about submitting it, not about saving a draft — and
        // Submit is where that belongs.
    }
}
