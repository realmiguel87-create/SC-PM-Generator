using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using SCPM.Api.Middleware;
using SCPM.Application;
using SCPM.Infrastructure;
using SCPM.Infrastructure.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

// --- Authentication: Microsoft Entra ID (SSO, JWT bearer) ---
builder.Services.AddAuthentication(Microsoft.Identity.Web.Constants.Bearer)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("EntraId"));

// --- Authorization: RBAC policies, one per platform role (see docs/architecture.md §7) ---
builder.Services.AddAuthorization(options =>
{
    var roles = new[]
    {
        "Administrator", "Director", "ProjectSponsor", "ProgrammeManager", "ProjectManager",
        "CommercialManager", "QuantitySurveyor", "GovernanceOfficer", "CommitteeOfficer", "ReadOnlyUser"
    };

    foreach (var role in roles)
        options.AddPolicy(role, policy => policy.RequireRole(role));

    options.AddPolicy("CanWrite", policy => policy.RequireRole(
        "Administrator", "Director", "ProjectSponsor", "ProgrammeManager", "ProjectManager",
        "CommercialManager", "QuantitySurveyor", "GovernanceOfficer", "CommitteeOfficer"));

    options.AddPolicy("CanApprove", policy => policy.RequireRole(
        "Administrator", "Director", "ProjectSponsor", "GovernanceOfficer"));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaClient", policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("SpaClient");
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization = [new HangfireAdministratorFilter()]
});

// Snapshot Engine v1 scheduled jobs (docs/roadmap.md Phase 2). Idempotent — AddOrUpdate
// replaces the existing recurring job definition on every app start rather than duplicating it.
//
// Wrapped because AddOrUpdate writes to Hangfire's SQL storage immediately, so an unreachable
// database here used to throw straight out of Program.cs and prevent the API starting at all.
// That is the wrong failure mode: a transient database outage — routine on a serverless Azure
// SQL tier that auto-pauses, and equally possible behind a firewall/network blip — should not
// stop the API serving requests that don't touch the database (health checks, and every
// authentication path, since Entra ID token validation needs no database at all).
//
// Trade-off, stated plainly: if this fails, the recurring jobs are NOT registered and scheduled
// snapshots will not run until the app is restarted against a reachable database. That is worse
// than registering them but better than a dead API, and the error is logged loudly rather than
// swallowed. A background retry would remove the need for a restart — see docs/roadmap.md.
try
{
    RecurringJob.AddOrUpdate<SnapshotJobs>("snapshot-daily", j => j.RunDailySnapshotAsync(CancellationToken.None), Cron.Daily);
    RecurringJob.AddOrUpdate<SnapshotJobs>("snapshot-weekly", j => j.RunWeeklySnapshotAsync(CancellationToken.None), Cron.Weekly);
    RecurringJob.AddOrUpdate<SnapshotJobs>("snapshot-monthly", j => j.RunMonthlySnapshotAsync(CancellationToken.None), Cron.Monthly);
}
catch (Exception ex)
{
    app.Services.GetRequiredService<ILogger<Program>>().LogError(ex,
        "Could not register Hangfire recurring snapshot jobs — the job storage database was unreachable at startup. "
        + "The API will continue to start, but scheduled snapshots will NOT run until it is restarted "
        + "with a reachable database.");
}

app.MapControllers();

app.Run();

/// <summary>Restricts the Hangfire dashboard to Administrator role only.</summary>
public class HangfireAdministratorFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("Administrator");
    }
}

/// <summary>Exposes the top-level-statements-generated Program class to
/// WebApplicationFactory&lt;Program&gt; in SCPM.IntegrationTests — otherwise it's internal and
/// invisible outside this assembly.</summary>
public partial class Program;
