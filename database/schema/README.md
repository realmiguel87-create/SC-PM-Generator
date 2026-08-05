# Schema — EF Core migrations are the source of truth

This folder used to hold hand-written CREATE TABLE / CREATE SCHEMA scripts alongside an
independent EF Core model describing the same tables. That's two authorities for one
schema with nothing keeping them in sync — a drift risk with no upside. It's been resolved
in favour of EF Core:

**EF Core migrations, generated from `src/Api/SCPM.Domain` entities and
`src/Api/SCPM.Infrastructure/Persistence/Configurations/*.cs`, are the only authoritative
definition of every table, column, constraint, index, and temporal table.** Nothing in this
repository hand-writes `CREATE TABLE` anymore.

## Generating and applying migrations

```bash
cd src/Api
dotnet ef migrations add InitialCreate --project SCPM.Infrastructure --startup-project SCPM.Api
dotnet ef database update --project SCPM.Infrastructure --startup-project SCPM.Api
```

Migration output lands in `src/Api/SCPM.Infrastructure/Migrations/` and is
committed like any other source file. `SCPM.Infrastructure/Persistence/AppDbContextFactory.cs`
lets the `dotnet ef` CLI construct `AppDbContext` at design time without booting the full
`SCPM.Api` host.

Role and RIBA stage reference data (previously seeded via hand-written `INSERT` statements)
is now seeded through `HasData(...)` in `RoleConfiguration` and `RibaStageDefinitionConfiguration`
— it ships as part of the generated migration, not a separate script to remember to run.

## What still lives as hand-written SQL, and why that's fine

`database/views/` and `database/procedures/` remain hand-written `.sql` files. This is not
the same problem: they don't redefine anything EF owns — they're read/write logic layered
*on top of* EF-migrated tables (reporting views, and the stage-gate approval procedure kept
for the case where atomic multi-table SQL is preferred over an EF Core round-trip). EF Core
migrations can't express views or stored procedures declaratively, so these are applied as a
deployment step after `dotnet ef database update`:

```bash
sqlcmd -S <server> -d SCPM -i database/views/010_Projects_Views.sql
sqlcmd -S <server> -d SCPM -i database/procedures/010_Governance_ApproveGateway.sql
```

(Phase 2+ will fold this into the CI/CD pipeline as a post-migration step — see
`docs/roadmap.md`.)
