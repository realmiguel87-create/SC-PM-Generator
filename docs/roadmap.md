# Delivery Roadmap

This platform's full specification spans 25 deliverables across governance, contract administration (NEC4/SBCC), reporting, document management, audit, Power BI, security, testing, and DevOps. Building all of it as production-grade code in a single pass is not realistic — it is a multi-month, multi-workstream programme. This roadmap sequences delivery so every phase lands a **working, testable increment** rather than a paper design.

## Phase 1 — Foundation (this phase)

Goal: prove the full-stack pattern end-to-end so every later module is a repeat of a known-good shape, not a new architecture.

- [x] Enterprise solution architecture (`docs/architecture.md`)
- [x] Entity-relationship model for the core + Phase 1 schemas (`docs/erd.md`)
- [x] Database schema for `Security`, `Projects`, `Governance`, `Audit` — modelled as EF Core entities/configurations (temporal tables, soft delete, constraints); EF Core migrations are the schema's source of truth, see `database/schema/README.md`
- [x] Core stored procedures + views for the Projects vertical
- [x] .NET solution: Domain / Application / Infrastructure / Api, CQRS via MediatR, FluentValidation
- [x] Entra ID authentication + RBAC middleware
- [x] Audit interceptor (field-level + activity log) wired into EF Core `SaveChanges`
- [x] Projects vertical slice: create/list/get project, RIBA stage progression, stage gate approval — API + React
- [x] React shell: routing, TanStack Query client, ShadCN theme using the Stirling palette, executive dashboard skeleton, project workspace tab shell
- [x] CI pipeline (build, test, lint) via GitHub Actions
- [x] Azure IaC skeleton (Bicep) for App Service, SQL, Key Vault, Blob, App Insights

## Phase 2 — Governance, Cost & Programme

- [x] Governance module: decision register (create/list) — API + Governance tab
- [x] Cost module: baseline cost plans (with lines), forecast recording + history, budget/forecast/variance summary — API + Cost tab
- [x] Programme module: milestones (baseline/forecast/actual dates, delay calculation, status) — API + Programme tab
- [x] Snapshot engine v1: `Snapshot` entity, manual capture (API + Snapshots tab), and scheduled Daily/Weekly/Monthly Hangfire recurring jobs that snapshot every active project
- [ ] Governance module: project mandates, strategic business cases — deferred, not yet modelled
- [ ] Cost module: multi-line budget approval workflow, funding sources/allocations — deferred
- [ ] Programme module: Gantt-style visual timeline (Recharts), delay-cause analysis — deferred; current UI is a milestone table
- [ ] Template generator (QuestPDF/OpenXML/ClosedXML document export) — deferred to Phase 6 (Reporting Centre export engine), so branding/export logic is built once rather than per-module

## Phase 3 — Risk, Issues, Opportunities & Stakeholders

- [x] Risk register (probability/impact, status, mitigation plan) with a Recharts probability×impact heatmap — API + Risks tab
- [x] Issue log (severity, status, resolution) — API + Issues tab
- [x] Opportunity register (potential value, probability, status) — API + Opportunities tab
- [x] Stakeholder register (influence/interest) with an engagement tracker (logged touchpoints) — API + Stakeholders tab
- [x] Escalation: raises a Risk or Issue for a decision above project-team authority (`Risk.Escalation`, distinct from a `Governance.Gateway`), with create/resolve endpoints — not yet surfaced in a tab UI
- [ ] Escalation workflow tied into formal Governance approvals (currently a standalone Pending/Resolved/Withdrawn record, not routed through `Governance.Approval`) — deferred
- [ ] Communications plan, consultation reporting — deferred; only the engagement tracker (a log of what happened) is implemented, not planning/scheduling of future engagement
- [ ] RiskScore history (probability/impact drift over time) — deferred; `Risk.Score` is a live computed value, not yet snapshotted per change the way `Cost.Forecast` is

## Phase 4 — NEC4 & SBCC Contract Administration

