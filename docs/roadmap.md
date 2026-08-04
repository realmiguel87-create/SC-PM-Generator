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

- Documents/Versions/Files model, versioning state machine (Draft → Review → Approved → Superseded/Archived/Rejected)
- SharePoint Online integration (Graph API), Azure Blob archive tier

## Phase 6 — Committee, Stakeholder & Executive Reporting Centre

- Committee/Cabinet/Board report generator with standard section structure
- Multi-format export engine (PDF/DOCX/XLSX/PPTX/CSV/JSON) with consistent branding
- Snapshot comparison engine + comparison reports

## Phase 7 — Power BI, Security Hardening, Testing, DevOps Maturity

- Power BI datasets and row-level security aligned to RBAC
- Full RBAC matrix across all 10 roles
- Unit/integration/API/UI/load/security test suites
- Full CI/CD (build → test → security scan → deploy per environment), disaster recovery runbook

Each phase is designed to be independently shippable and reviewable. Modules from Phase 2 onward follow the same Domain → Application → Infrastructure → Api → Web pattern established in Phase 1.
