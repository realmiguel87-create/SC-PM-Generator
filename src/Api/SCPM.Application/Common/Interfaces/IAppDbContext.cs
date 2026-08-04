using Microsoft.EntityFrameworkCore;
using SCPM.Domain.Entities;

namespace SCPM.Application.Common.Interfaces;

/// <summary>
/// Application-facing view of the persistence context. Infrastructure's AppDbContext
/// implements this so handlers depend on an abstraction, not EF Core directly.
/// </summary>
public interface IAppDbContext
{
    DbSet<Programme> Programmes { get; }
    DbSet<Project> Projects { get; }
    DbSet<RibaStageDefinition> RibaStageDefinitions { get; }
    DbSet<RibaStageInstance> RibaStageInstances { get; }
    DbSet<Gateway> Gateways { get; }
    DbSet<Approval> Approvals { get; }
    DbSet<User> Users { get; }
    DbSet<CostPlan> CostPlans { get; }
    DbSet<CostPlanLine> CostPlanLines { get; }
    DbSet<Forecast> Forecasts { get; }
    DbSet<Milestone> Milestones { get; }
    DbSet<DecisionRegisterEntry> DecisionRegisterEntries { get; }
    DbSet<Snapshot> Snapshots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
