# Delivery Roadmap

This platform's full specification spans 25 deliverables across governance, contract administration (NEC4/SBCC), reporting, document management, audit, Power BI, security, testing, and DevOps. Building all of it as production-grade code in a single pass is not realistic — it is a multi-month, multi-workstream programme. This roadmap sequences delivery so every phase lands a **working, testable increment** rather than a paper design.

## Phase 1 — Foundation (this phase)

Goal: prove the full-stack pattern end-to-end so every later module is a repeat of a known-good shape, not a new architecture.

- [x] Enterprise solution architecture (`docs/architecture.md`)
- [x] Entity-relationship model for the core + Phase 1 schemas (`docs/erd.md`)
- [x] Database schemas: `Security`, `Projects`, `Governance`, `Audit` — temporal tables, soft delete, constraints
- [x] Core stored procedures + views for the Projects vertical
- [x] .NET solution: Domain / Application / Infrastructure / Api, CQRS via MediatR, FluentValidation
- [x] Entra ID authentication + RBAC middleware
- [x] Audit interceptor (field-level + activity log) wired into EF Core `SaveChanges`
- [x] Projects vertical slice: create/list/get project, RIBA stage progression, stage gate approval — API + React
- [x] React shell: routing, TanStack Query client, ShadCN theme using the Stirling palette, executive dashboard skeleton, project workspace tab shell
- [x] CI pipeline (build, test, lint) via GitHub Actions
- [x] Azure IaC skeleton (Bicep) for App Service, SQL, Key Vault, Blob, App Insights

## Phase 2 — Governance, Cost & Programme

- Governance module: mandates, business cases, decision register, approval gates
- Cost module: cost plans, budgets, forecasts, temporal cost history
- Programme module: milestones, Gantt-style programme view, delay analysis
- Template generator (governance + cost + programme templates) via QuestPDF/OpenXML/ClosedXML
- Snapshot engine v1 (scheduled + manual snapshots, Hangfire jobs)

## Phase 3 — Risk, Issues, Opportunities & Stakeholders

- Risk/issue/opportunity registers with heatmaps (Recharts)
- Stakeholder register, engagement tracker, communications plan
- Escalation workflow tied into approvals

## Phase 4 — NEC4 & SBCC Contract Administration

- NEC4: Early Warning, Compensation Event, Contract Data, Risk Allocation Matrix, Accepted Programme, Payment Assessment, Change registers + export packs
- SBCC: Variation, EOT, Loss & Expense, Architect's Instructions, Interim Valuation registers + export packs

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
