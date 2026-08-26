using Microsoft.Extensions.Configuration;

namespace SCPM.Infrastructure.Persistence;

/// <summary>
/// Works out which database the EF Core design-time tools should talk to.
///
/// This exists because of a trap that is easy to fall into and unpleasant to diagnose. An
/// <see cref="Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory{TContext}"/> takes
/// priority over the startup project's host builder — EF finds the factory first and never builds
/// the host at all. So a factory with a hardcoded connection string silently sends every
/// `dotnet ef database update` to that string, no matter what `--startup-project` is passed, what
/// `ASPNETCORE_ENVIRONMENT` is set to, or what sits in user secrets. The failure surfaces as a
/// connection error naming a server nobody configured, which reads like a broken machine rather
/// than a wiring mistake.
///
/// So the connection string is resolved from configuration instead, in this order:
///
///   1. <c>ConnectionStrings__SqlServer</c> in the environment — how CI and containers pass it.
///   2. SCPM.Api's user secrets — how a developer holds a real connection string without
///      committing it.
///   3. A LocalDB fallback, so `dotnet ef migrations add` still works on a machine with neither.
///
/// The fallback is genuinely only a fallback: `migrations add` never opens a connection, it only
/// needs a provider to generate SQL Server syntax. `database update` does connect, and on a
/// machine with no LocalDB it will fail — correctly, because at that point nothing has told it
/// where the database actually is.
///
/// `--connection` still overrides all of this; EF applies it after the factory returns.
/// </summary>
public static class DesignTimeConnectionString
{
    /// <summary>
    /// Must match &lt;UserSecretsId&gt; in SCPM.Api.csproj. Repeated as a literal rather than read
    /// from the assembly because Infrastructure does not reference Api — the dependency runs the
    /// other way, and inverting it to save a duplicated string would be a poor trade.
    /// </summary>
    public const string ApiUserSecretsId = "scpm-api-secrets";

    /// <summary>The name SCPM.Api uses for this connection string, in every configuration source.</summary>
    public const string ConnectionName = "SqlServer";

    /// <summary>
    /// Used only when nothing else supplies a connection string. Enough for EF to emit SQL Server
    /// syntax during `migrations add`; not expected to be reachable.
    /// </summary>
    public const string LocalDbFallback =
        "Server=(localdb)\\mssqllocaldb;Database=SCPM;Trusted_Connection=True;TrustServerCertificate=True";

    /// <summary>
    /// Environment variables last so they win: CI and container runs set them, and a developer's
    /// user secrets should not quietly override what a pipeline explicitly passed.
    ///
    /// SCPM.Api's appsettings.json is deliberately not read. Its SqlServer entry is the same
    /// LocalDB placeholder used above, so loading it would add a step that changes nothing while
    /// requiring this class to guess at a relative path to another project's files.
    /// </summary>
    public static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            // No `optional` parameter on this overload, and none needed: a missing secrets file is
            // already treated as an empty source rather than an error. Verified against the
            // installed Microsoft.Extensions.Configuration.UserSecrets, not assumed.
            .AddUserSecrets(ApiUserSecretsId)
            .AddEnvironmentVariables()
            .Build();

    /// <summary>
    /// Picks the configured connection string, or the fallback when there isn't one.
    /// </summary>
    public static string Resolve(IConfiguration configuration) =>
        // An empty or whitespace value is treated as absent. A blank environment variable is a
        // common way for a pipeline to "unset" something, and passing it through would produce an
        // ArgumentException from SqlConnection rather than a usable fallback.
        configuration.GetConnectionString(ConnectionName) is { } configured
        && !string.IsNullOrWhiteSpace(configured)
            ? configured
            : LocalDbFallback;

    /// <summary>Resolves from the ambient environment and user secrets.</summary>
    public static string Resolve() => Resolve(BuildConfiguration());
}
