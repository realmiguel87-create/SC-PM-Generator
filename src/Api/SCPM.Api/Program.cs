using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using SCPM.Api.Middleware;
using SCPM.Application;
using SCPM.Infrastructure;

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

// Snapshot Engine v1 scheduled jobs (docs/roadmap.md Phase 2) are registered by
// RecurringJobRegistrationService, a hosted service wired up in AddInfrastructure. It is not done
// inline here because registration is a database write: doing it once on the startup path means
// doing it at the one moment least likely to succeed, and a transient outage then left the jobs
// unregistered until someone restarted the process. The hosted service retries until it succeeds.
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
