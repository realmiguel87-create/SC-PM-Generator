using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.ProgrammeManagement.Commands.RebaselineProgramme;

/// <summary>
/// Rebaselines a project's programme: the current forecast becomes the sanctioned dates, and the
/// programme being replaced is kept as a numbered, named, reasoned record.
/// </summary>
/// <param name="ApprovedDate">
/// When the rebaseline was sanctioned. The approver is not a parameter: it is taken from the
/// caller's identity.
///
/// It was originally a parameter, and could not be used. The approver is an SCPM user id — the
/// `scpm_user_id` claim minted by EntraClaimsTransformation — and a browser client has no way to
/// know it; MSAL knows only the Entra object id. The field could therefore only be filled by
/// typing a GUID, which in a record whose purpose is evidencing who sanctioned a change is worse
/// than leaving it empty: an unusable field invites a wrong one.
///
/// So this records who exercised the authority to make the change, which is a fact the server
/// holds and can vouch for. Attributing it instead to the body that took the decision — a
/// committee rather than the officer recording its minute — is a modelling question, not a
/// parameter, and is noted in the roadmap.
/// </param>
public record RebaselineProgrammeCommand(
    Guid ProjectId,
    string Name,
    string Reason,
    DateOnly? ApprovedDate) : IRequest<Guid>;

public class RebaselineProgrammeCommandHandler : IRequestHandler<RebaselineProgrammeCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RebaselineProgrammeCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(RebaselineProgrammeCommand request, CancellationToken cancellationToken)
    {
        var milestones = await _db.Milestones
            .Where(m => m.ProjectId == request.ProjectId)
            .ToListAsync(cancellationToken);

        if (milestones.Count == 0)
        {
            // Refused rather than allowed as a no-op. A baseline with no dates in it is not a
            // programme, and recording one would put a governance entry in the register that
            // sanctions nothing — worse than the absence it replaces, because it reads as a
            // decision that was taken.
            throw new InvalidOperationException(
                "Cannot rebaseline a programme with no milestones.");
        }

        var existing = await _db.ProgrammeBaselines
            .Where(b => b.ProjectId == request.ProjectId)
            .OrderByDescending(b => b.Revision)
            .ToListAsync(cancellationToken);

        var userId = _currentUser.UserId ?? Guid.Empty;
        var nextRevision = existing.Count == 0 ? 2 : existing[0].Revision + 1;

        if (existing.Count == 0)
        {
            // First rebaseline on a project that predates this feature. The dates currently in
            // Milestone.BaselineDate *are* the original sanctioned programme, and they are about
            // to be overwritten — so they are captured as revision 1 before that happens.
            //
            // Without this step the original programme would survive only in the milestone
            // temporal history, where nothing marks which of its rows was the sanctioned one. The
            // record would be technically recoverable and practically unusable, which is the
            // failure this whole entity exists to prevent.
            //
            // No approver is recorded against it. Whoever sanctioned those dates did so before
            // this record existed, and naming the person running the rebaseline would attribute a
            // decision they did not take.
            _db.ProgrammeBaselines.Add(BuildBaseline(
                request.ProjectId,
                revision: 1,
                name: "Original baseline",
                reason: "Captured automatically when the programme was first rebaselined. These "
                      + "are the baseline dates that were in place beforehand; the approval "
                      + "behind them predates this record.",
                approvedBy: null,
                approvedDate: null,
                isCurrent: false,
                createdBy: userId,
                dates: milestones.Select(m => (m.Id, m.Name, m.BaselineDate))));
        }

        foreach (var superseded in existing.Where(b => b.IsCurrent))
        {
            superseded.IsCurrent = false;
            superseded.ModifiedBy = userId;
            superseded.ModifiedDate = DateTime.UtcNow;
        }

        // The new sanctioned date is where the milestone actually sits: its actual date once it
        // has completed, its forecast otherwise. Same precedence as Milestone.DelayDays and the
        // timeline chart — a completed milestone's forecast stopped meaning anything the day it
        // completed, and rebaselining to it would sanction a date that has already been disproved.
        var newDates = milestones
            .Select(m => (m.Id, m.Name, Date: m.ActualDate ?? m.ForecastDate))
            .ToList();

        var baseline = BuildBaseline(
            request.ProjectId,
            nextRevision,
            request.Name,
            request.Reason,
            // Approver and date travel together or not at all: a date with nobody attached, or a
            // name with no date, is a half-record that reads as authority without being any.
            request.ApprovedDate.HasValue ? userId : null,
            request.ApprovedDate,
            isCurrent: true,
            createdBy: userId,
            dates: newDates);

        _db.ProgrammeBaselines.Add(baseline);

        foreach (var milestone in milestones)
        {
            milestone.BaselineDate = milestone.ActualDate ?? milestone.ForecastDate;
            milestone.ModifiedBy = userId;
            milestone.ModifiedDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return baseline.Id;
    }

    private static ProgrammeBaseline BuildBaseline(
        Guid projectId,
        int revision,
        string name,
        string reason,
        Guid? approvedBy,
        DateOnly? approvedDate,
        bool isCurrent,
        Guid createdBy,
        IEnumerable<(Guid Id, string Name, DateOnly Date)> dates) =>
        new()
        {
            ProjectId = projectId,
            Revision = revision,
            Name = name,
            Reason = reason,
            ApprovedBy = approvedBy,
            ApprovedDate = approvedDate,
            IsCurrent = isCurrent,
            CreatedBy = createdBy,
            Entries = dates
                .Select(d => new ProgrammeBaselineEntry
                {
                    MilestoneId = d.Id,
                    // Copied, not joined. See ProgrammeBaselineEntry.MilestoneName.
                    MilestoneName = d.Name,
                    BaselineDate = d.Date,
                    CreatedBy = createdBy,
                })
                .ToList(),
        };
}