- [x] NEC4: Early Warning (raise/close), Compensation Event (notify/status), Contract Data (Part One/Two), Risk Allocation Matrix, Accepted Programme (acceptance log), Payment Assessment (assess/certify), Change Register — API + NEC4 tab (in-tab sub-navigation across all seven registers)
- [x] SBCC: Variation (instruct/status), Extension of Time (claim/award), Loss & Expense (claim), Architect's Instructions (issue), Interim Valuation — API + SBCC tab
- [ ] Export packs (PDF/DOCX/XLSX per register) — deferred to Phase 6 (Reporting Centre export engine), consistent with the Phase 2 template-generator deferral
- [ ] Not every register got a full status lifecycle in the UI (e.g. Change Register status update, EOT partial-award, L&E award) — the API endpoints exist (`UpdateChangeRegisterItemStatus`, `UpdateExtensionOfTimeStatus` with a `DaysAwarded` override) but aren't all wired to a button yet; deferred as UI polish, not a data-model gap

## Phase 5 — Document Management & SharePoint/Blob Integration

- [x] Document/DocumentVersion/DocumentFile model — versions are never overwritten: approving a
      draft bumps its own row to the next major version in place (1.2 Draft -> 2.0 Approved),
      and any previously Approved version moves to Superseded. Physical files are immutable
      per-version rows, one per exported format. API + Documents tab (master/detail: pick a
      document, see its version history, upload files, approve/reject/archive).
- [x] `ISharePointDocumentStore` / `IBlobArchiveStore` abstractions with real implementations
      (`GraphSharePointDocumentStore` via Microsoft Graph, `AzureBlobArchiveStore` via
      Azure.Storage.Blobs) — compiled against the actual SDKs (verified via reflection against
      the installed Microsoft.Graph package, not guessed) but not exercised against a live
      SharePoint tenant, since this environment has none.
- [x] **This phase got real infrastructure to test against for the first time**: a .NET 9 SDK
      and a SQL Server 2022 container. `dotnet ef database update` against that live database
      caught a genuine bug no compiler could — three spots (`Gateway.RibaStageInstance`,
      `Escalation.Risk`/`Issue`, `DocumentVersion.Snapshot`) had multiple cascade delete paths
      converging on `Project`, which SQL Server rejects at `CREATE TABLE` time (error 1785).
      Fixed with `OnDelete(DeleteBehavior.Restrict)` on the redundant FK in each case. The
      migration now applies cleanly to a real database — verified, not assumed — and the CI
      pipeline gained a job that does this on every push so it can't regress silently.
- [ ] SharePoint/Blob integration is unverified beyond "compiles against the real SDK types" —
      needs a real Entra ID app registration + SharePoint site + storage account to actually
      exercise the upload/archive round trip. **Superseded in Phase 7**: SharePoint's Graph
      app-only access needs a tenant admin to grant application-permission consent, which turned
      out not to be obtainable, so the active tier was moved to Azure Blob Storage alongside the
      archive tier — see Phase 7.

## Phase 6 — Committee, Stakeholder & Executive Reporting Centre

