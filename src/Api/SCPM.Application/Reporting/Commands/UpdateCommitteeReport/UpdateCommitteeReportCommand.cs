using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Common;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.Reporting.Commands.UpdateCommitteeReport;

/// <summary>Content for one section, keyed by <see cref="ReportSection.Key"/>.</summary>
public record ReportSectionUpdate(string Key, string? Content);

/// <summary>
/// Saves a report's narrative.
///
/// Takes only the sections the caller is changing rather than the whole document. The previous
/// version took every field as a parameter, so a client editing one paragraph had to send back all
/// ten — and any it failed to send were written as null, silently erasing sections nobody meant to
/// touch.
/// </summary>
public record UpdateCommitteeReportCommand(
    Guid CommitteeReportId,
    IReadOnlyList<ReportSectionUpdate> Sections,
    DateOnly? ReportDate = null) : IRequest<Unit>;

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
        var report = await _db.CommitteeReports
            .Include(r => r.Sections)
            .FirstOrDefaultAsync(r => r.Id == request.CommitteeReportId, cancellationToken)
            ?? throw new KeyNotFoundException($"Committee report {request.CommitteeReportId} not found.");

        if (report.Status != CommitteeReportStatus.Draft)
        {
            throw new InvalidOperationException($"Report is {report.Status} and can no longer be edited.");
        }

        var actorId = _currentUser.UserId ?? Guid.Empty;

        foreach (var update in request.Sections)
        {
            // A key the report type does not define is refused rather than stored. Content under
            // an unknown key would be invisible — no heading renders it and no export includes it
            // — so accepting it would tell an author their work was saved when it had in effect
            // been discarded.
            if (ReportSections.Find(report.ReportType, update.Key) is null)
            {
                throw new InvalidOperationException(
                    $"'{update.Key}' is not a section of a {report.ReportType}.");
            }

            var existing = report.Sections.FirstOrDefault(s => s.SectionKey == update.Key);

            if (string.IsNullOrWhiteSpace(update.Content))
            {
                // Clearing a section removes the row rather than storing an empty string, so
                // "never written" and "deliberately emptied" end up the same state. They read
                // identically in the document, and keeping them apart in the database would be a
                // distinction the document cannot express.
                if (existing is not null) report.Sections.Remove(existing);
                continue;
            }

            if (existing is null)
            {
                report.Sections.Add(new CommitteeReportSectionContent
                {
                    CommitteeReportId = report.Id,
                    SectionKey = update.Key,
                    Content = update.Content,
                    CreatedBy = actorId,
                });
            }
            else
            {
                existing.Content = update.Content;
                existing.ModifiedBy = actorId;
                existing.ModifiedDate = DateTime.UtcNow;
            }
        }

        if (request.ReportDate.HasValue) report.ReportDate = request.ReportDate;

        report.ModifiedBy = actorId;
        report.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
