using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Programme> Programmes => Set<Programme>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<RibaStageDefinition> RibaStageDefinitions => Set<RibaStageDefinition>();
    public DbSet<RibaStageInstance> RibaStageInstances => Set<RibaStageInstance>();
    public DbSet<Gateway> Gateways => Set<Gateway>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<ActivityLogEntry> ActivityLog => Set<ActivityLogEntry>();
    public DbSet<FieldAuditEntry> FieldAudit => Set<FieldAuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global soft-delete filters — governance-critical entities are never
        // physically deleted; queries transparently exclude them unless a caller
        // explicitly opts out with IgnoreQueryFilters() for admin/audit views.
        modelBuilder.Entity<Programme>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RibaStageInstance>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Gateway>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Approval>().HasQueryFilter(e => !e.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }
}