- [x] Committee/Cabinet/Board report generator with the standard section structure (Executive
      Summary, Background, Current Position, Finance, Programme, Risk, Stakeholders,
      Sustainability, Equality Impact, Recommendations — Appendices are the report's attached
      DocumentFiles, not a separate field). Finance/Programme/Risk/Stakeholder commentary is
      auto-drafted from live project data on creation ("generate project documentation
      automatically"); Executive Summary/Background/Recommendations are always author-written.
      API + workspace Reports tab + a portfolio-wide Reporting Centre page.
- [x] Export engine: PDF (QuestPDF), XLSX (ClosedXML), CSV, JSON — one `ICommitteeReportExporter`
      generating all four from the same `CommitteeReportDto`, so they can't drift from each
      other. Genuinely verified: 4 unit tests check actual output bytes (`%PDF` header, `PK` zip
      header for XLSX, CSV escaping, JSON round-trip), not just that the code compiles against
      QuestPDF/ClosedXML.
- [ ] DOCX and PPTX export — deliberately **not** attempted this phase. Six half-verified formats
      is a worse outcome than four verified ones; OpenXML SDK's WordprocessingDocument/
      PresentationDocument part hierarchy is fiddly enough (see Phase 5's Microsoft.Graph lesson)
      that it deserves its own pass with the same reflection-first verification approach, not a
      rushed addition at the end of an already-large phase.
- [x] Snapshot comparison engine: compares the fields `Snapshot` actually captures (RIBA stage,
      budget, forecast) between any two snapshots of the same project. Comparing risk/programme/
      NEC4/SBCC registers between snapshots isn't possible yet — `Snapshot` doesn't capture those
      registers, only project-header figures (see docs/roadmap.md Phase 2) — extending what a
      snapshot captures is the natural next step, not a bug in this query.

## Phase 7 — Power BI, Security Hardening, Testing, DevOps Maturity

- [x] **Critical RBAC bug found and fixed**: seeded `Security.Role.Name` values were human-readable ("Project Sponsor", "Programme Manager", ...) while every `RequireRole()`/`[Authorize(Policy=...)]` check in `Program.cs` matches on PascalCase-no-space identifiers ("ProjectSponsor", "ProgrammeManager", ...). Only the two single-word roles (`Administrator`, `Director`) ever coincidentally matched — every other role (8 of 10) was silently locked out of its own policy and the `CanWrite`/`CanApprove` composite policies since Phase 1. Undetected through six prior phases because unit tests mock `ICurrentUserService` directly and never exercise ASP.NET Core's real authorization pipeline. Fixed by adding a separate `DisplayName` field to `Role` (for UI labels) and correcting `Name` to match the `RoleName` enum convention; migration `FixRoleNamesForRbac` applied and verified against a live SQL Server container.
- [x] `EntraClaimsTransformation` (`IClaimsTransformation`): bridges a real Entra ID JWT (no app-specific claims) to the platform's internal user/role model by looking up `Security.User` by `EntraObjectId` and adding `scpm_user_id` + role claims — without this, `ICurrentUserService.UserId` always resolved null against a real token and no `RequireRole()` policy could ever match.
- [x] `SCPM.IntegrationTests`: new test project using `WebApplicationFactory<Program>` against a real SQL Server (Docker locally, service container in CI) with a minimal `TestAuthHandler` that supplies only an Entra object-identifier claim, so all real downstream code (`EntraClaimsTransformation`, `RequireRole()` policies) runs unmodified. `RbacTests` (8 tests) exercises: unauthenticated → 401, authenticated-but-unprovisioned → 403, each `CanWrite`-policy role (including the previously-broken multi-word roles) → 201, `ReadOnlyUser` denied write → 403 / allowed read → 200. This is the suite that caught the bug above.
- [x] CI: new `integration` job runs `SCPM.IntegrationTests` against a SQL Server service container on every push/PR, alongside the existing `api` (unit tests) and `migrate` (`dotnet ef database update` against a live SQL Server) jobs.
- [x] Manual security review of the document upload/archive path (the `security-review` skill could not run automatically in this repo — its `git diff origin/HEAD...` comparison fails because `origin/HEAD` points at an unrelated branch from another repository attached in this session). Found and fixed a path-traversal vulnerability: `IFormFile.FileName` from the multipart upload endpoint is fully client-controlled and was used unsanitised to build both the SharePoint Graph API path (`GraphSharePointDocumentStore.UploadAsync`) and, via the DB-persisted `DocumentFile.FileName`, the Azure Blob archive path (`ArchiveVersionCommand`). Fixed at the source — `AddDocumentFileCommandHandler` now sanitises the filename once (`FileNameSanitizer.Sanitise`, strips any `/`/`\` directory component) before it is ever persisted or sent to SharePoint, so every downstream consumer inherits the safe value. `GraphSharePointDocumentStore` also sanitises independently as defence-in-depth.
- [x] Reviewed CORS (`Cors:AllowedOrigins`-scoped, not a wildcard), export/download filename handling (`CommitteeReportsController.ExportReport` relies on ASP.NET Core's built-in `Content-Disposition` encoding, which quote/percent-escapes the filename — no header-injection risk found), and secrets handling in `appsettings.json` (only `REPLACE_WITH_...` placeholders committed, no real secrets) — no further issues found in this pass.
- [ ] Power BI datasets and row-level security aligned to RBAC — deferred; this sandbox has no Power BI Desktop/workspace to verify against, so no SQL views or RLS rules are claimed as done without live verification. Tracked as a GitHub issue.
- [ ] Full RBAC matrix across all 10 roles × every `CanWrite`/`CanApprove` endpoint — the `CanWrite`/`CanApprove` composite policies themselves are now proven correct end-to-end (see `RbacTests` above); a full per-endpoint matrix across all ~10 roles × ~80 endpoints is not attempted here as it's largely repetitive given the composite-policy proof.
- [ ] UI/E2E test suite — deferred; the frontend has no real MSAL auth wiring yet (documented TODO in `src/Web/src/lib/api-client.ts`), so a real login-through-role-check E2E flow isn't possible yet. A basic Playwright smoke test (app boots, navigates, no console errors) was considered but not completed in this pass. **Done in Phase 9** — see below.
- [ ] Load testing at scale — deferred; this sandbox has no environment resembling production scale/network topology, so no load test numbers are claimed. A local Docker-based smoke check would only prove the app doesn't crash under trivial concurrency, not that it performs at £500m-programme scale — not worth presenting as "load testing."
- [ ] Full CI/CD (security scan stage, per-environment deploy) — deferred; requires real target environments (dev/test/prod Azure subscriptions) this sandbox does not have.
- [x] Disaster recovery runbook — see `docs/disaster-recovery-runbook.md` (Phase 11).

## Phase 8 — Document Storage: SharePoint → Azure Blob Storage

- [x] **Replaced the SharePoint-backed active tier with Azure Blob Storage.** `ISharePointDocumentStore` / `GraphSharePointDocumentStore` (Microsoft.Graph, app-only auth via `ClientSecretCredential`) are gone. Reason: Graph's application-permission consent (`Sites.ReadWrite.All` or a site-scoped equivalent) has to be granted by a tenant admin, and that consent is not obtainable in this deployment — see GitHub issue #2. A storage account connection string needs no tenant admin, so it replaced SharePoint entirely rather than sitting unresolved.
- [x] New `IDocumentStore` / `AzureBlobDocumentStore` (renamed from `ISharePointDocumentStore`) is the active tier now; `IBlobArchiveStore` / `AzureBlobArchiveStore` remains the archive tier. Both live in the same storage account (`BlobStorage` config section: one `ConnectionString`, `ActiveContainerName` + `ArchiveContainerName`), which also meant the archive copy could switch from an anonymous `HttpClient` GET against a (would-be-private) SharePoint/Blob URL to an authenticated blob-to-blob copy through the SDK — a real correctness fix, not just a rename, since the old HTTP-GET approach would have 403'd against a non-public active-tier container.
- [x] `DocumentFile.SharePointUrl` renamed to `StorageUrl` end-to-end (entity, EF configuration + migration, DTOs, query handlers, frontend `types.ts`) rather than keeping a misleading name.
- [x] Removed the now-unused `Microsoft.Graph` and `Azure.Identity` package references from `SCPM.Infrastructure`.
- [x] `dotnet build`/unit tests/integration tests re-verified green after the swap; see commit history for the exact migration.
- [x] **Exercised against a real Azure Storage account** (closing GitHub issue #2). The user provided a scoped, short-lived SAS token; a throwaway verification script (not committed, deleted after use) ran the exact upload/overwrite-rejection/archive-copy logic from `AzureBlobDocumentStore`/`AzureBlobArchiveStore` against it. Confirmed against the real service: both containers create correctly, upload succeeds, a second upload to the same path is genuinely rejected with `409 Conflict` (the "never overwrite" guarantee holds, not just in theory), downloaded content matches byte-for-byte, and the archive-tier copy (parsing the source container/blob name back out of the active-tier URL, then an authenticated blob-to-blob copy) round-trips correctly. Not covered by this: the API host itself booted with a real `BlobStorage` connection string and a full HTTP request through `DocumentsController` — the SDK-level behavior is now verified, the controller→DI→SDK wiring is a smaller separate follow-up if ever wanted.

## Phase 9 — Playwright Smoke Test

- [x] `src/Web/e2e/smoke.spec.ts` + `playwright.config.ts`: builds the frontend (`npm run build`), serves it (`vite preview`), and drives it with a real Chromium instance — loads the app shell, then clicks through all four top-level nav items (Executive Dashboard, Projects, Governance, Reporting Centre), asserting each route actually becomes active and nothing throws an uncaught exception or logs a genuine (non-network) console error.
- [x] Deliberately frontend-only, no backend started: MSAL auth isn't wired into the frontend yet (`src/lib/api-client.ts` sends every request with no token), so even a live API would 401 everything here — this test verifies the app doesn't crash while every data fetch on the page is failing, which is the condition the app is actually in today, not a fabricated happy path.
- [x] Wired into CI as a new `web-e2e-smoke` job (installs Chromium via `playwright install --with-deps` — GitHub-hosted runners don't have this sandbox's pre-installed browser).
- [ ] Does not cover a real login-through-role-check flow, or any page's actual data rendering once the API returns real (as opposed to 401) responses — both need MSAL wired in first, which is still a Phase 2-labelled TODO in `api-client.ts` that was never picked up as the phases progressed elsewhere.

## Phase 10 — Real Entra ID Authentication (MSAL)

- [x] **Real Entra ID app registration wired in, not a placeholder.** The user registered a single app in Stirling's actual Entra ID tenant (self-referencing scope pattern — the same app registration serves as both the API's identity and the SPA's client), added an `access_as_user` delegated scope with an App ID URI in the tenant-policy-required `api://<app-id-guid>` form, and added a Single-page application platform with redirect URI `http://localhost:5173`. `appsettings.json`'s `EntraId` section and `src/Web/src/lib/msal-config.ts` now hold the real tenant ID and client ID — not secrets (both are visible in any login redirect URL and in the ID token itself), so committing them directly (with `VITE_MSAL_*` env-var overrides for anyone using a different tenant) is safe in a way a client secret or connection string would not be.
- [x] `@azure/msal-browser` + `@azure/msal-react` added to the frontend. `src/lib/msal-instance.ts` holds a single shared `PublicClientApplication`, used both by `MsalProvider` (`main.tsx`) and by the plain-code `api-client.ts` fetch wrapper (which isn't a React component, so it can't use MSAL's hooks). `AppShell.tsx` gained a real sign-in/sign-out control (`loginPopup`/`logoutPopup`, `AuthenticatedTemplate`/`UnauthenticatedTemplate`).
- [x] `api-client.ts`'s `getAccessToken` now calls `acquireTokenSilent` for real, attaching a genuine bearer token to every request when signed in. Deliberately does *not* pop an interactive prompt from inside a background fetch (e.g. a TanStack Query refetch) on silent-acquisition failure — it lets the request go out unauthenticated and the API's resulting 401 is what surfaces the need to re-authenticate through the normal sign-in control, not a jarring popup from nowhere.
- [x] Verified: `dotnet build` (0 warnings/errors), 11/11 unit tests, 8/8 integration tests against a live SQL Server, `npm run lint`, `npm run build`, and the Playwright smoke test (Phase 9) all still pass with real MSAL wired in.
- [x] Scope name confirmed against the real app registration's "Expose an API" blade: `api://5ee8daf1-b7bb-43f8-9d78-b9741de0657e/access_as_user`, exactly matching the default already baked into `msal-config.ts` — no code change needed.
- [x] **VERIFIED: a real interactive sign-in, end to end, in a real browser, against live Azure SQL.** A user signed in against the live Entra ID tenant, the UI showed their account, and a real access token was accepted by `Microsoft.Identity.Web`. Confirmed directly from the API's own logs rather than inferred: `IDX10242` (valid signature), `IDX10239` (valid lifetime), `IDX10234 Audience Validated. Audience: 'api://5ee8daf1-…'` matching `appsettings.json` exactly, and `IDX10245` (claims identity created). `EntraClaimsTransformation` then ran its `Security.User` lookup and the projects query executed successfully against the real database.
- [x] This supersedes a weaker earlier confirmation, and the difference is worth being precise about. The first successful sign-in happened on a network blocking outbound port 1433, so it could only be evidenced by the API returning `500` rather than `401` — enough to conclude the token had passed validation, but an inference from a failure mode. Re-running with the database reachable produced the validation log lines and a successful query: direct evidence.
- [x] Getting there took four real fixes, all now in the codebase: the API refusing to start without a database (Phase 12), sign-in errors being swallowed with no trace, MSAL's popup timing out because it loaded the whole app before handing the auth code back, and finally abandoning the popup flow for the redirect flow when the handoff kept failing even with a blank redirect page. Notably, the authentication itself was correct from the very first attempt — every failure was in the plumbing around it, and the absence of error reporting is what made that impossible to see.
- [x] A theory raised and disproved during that work, recorded so it isn't re-litigated: the sign-in resolved to the personal-Microsoft-account tenant (`utid 9188040d-…`) rather than the org tenant, which looked like it should cause the API to reject the token on issuer validation. It did not — the token was accepted. The concern was wrong.
- [ ] No role/App-role mapping was set up in Entra ID — this deliberately keeps `Security.User`/`Security.Role` in SQL Server as the single source of truth for authorization (via the already-built `EntraClaimsTransformation`), with Entra ID handling authentication only. This matches the existing architecture and needed no new work, but is worth stating explicitly since Entra ID *can* carry app roles and this setup intentionally doesn't use that path.
- [ ] The Playwright smoke test (Phase 9) still does not cover an authenticated flow — doing so would mean either scripting a real interactive login (fragile, slow, and the kind of test that breaks on Microsoft's own login-page changes) or building a token-injection test harness akin to `SCPM.IntegrationTests`' `TestAuthHandler`. Neither was attempted here.

## Phase 11 — Disaster Recovery Runbook

- [x] `docs/disaster-recovery-runbook.md`: RPO/RTO targets (flagged explicitly as proposed defaults needing real sign-off, not agreed commitments), what's actually backed up today per component (SQL automated backups, Blob GZRS/LRS redundancy, Key Vault soft delete, source-controlled IaC/migrations as the compute recovery mechanism), step-by-step recovery procedures for five concrete failure scenarios (SQL corruption, accidental blob deletion, storage account/SQL Server deletion, regional outage, Entra ID outage), and a "Known gaps" section listing exactly what this runbook cannot yet deliver on.
- [x] Written directly against `infrastructure/bicep/main.bicep`'s actual resources — every claim about redundancy/backup behavior traces to a specific Bicep property (or its absence). Where the IaC doesn't provision something a real DR posture needs (blob soft delete, Key Vault purge protection, SQL auto-failover groups, a secondary-region App Service), the runbook says so plainly rather than describing an aspirational capability as if it existed — most visibly in §3.4, which states outright that the 1-hour regional-outage RTO in §1 is not realistically achievable against the current IaC.
- [ ] Not a substitute for an actual DR drill — every procedure is documented but has never been executed end-to-end against a real deployed environment, since this sandbox has never had one to drill against. The runbook's own "Recommended next steps" section names this as the natural follow-up once a real environment exists.
- [ ] No on-call/escalation contact list — deliberately omitted rather than filled with placeholder names, since that's organisational information this project has no way to know, not a technical gap to close here.

## Phase 12 — API Startup Resilience (bug found during real local setup)

- [x] **Fixed: the API could not start at all if the database was unreachable.** `Program.cs` called `RecurringJob.AddOrUpdate` (Hangfire snapshot job registration) unguarded at startup; that writes to Hangfire's SQL storage immediately, so a database outage threw straight out of `Main` and killed the process. Found the hard way — during first real local setup against an Azure SQL free tier, where the database auto-pauses on inactivity and the developer's IP rotated (mobile hotspot) out of the SQL firewall allow-list. Both are routine conditions, not exotic ones.
- [x] Registration is now wrapped, logging a loud `LogError` and continuing to start. Verified by booting the API against a deliberately unreachable database: it starts, logs the error, serves requests, and an unauthenticated `GET /api/projects` correctly returns `401` — proving the entire Entra ID authentication path works with no database at all, since JWT bearer validation never touches SQL.
- [x] This also has a practical benefit for setup and testing: the MSAL sign-in flow can be exercised end-to-end without a working database connection, decoupling "is auth wired correctly" from "is the database reachable" — two problems that were previously tangled together and had to be solved in sequence.
- [x] **Gap closed in Phase 13.** This entry previously recorded that recurring jobs stayed unregistered until the app was restarted against a reachable database, with scheduled snapshots silently not running in the meantime. `RecurringJobRegistrationService` removes the need for that restart — see below.

## Phase 13 — Recurring Job Registration Retry

- [x] **`RecurringJobRegistrationService` (an `IHostedService`) replaces the inline try/catch registration in `Program.cs`.** Registering a recurring job is a database write, and doing it once on the startup path meant doing it at the one moment least likely to succeed. Both previous shapes were wrong in different directions: unguarded, an unreachable database killed the process outright (Phase 12); wrapped in try/catch, the API started but the jobs stayed unregistered indefinitely and nothing recovered on its own. The hosted service retries with exponential backoff (5s, doubling, capped at 5 minutes) until the write succeeds, then stops. `AddOrUpdate` is idempotent, so a late registration produces exactly the same three job definitions as a prompt one.
- [x] The cap matters more than the growth rate, and is deliberate: an outage measured in hours must not back off to a retry interval measured in hours too, or recovery is discovered long after it actually happens.
- [x] `await Task.Yield()` before the first attempt. `BackgroundService` runs `ExecuteAsync` synchronously up to its first `await`, on the host's startup path — without the yield, a first attempt against an unreachable database would block the API from serving requests for the length of the SQL connection timeout, which is the exact delay this class exists to avoid.
- [x] Logging is deliberately asymmetric: the first failure carries the full exception, subsequent ones log message-only at `Warning` along with the next retry interval. A database down for an hour would otherwise fill the log with identical stack traces and bury the one that explains what happened.
- [x] Depends on `IRecurringJobManager` rather than Hangfire's static `RecurringJob` facade, which is what makes it testable — the old inline code could not be covered at all. 3 unit tests (`RecurringJobRegistrationServiceTests`): all three jobs register when storage is reachable (asserting on job *ids*, so a copy-paste slip registering the same id three times fails rather than passing on call count); registration succeeds on retry after an initial failure, exercising a real 5-second backoff rather than mocked time; and `StopAsync` returns promptly mid-backoff instead of hanging shutdown until the host's timeout.
- [x] Verified: `dotnet build` clean (0 warnings, 0 errors), 14/14 unit tests pass (11 existing + 3 new).
- [ ] Not covered: the retry path against a genuinely unreachable *SQL Server*, as opposed to a substituted `IRecurringJobManager` that throws. The integration suite has a reachable database by construction, and taking it away mid-run would be testing Hangfire's own connection handling more than this class's behaviour.

Each phase is designed to be independently shippable and reviewable. Modules from Phase 2 onward follow the same Domain → Application → Infrastructure → Api → Web pattern established in Phase 1.
