using FluentAssertions;
// For DatabaseFacade.GetConnectionString(). Without it the compiler binds to the identically
// named IConfiguration extension from Microsoft.Extensions.Configuration and asks for its
// missing 'name' argument, which reads like a wrong call rather than a missing using.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCPM.Infrastructure.Persistence;
using Xunit;

namespace SCPM.UnitTests.Persistence;

/// <summary>
/// The connection string EF's design-time tooling uses.
///
/// These matter because the failure they guard against is a quiet one. A design-time factory takes
/// priority over the startup project's host builder, so a hardcoded string in the factory sends
/// every `dotnet ef database update` to that string while appearing to honour `--startup-project`,
/// `ASPNETCORE_ENVIRONMENT` and user secrets. What a developer sees is a connection error naming a
/// server they never configured — which reads as a broken machine, not a wiring bug, and costs
/// real time before anyone thinks to open the factory.
/// </summary>
public class DesignTimeConnectionStringTests
{
    private static IConfiguration ConfigurationWith(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DesignTimeConnectionString.ConnectionName}"] = connectionString,
            })
            .Build();

    [Fact]
    public void Uses_the_configured_connection_string_when_there_is_one()
    {
        var resolved = DesignTimeConnectionString.Resolve(
            ConfigurationWith("Server=tcp:example.database.windows.net,1433;Database=SCPM"));

        resolved.Should().Be("Server=tcp:example.database.windows.net,1433;Database=SCPM");
    }

    [Fact]
    public void Falls_back_to_localdb_when_nothing_is_configured()
    {
        var resolved = DesignTimeConnectionString.Resolve(new ConfigurationBuilder().Build());

        resolved.Should().Be(DesignTimeConnectionString.LocalDbFallback);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Treats_a_blank_connection_string_as_absent(string blank)
    {
        // Clearing an environment variable by setting it empty is common in pipelines. Passing the
        // blank through would reach SqlConnection and throw an ArgumentException about an
        // unsupported keyword, which says nothing about what is actually wrong.
        DesignTimeConnectionString.Resolve(ConfigurationWith(blank))
            .Should().Be(DesignTimeConnectionString.LocalDbFallback);
    }

    [Fact]
    public void Reads_the_connection_string_from_the_environment()
    {
        // ConnectionStrings__SqlServer is how CI and containers supply this — the double
        // underscore is the configuration system's separator convention, so it lands at
        // ConnectionStrings:SqlServer.
        var key = $"ConnectionStrings__{DesignTimeConnectionString.ConnectionName}";
        var original = Environment.GetEnvironmentVariable(key);

        try
        {
            Environment.SetEnvironmentVariable(key, "Server=from-the-environment");

            DesignTimeConnectionString.Resolve(DesignTimeConnectionString.BuildConfiguration())
                .Should().Be("Server=from-the-environment");
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, original);
        }
    }

    [Fact]
    public void Builds_a_configuration_even_when_no_user_secrets_file_exists()
    {
        // CI machines have no secrets store. The user-secrets provider must treat that as an empty
        // source rather than an error, or `dotnet ef` breaks everywhere except a developer laptop.
        var act = () => DesignTimeConnectionString.BuildConfiguration();

        act.Should().NotThrow();
    }

    [Fact]
    public void Points_the_factory_at_the_resolved_connection_string()
    {
        // The whole point of the change: what the factory hands EF is what configuration says, not
        // a literal baked into the factory.
        var key = $"ConnectionStrings__{DesignTimeConnectionString.ConnectionName}";
        var original = Environment.GetEnvironmentVariable(key);

        try
        {
            Environment.SetEnvironmentVariable(
                key, "Server=tcp:factory-test.database.windows.net,1433;Database=SCPM");

            using var context = new AppDbContextFactory().CreateDbContext([]);

            context.Database.GetConnectionString()
                .Should().Contain("factory-test.database.windows.net");
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, original);
        }
    }
}
