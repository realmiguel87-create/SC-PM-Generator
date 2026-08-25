using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence;

/// <summary>
/// Reads registers out of SQL Server's temporal history with `FOR SYSTEM_TIME AS OF`.
///
/// Nothing extra is stored to make this work. Every register here is already configured as a
/// system-versioned temporal table (see the IsTemporal calls in Persistence/Configurations), so
/// the history is a by-product of ordinary writes rather than something snapshots have to
/// duplicate. That has a second benefit worth stating: unlike the aggregate columns on Snapshot,
/// which only exist from the moment they were added, this works for snapshots taken at any point
/// in the project's life — the history was being kept whether or not anyone was going to ask.
///
/// AsNoTracking throughout: these are historical rows, they are read to be compared, and letting
/// the change tracker take them would put versions of an entity in the context that must never be
/// saved back over the live row.
/// </summary>
public class SqlServerRegisterHistory : IRegisterHistory
{
    private readonly AppDbContext _db;

    public SqlServerRegisterHistory(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Risk>> RisksAsOfAsync(
        Guid projectId, DateTime asOfUtc, CancellationToken cancellationToken) =>
        await _db.Risks
            .TemporalAsOf(asOfUtc)
            .AsNoTracking()
            .Where(r => r.ProjectId == projectId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Milestone>> MilestonesAsOfAsync(
        Guid projectId, DateTime asOfUtc, CancellationToken cancellationToken) =>
        await _db.Milestones
            .TemporalAsOf(asOfUtc)
            .AsNoTracking()
            .Where(m => m.ProjectId == projectId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EarlyWarning>> EarlyWarningsAsOfAsync(
        Guid projectId, DateTime asOfUtc, CancellationToken cancellationToken) =>
        await _db.EarlyWarnings
            .TemporalAsOf(asOfUtc)
            .AsNoTracking()
            .Where(e => e.ProjectId == projectId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CompensationEvent>> CompensationEventsAsOfAsync(
        Guid projectId, DateTime asOfUtc, CancellationToken cancellationToken) =>
        await _db.CompensationEvents
            .TemporalAsOf(asOfUtc)
            .AsNoTracking()
            .Where(c => c.ProjectId == projectId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Variation>> VariationsAsOfAsync(
        Guid projectId, DateTime asOfUtc, CancellationToken cancellationToken) =>
        await _db.Variations
            .TemporalAsOf(asOfUtc)
            .AsNoTracking()
            .Where(v => v.ProjectId == projectId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ExtensionOfTime>> ExtensionsOfTimeAsOfAsync(
        Guid projectId, DateTime asOfUtc, CancellationToken cancellationToken) =>
        await _db.ExtensionsOfTime
            .TemporalAsOf(asOfUtc)
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .ToListAsync(cancellationToken);

    public Task<IReadOnlyList<Risk>> RiskVersionsBetweenAsync(
        Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        VersionsBetweenAsync(_db.Risks, fromUtc, toUtc, r => r.ProjectId == projectId, cancellationToken);

    public Task<IReadOnlyList<Milestone>> MilestoneVersionsBetweenAsync(
        Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        VersionsBetweenAsync(_db.Milestones, fromUtc, toUtc, m => m.ProjectId == projectId, cancellationToken);

    public Task<IReadOnlyList<EarlyWarning>> EarlyWarningVersionsBetweenAsync(
        Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        VersionsBetweenAsync(_db.EarlyWarnings, fromUtc, toUtc, e => e.ProjectId == projectId, cancellationToken);

    public Task<IReadOnlyList<CompensationEvent>> CompensationEventVersionsBetweenAsync(
        Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        VersionsBetweenAsync(_db.CompensationEvents, fromUtc, toUtc, c => c.ProjectId == projectId, cancellationToken);

    public Task<IReadOnlyList<Variation>> VariationVersionsBetweenAsync(
        Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        VersionsBetweenAsync(_db.Variations, fromUtc, toUtc, v => v.ProjectId == projectId, cancellationToken);

    public Task<IReadOnlyList<ExtensionOfTime>> ExtensionOfTimeVersionsBetweenAsync(
        Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        VersionsBetweenAsync(_db.ExtensionsOfTime, fromUtc, toUtc, x => x.ProjectId == projectId, cancellationToken);

    /// <summary>
    /// Every version of every row in the window, via `FOR SYSTEM_TIME BETWEEN`. The window is
    /// ordered before it is passed to SQL Server, which rejects a range whose start is after its
    /// end — and a caller comparing a later snapshot against an earlier one is doing something
    /// legitimate, not something to fail on.
    ///
    /// AsNoTracking is not optional here, unlike in the AsOf reads where it is merely correct:
    /// this returns several versions of the same entity, all sharing one primary key, and the
    /// change tracker cannot hold those simultaneously.
    /// </summary>
    private static async Task<IReadOnlyList<T>> VersionsBetweenAsync<T>(
        DbSet<T> set,
        DateTime fromUtc,
        DateTime toUtc,
        Expression<Func<T, bool>> forProject,
        CancellationToken cancellationToken) where T : class
    {
        var (start, end) = fromUtc <= toUtc ? (fromUtc, toUtc) : (toUtc, fromUtc);

        return await set
            .TemporalBetween(start, end)
            .AsNoTracking()
            .Where(forProject)
            .ToListAsync(cancellationToken);
    }
}
