using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SCPM.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` / `dotnet ef database update` construct AppDbContext without
/// spinning up the full SCPM.Api host (its DI container needs Entra ID config, Hangfire storage,
/// etc. that migration tooling shouldn't have to care about).
///
/// The connection string comes from <see cref="DesignTimeConnectionString"/> rather than being
/// hardcoded here. That matters more than it looks: EF prefers this factory over the startup
/// project's host builder, so anything hardcoded here would win over `--startup-project`,
/// `ASPNETCORE_ENVIRONMENT` and user secrets alike, without a word of explanation. See that class
/// for the resolution order.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(DesignTimeConnectionString.Resolve());

        return new AppDbContext(optionsBuilder.Options);
    }
}
