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
| Storage | SharePoint Online (primary), Azure Blob Storage (archive) |
| Reporting | QuestPDF, OpenXML SDK, ClosedXML, Power BI |
| Background Jobs | Hangfire |
| Hosting | Azure App Service, Azure SQL, Key Vault, Application Insights |

## Repository Layout

```
docs/                    Architecture, ERD, roadmap, module specs
database/
  schema/                DDL per schema (Security, Projects, Governance, Cost, ...)
  procedures/            Stored procedures
  views/                 Reporting/consumption views
  migrations/            EF Core migration output (generated)
src/
  Api/
    SCPM.Domain/          Entities, enums, domain events — no framework dependencies
    SCPM.Application/     CQRS handlers (MediatR), validators (FluentValidation), DTOs
    SCPM.Infrastructure/  EF Core, SharePoint/Blob clients, report generators, Hangfire jobs
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

### API
```bash
cd src/Api
dotnet restore
dotnet ef database update --project SCPM.Infrastructure --startup-project SCPM.Api
dotnet run --project SCPM.Api
```

### Web
```bash
cd src/Web
npm install
npm run dev
```

See [`docs/architecture.md`](docs/architecture.md) for the full solution architecture and [`docs/erd.md`](docs/erd.md) for the data model.
