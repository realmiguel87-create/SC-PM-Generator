using FluentValidation;
using SCPM.Domain.Enums;

namespace SCPM.Application.Reporting.Commands.CreateCommitteeReport;

public class CreateCommitteeReportCommandValidator : AbstractValidator<CreateCommitteeReportCommand>
{
    public CreateCommitteeReportCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ReportType).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);

        // A stage gate approval report exists to seek a decision on a particular gateway. Without
        // one it is a decision document about no decision, and its stage, its approver and its
        // outcome would all have nowhere to come from but someone's typing.
        RuleFor(x => x.GatewayId)
            .NotEmpty()
            .When(x => x.ReportType == CommitteeReportType.StageGateApproval)
            .WithMessage("A stage gate approval report must name the gateway it seeks a decision on.");

        // And no other report type may carry one. A committee paper pointing at a gateway would
        // imply the paper records that gateway's decision, which it does not.
        RuleFor(x => x.GatewayId)
            .Empty()
            .When(x => x.ReportType != CommitteeReportType.StageGateApproval)
            .WithMessage("Only a stage gate approval report is attached to a gateway.");
    }
}
