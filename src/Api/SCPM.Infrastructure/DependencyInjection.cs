using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCPM.Application.Common.Interfaces;
using SCPM.Infrastructure.BackgroundJobs;
using SCPM.Infrastructure.Identity;
using SCPM.Infrastructure.Persistence;
using SCPM.Infrastructure.Persistence.Interceptors;
using SCPM.Infrastructure.Reporting;
using SCPM.Infrastructure.Storage;

namespace SCPM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:SqlServer.");

        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IClaimsTransformation, EntraClaimsTransformation>();

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                SchemaName = "Hangfire",
                PrepareSchemaIfNecessary = true
            }));
        services.AddHangfireServer();

        // Registered explicitly (not just constructed ad hoc) so Hangfire's ASP.NET Core job
        // activator — which resolves job types via the DI container, not Activator.CreateInstance
        // — can find it and inject the scoped IAppDbContext/ISender it depends on.
        services.AddScoped<SnapshotJobs>();

        services.Configure<BlobStorageOptions>(configuration.GetSection("BlobStorage"));
        services.AddScoped<IDocumentStore, AzureBlobDocumentStore>();
        services.AddScoped<IBlobArchiveStore, AzureBlobArchiveStore>();

        services.AddScoped<ICommitteeReportExporter, CommitteeReportExporter>();

        return services;
    }
}
