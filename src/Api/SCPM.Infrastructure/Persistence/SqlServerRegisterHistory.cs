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
}
