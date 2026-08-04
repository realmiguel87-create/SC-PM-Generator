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

builder.Services.AddControllers();
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
