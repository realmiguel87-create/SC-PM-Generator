using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.Reporting.Commands.UpdateCommitteeReport;

public record UpdateCommitteeReportCommand(
    Guid CommitteeReportId,
    string ExecutiveSummary,
    string? Background,
    string? CurrentPosition,
    string? FinanceCommentary,
    string? ProgrammeCommentary,
    string? RiskCommentary,
    string? StakeholderCommentary,
    string? SustainabilityCommentary,
    string? EqualityImpactCommentary,
    string? Recommendations) : IRequest<Unit>;

public class UpdateCommitteeReportCommandHandler : IRequestHandler<UpdateCommitteeReportCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCommitteeReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateCommitteeReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _db.CommitteeReports.FirstOrDefaultAsync(r => r.Id == request.CommitteeReportId, cancellationToken)
            ?? throw new KeyNotFoundException($"Committee report {request.CommitteeReportId} not found.");

        if (report.Status != CommitteeReportStatus.Draft)
            throw new InvalidOperationException($"Report is {report.Status} and can no longer be edited.");

        report.ExecutiveSummary = request.ExecutiveSummary;
        report.Background = request.Background;
        report.CurrentPosition = request.CurrentPosition;
        report.FinanceCommentary = request.FinanceCommentary;
        report.ProgrammeCommentary = request.ProgrammeCommentary;
        report.RiskCommentary = request.RiskCommentary;
        report.StakeholderCommentary = request.StakeholderCommentary;
        report.SustainabilityCommentary = request.SustainabilityCommentary;
        report.EqualityImpactCommentary = request.EqualityImpactCommentary;
        report.Recommendations = request.Recommendations;
        report.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        report.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
