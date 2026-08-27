using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.ProgrammeManagement.Commands.RecordDelayCause;

/// <summary>
/// Records that some number of a milestone's slipped days are accounted for by a particular cause,
/// optionally evidenced by an extension-of-time claim or a compensation event.
/// </summary>
public record RecordDelayCauseCommand(
    Guid MilestoneId,
    int DelayDays,
    DelayCauseCategory Category,
    string Narrative,
    Guid? ExtensionOfTimeId,
    Guid? CompensationEventId) : IRequest<Guid>;

public class RecordDelayCauseCommandHandler : IRequestHandler<RecordDelayCauseCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RecordDelayCauseCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(RecordDelayCauseCommand request, CancellationToken cancellationToken)
    {
        var milestone = await _db.Milestones
            .FirstOrDefaultAsync(m => m.Id == request.MilestoneId, cancellationToken)
            ?? throw new InvalidOperationException($"Milestone {request.MilestoneId} was not found.");

        if (request.ExtensionOfTimeId.HasValue && request.CompensationEventId.HasValue)
        {
            // One cause, one piece of evidence. A record pointing at both an SBCC claim and an
            // NEC4 event is describing two different contracts at once, which no single project
            // is administered under — it is a data-entry error, not a scenario.
            throw new InvalidOperationException(
                "A delay cause can cite an extension of time or a compensation event, not both.");
        }

        // Both links are checked against the milestone's own project. Without this a cause could
        // cite a claim from an entirely different scheme, and the resulting analysis would look
        // perfectly well-formed while attributing one project's delay to another's paperwork —
        // the kind of error that survives review precisely because nothing about it looks wrong.
        if (request.ExtensionOfTimeId.HasValue)
        {
            var belongs = await _db.ExtensionsOfTime.AnyAsync(
                e => e.Id == request.ExtensionOfTimeId && e.ProjectId == milestone.ProjectId,
                cancellationToken);

            if (!belongs)
            {
                throw new InvalidOperationException(
                    "That extension of time does not belong to this milestone's project.");
            }
        }

        if (request.CompensationEventId.HasValue)
        {
            var belongs = await _db.CompensationEvents.AnyAsync(
                e => e.Id == request.CompensationEventId && e.ProjectId == milestone.ProjectId,
                cancellationToken);

            if (!belongs)
            {
                throw new InvalidOperationException(
                    "That compensation event does not belong to this milestone's project.");
            }
        }

        var cause = new MilestoneDelayCause
        {
            MilestoneId = request.MilestoneId,
            DelayDays = request.DelayDays,
            Category = request.Category,
            Narrative = request.Narrative,
            ExtensionOfTimeId = request.ExtensionOfTimeId,
            CompensationEventId = request.CompensationEventId,
            CreatedBy = _currentUser.UserId ?? Guid.Empty,
        };

        // Deliberately no check that attributions stay within the milestone's slip. Over-attribution
        // is reported by the analysis rather than prevented here, because the causes are entered one
        // at a time and often before the slip settles: refusing the fourth entry because the first
        // three already covered the delay would block a contract administrator from recording what
        // they know, and would do it at the least convenient moment.
        _db.MilestoneDelayCauses.Add(cause);
        await _db.SaveChangesAsync(cancellationToken);

        return cause.Id;
    }
}
