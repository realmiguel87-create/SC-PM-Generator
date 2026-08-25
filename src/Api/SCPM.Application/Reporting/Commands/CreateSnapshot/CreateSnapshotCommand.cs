using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.Reporting.Commands.CreateSnapshot;

/// <summary>
/// Captures a named, point-in-time snapshot of a project's key figures and register position.
/// Used both for user-initiated manual snapshots (ReportingController) and scheduled ones
/// (SCPM.Infrastructure.BackgroundJobs.SnapshotJobs, via Hangfire).
/// </summary>
public record CreateSnapshotCommand(Guid ProjectId, SnapshotType Type, string Label) : IRequest<Guid>;

public class CreateSnapshotCommandHandler : IRequestHandler<CreateSnapshotCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateSnapshotCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateSnapshotCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {request.ProjectId} not found.");

        var snapshot = new Snapshot
        {
            ProjectId = project.Id,
            Type = request.Type,
            Label = request.Label,
            CapturedAt = DateTime.UtcNow,
            RibaStageAtCapture = project.CurrentRibaStage,
            ApprovedBudgetAtCapture = project.ApprovedBudget,
            ForecastCostAtCapture = project.ForecastCost,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        await CaptureRegisterMetricsAsync(snapshot, project.Id, cancellationToken);

        _db.Snapshots.Add(snapshot);
        await _db.SaveChangesAsync(cancellationToken);

        return snapshot.Id;
    }

    /// <summary>
    /// Fills in the register aggregates. Counts and totals are computed by the database: the
    /// scheduled job snapshots every active project on every run, so pulling whole registers back
    /// to count them in memory would scale with the size of the programme rather than with the
    /// handful of figures actually wanted.
    ///
    /// Soft-deleted rows drop out automatically — all of these entities carry EF Core global query
    /// filters (AppDbContext.OnModelCreating), so a deleted risk stops being counted without this
    /// code having to remember to exclude it.
    ///
    /// Milestones are the deliberate exception; see below.
    /// </summary>
    private async Task CaptureRegisterMetricsAsync(
        Snapshot snapshot, Guid projectId, CancellationToken cancellationToken)
    {
        var openRisks = _db.Risks.Where(r => r.ProjectId == projectId).Where(SnapshotMetrics.IsOpenRisk);

        snapshot.OpenRiskCount = await openRisks.CountAsync(cancellationToken);

        // Risk.Score is a computed property rather than a mapped column, so the multiplication is
        // written out here for EF Core to translate into SQL.
        snapshot.HighRiskCount = await openRisks
            .CountAsync(r => r.Probability * r.Impact >= SnapshotMetrics.HighRiskScoreThreshold, cancellationToken);

        snapshot.TotalOpenRiskScore = await openRisks
            .SumAsync(r => r.Probability * r.Impact, cancellationToken);

        var openIssues = _db.Issues.Where(i => i.ProjectId == projectId).Where(SnapshotMetrics.IsOpenIssue);

        snapshot.OpenIssueCount = await openIssues.CountAsync(cancellationToken);
        snapshot.SevereOpenIssueCount = await openIssues
            .Where(SnapshotMetrics.IsSevereIssue)
            .CountAsync(cancellationToken);

        // Milestones are materialised rather than aggregated in SQL. The delay calculation works
        // on DateOnly.DayNumber, which EF Core cannot translate, and re-expressing it as a SQL
        // date difference here would put a second, subtly different definition of "delayed" in
        // the codebase — precisely the drift SnapshotMetrics exists to prevent. A project's
        // milestone count is small and bounded, so reading them is cheap; the registers above are
        // neither, and are not read.
        var milestones = await _db.Milestones
            .Where(m => m.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        snapshot.MilestoneCount = milestones.Count;
        snapshot.MilestonesCompleteCount = milestones.Count(m => m.Status == MilestoneStatus.Complete);
        snapshot.MilestonesDelayedCount = milestones.Count(SnapshotMetrics.IsDelayed);

        // DefaultIfEmpty covers a project with no milestones. The clamp to 0 covers a project
        // where everything is ahead of baseline: a "worst delay" of -12 days would read as a
        // delay when it is the opposite.
        snapshot.WorstMilestoneDelayDays = Math.Max(
            0, milestones.Select(SnapshotMetrics.DelayDays).DefaultIfEmpty(0).Max());

        snapshot.OpenEarlyWarningCount = await _db.EarlyWarnings
            .Where(e => e.ProjectId == projectId)
            .Where(SnapshotMetrics.IsOpenEarlyWarning)
            .CountAsync(cancellationToken);

        var compensationEvents = _db.CompensationEvents.Where(c => c.ProjectId == projectId);

        snapshot.OpenCompensationEventCount = await compensationEvents
            .Where(SnapshotMetrics.IsOpenCompensationEvent)
            .CountAsync(cancellationToken);

        snapshot.CompensationEventValue = await compensationEvents
            .Where(SnapshotMetrics.CarriesCompensationEventValue)
            .SumAsync(c => c.EstimatedValue, cancellationToken);

        var variations = _db.Variations.Where(v => v.ProjectId == projectId);

        snapshot.OpenVariationCount = await variations
            .Where(SnapshotMetrics.IsOpenVariation)
            .CountAsync(cancellationToken);

        snapshot.VariationValue = await variations.SumAsync(v => v.ValueImpact, cancellationToken);

        // DaysAwarded is null until an extension of time is determined, so an undetermined
        // register sums to 0 days rather than producing a null.
        snapshot.ExtensionOfTimeDaysAwarded = await _db.ExtensionsOfTime
            .Where(x => x.ProjectId == projectId)
            .SumAsync(x => x.DaysAwarded ?? 0, cancellationToken);
    }
}
