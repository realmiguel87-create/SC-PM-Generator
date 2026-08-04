using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.Reporting.Commands.SubmitCommitteeReport;

/// <summary>Locks a report against further edits once it's gone to committee — Draft -> Submitted.</summary>
public record SubmitCommitteeReportCommand(Guid CommitteeReportId) : IRequest<Unit>;

public class SubmitCommitteeReportCommandHandler : IRequestHandler<SubmitCommitteeReportCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SubmitCommitteeReportCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(SubmitCommitteeReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _db.CommitteeReports.FirstOrDefaultAsync(r => r.Id == request.CommitteeReportId, cancellationToken)
            ?? throw new KeyNotFoundException($"Committee report {request.CommitteeReportId} not found.");

        if (report.Status != CommitteeReportStatus.Draft)
            throw new InvalidOperationException($"Report is already {report.Status}.");

        report.Status = CommitteeReportStatus.Submitted;
        report.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        report.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
