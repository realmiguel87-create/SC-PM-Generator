# Stirling Council Capital Projects Management Platform (SC-PM-Generator)

An enterprise platform managing the complete lifecycle of capital projects from **RIBA Stage 0 (Strategic Definition)** through **RIBA Stage 7 (Use)**, providing a single source of truth for governance, cost, programme, risk, stakeholder management, NEC4/SBCC contract administration, committee reporting and portfolio management for a Scottish local authority capital programme (£500m+).

## Status

This repository is being built in phases (see [`docs/roadmap.md`](docs/roadmap.md)). Phase 1 delivers the enterprise architecture, database foundation, and a working end-to-end vertical slice (Projects + RIBA Stages) across the full stack, so every subsequent module lands on a proven pattern rather than a paper design.

## Technology Stack

| Layer | Technology |
|---|---|
| Frontend | React 18, TypeScript, Tailwind CSS, ShadCN UI, TanStack Query, React Router, Recharts |
| Backend | ASP.NET Core 9 Web API, MediatR (CQRS), FluentValidation, EF Core 9 |
| Database | SQL Server, System-Versioned Temporal Tables, Stored Procedures, Views, Schemas |
| Auth | Microsoft Entra ID (SSO, OIDC), Role-Based Access Control |
| Storage | Azure Blob Storage (active tier + archive tier) |
| Reporting | QuestPDF, OpenXML SDK, ClosedXML, Power BI |
| Background Jobs | Hangfire |
| Hosting | Azure App Service, Azure SQL, Key Vault, Application Insights |

## Repository Layout

```
docs/                    Architecture, ERD, roadmap, module specs
database/
  schema/                README explaining why EF Core migrations are the schema's source of truth
  procedures/            Stored procedures (hand-written, layered on top of EF-migrated tables)
  views/                 Reporting/consumption views (same)
src/
  Api/
    SCPM.Domain/          Entities, enums, domain events — no framework dependencies
    SCPM.Application/     CQRS handlers (MediatR), validators (FluentValidation), DTOs
    SCPM.Infrastructure/  EF Core (+ Migrations, the generated schema source of truth),
                           Blob Storage clients, report generators, Hangfire jobs
    SCPM.Api/              Controllers, auth, middleware, composition root
  Web/                    React + TypeScript SPA
tests/
  SCPM.UnitTests/
  SCPM.IntegrationTests/
  SCPM.ApiTests/
infrastructure/
  bicep/                 Azure IaC
  github-actions/        Reusable workflow fragments
.github/workflows/       CI/CD pipelines
```

## Getting Started

### Local configuration

The connection strings in `appsettings.json` are non-working placeholders on purpose — a real
Azure SQL connection string and a storage account key are both secrets and must not live in the
repository. Supply them per-developer with user secrets, which override `appsettings.json`
and are stored outside the project tree:

```bash
cd src/Api/SCPM.Api
dotnet user-secrets set "ConnectionStrings:SqlServer" "<azure sql connection string>"
dotnet user-secrets set "BlobStorage:ConnectionString" "<storage account connection string>"
dotnet user-secrets list
```

User secrets are only loaded when the environment is `Development` — which
`Properties/launchSettings.json` sets, along with pinning the ports below. Running the API
some other way (`dotnet SCPM.Api.dll`, a container, a published build) bypasses launch
profiles entirely and needs `ASPNETCORE_ENVIRONMENT` and configuration supplied by other means.

Trust the ASP.NET Core development certificate once per machine, or HTTPS on 5001 will fail
and the SPA's proxy will have nothing to talk to:

```bash
dotnet dev-certs https --trust
```

| Component | URL |
| --- | --- |
| API (HTTPS) | `https://localhost:5001` — the SPA's Vite proxy target |
| API (HTTP) | `http://localhost:5000` |
| Swagger | `https://localhost:5001/swagger` (Development only) |
| Web | `http://localhost:5173` |

Port 5173 is not arbitrary: it is registered as the SPA redirect URI on the Entra ID app
registration and listed in `Cors:AllowedOrigins`. Vite silently moves to 5174 if 5173 is
already in use, which produces an `AADSTS50011` redirect-URI mismatch at sign-in — check the
port Vite actually reports rather than assuming.

### API
```bash
cd src/Api
dotnet restore

# EF Core migrations are the schema's source of truth (see database/schema/README.md).
# First run only — generates src/Api/SCPM.Infrastructure/Migrations/:
dotnet ef migrations add InitialCreate --project SCPM.Infrastructure --startup-project SCPM.Api

dotnet ef database update --project SCPM.Infrastructure --startup-project SCPM.Api

# Views and the stage-gate stored procedure aren't EF-managed; apply them once after the
# tables exist (see database/schema/README.md for why these stay hand-written SQL):
# (adjust -S if your database isn't LocalDB — see "Which database do the EF tools use?" below)
sqlcmd -S "(localdb)\mssqllocaldb" -d SCPM -i ../../database/views/010_Projects_Views.sql
sqlcmd -S "(localdb)\mssqllocaldb" -d SCPM -i ../../database/procedures/010_Governance_ApproveGateway.sql

dotnet run --project SCPM.Api
```

#### Which database do the EF tools use?

`dotnet ef` does **not** read `launchSettings.json`, so `ASPNETCORE_ENVIRONMENT` is unset when it
runs and none of the Development-only configuration applies. The connection string is resolved by
`DesignTimeConnectionString`, in this order:

1. **`ConnectionStrings__SqlServer` in the environment.** How CI and containers supply it. The
   double underscore is the configuration system's separator — it lands at
   `ConnectionStrings:SqlServer`.
2. **SCPM.Api's user secrets.** How to hold a real connection string without committing it:
   ```bash
   cd src/Api/SCPM.Api
   dotnet user-secrets set "ConnectionStrings:SqlServer" 'Server=...;Database=...;User Id=...;Password=...'
   ```
   Note the single quotes — a connection string contains characters your shell will otherwise eat.
3. **A LocalDB fallback**, so `dotnet ef migrations add` works on a machine with neither. That
   command never opens a connection; it only needs a provider to generate SQL Server syntax.
   `database update` *does* connect, and will fail here if LocalDB isn't installed — correctly,
   because at that point nothing has said where the database is.

`--connection "..."` overrides all three. Reach for it for a one-off against another database, not
as the everyday route — if you find yourself needing it every time, something in the list above
isn't set.

> This used to be a trap. `AppDbContextFactory` hardcoded the LocalDB string, and because an
> `IDesignTimeDbContextFactory` takes priority over the startup project's host builder, EF never
> built the host at all — every `dotnet ef database update` went to LocalDB while appearing to
> honour `--startup-project` and user secrets. It surfaced as a connection error naming a server
> nobody had configured.

### Web
```bash
cd src/Web
npm install
npm run dev
```

See [`docs/architecture.md`](docs/architecture.md) for the full solution architecture and [`docs/erd.md`](docs/erd.md) for the data model.
