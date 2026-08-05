using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SCPM.Infrastructure.Persistence;

namespace SCPM.IntegrationTests;

/// <summary>
/// Boots the real SCPM.Api host — real DI container, real controllers, real RequireRole()
/// policies, real EntraClaimsTransformation — against a dedicated SQL Server database, with only
/// token *validation* swapped for TestAuthHandler. Everything after "is this a valid token" runs
/// unmodified, which is the entire point: this is what catches wiring bugs (like the
/// Name-vs-DisplayName role mismatch fixed alongside this test suite) that handler-level unit
/// tests, which never touch ASP.NET Core's authorization pipeline, structurally cannot catch.
///
/// Requires a real SQL Server reachable at the connection string below — see
/// SCPM_INTEGRATIONTESTS_CONNECTION env var to override (used by CI's service container; see
/// .github/workflows/ci.yml).
/// </summary>
public class ScpmWebApplicationFactory : WebApplicationFactory<Program>
{
    public static string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("SCPM_INTEGRATIONTESTS_CONNECTION")
        ?? "Server=127.0.0.1,1433;Database=SCPM_IntegrationTests;User Id=sa;Password=SCPM_Passw0rd!;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting (not ConfigureAppConfiguration) — it's backed by an in-memory source that
        // WebApplicationFactory guarantees wins over appsettings.json regardless of the minimal
        // hosting model's own configuration build order, which ConfigureAppConfiguration doesn't.
        builder.UseSetting("ConnectionStrings:SqlServer", ConnectionString);

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    /// <summary>Drops and recreates the test database from the current EF model, then applies
    /// migrations — so every test run starts from a known, model-accurate schema (including
    /// HasData seeds) regardless of what a previous run left behind.
    ///
    /// The drop/create happens over a raw connection to `master`, entirely before this method
    /// ever touches `Services` — accessing `Services` is what boots the host, and Program.cs
    /// wires up Hangfire's SQL Server storage (which opens a connection immediately, to
    /// register recurring jobs) as part of that boot. If the database doesn't exist yet at that
    /// point, host startup itself throws, before EnsureDeleted/Migrate ever get a chance to run.</summary>
    public async Task ResetDatabaseAsync()
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using (var connection = new SqlConnection(builder.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                $"IF DB_ID('{databaseName}') IS NOT NULL " +
                $"BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END; " +
                $"CREATE DATABASE [{databaseName}];";
            await command.ExecuteNonQueryAsync();
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
