using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SCPM.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` / `dotnet ef database update` construct AppDbContext without
/// spinning up the full SCPM.Api host (its DI container needs Entra ID config, Hangfire storage,
/// etc. that migration tooling shouldn't have to care about). The connection string here only
/// needs to be valid enough for EF to generate SQL Server migrations — it is never connected to
/// by `migrations add`, and `database update` reads the real one from SCPM.Api's configuration
/// when run with --startup-project SCPM.Api (see README.md).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=SCPM;Trusted_Connection=True;TrustServerCertificate=True");

        return new AppDbContext(optionsBuilder.Options);
    }
}
