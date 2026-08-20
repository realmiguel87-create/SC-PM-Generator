# Disaster Recovery Runbook

This runbook covers backup, restore, and failover procedures for the Stirling Council Capital
Projects Management Platform. It is written against the infrastructure actually defined in
`infrastructure/bicep/main.bicep` — where that Bicep doesn't yet provision something a real DR
posture would need (geo-redundant SQL failover, a secondary-region App Service, automated
restore drills), this document says so explicitly rather than describing a capability that
doesn't exist. See the "Gaps against this runbook" section at the end for the full list.

## 1. Scope and objectives

| Component | RPO (max acceptable data loss) | RTO (max acceptable downtime) |
|---|---|---|
| Azure SQL Database (`SCPM`) | 5 minutes (point-in-time restore granularity) | 1 hour (single-region restore) |
| Azure Blob Storage (active + archive document tiers) | Near-zero (GZRS in prod replicates synchronously within the primary region, asynchronously to the paired region) | 1 hour |
| API (App Service) | N/A (stateless) | 15 minutes (redeploy from source/IaC) |
| Web (static SPA) | N/A (stateless) | 15 minutes (redeploy) |

These targets are **proposed defaults**, not values agreed with Stirling Council's actual
business continuity requirements — they need sign-off from whoever owns the council's IT
disaster recovery policy before being treated as commitments. A £500m capital programme's
governance/audit data may well warrant a tighter RPO than 5 minutes; this table is a starting
point for that conversation, not the conversation itself.

## 2. What's actually backed up today, and how

### 2.1 Azure SQL Database

- **Automated backups**: Azure SQL Database takes these automatically — full weekly, differential
  every 12–24 hours, transaction log every 5–10 minutes — with **no configuration required** and
  none present in `main.bicep`. Default backup storage redundancy for a database created without
  an explicit `requestedBackupStorageRedundancy` property is **geo-redundant (RA-GRS)** as of
  current Azure defaults, but this is Azure's default behaviour, not something this repository's
  IaC asserts — **verify this setting in the Azure Portal** (Database → Backups → Configure)
  rather than assuming it, since Microsoft has changed this default before and region/subscription
  policy can override it.
- **Retention**: 7 days point-in-time restore (PITR) by default on the `GP_Gen5` tier provisioned
  here. Long-term retention (LTR — monthly/yearly backups kept for years) is **not configured** in
  `main.bicep` and would need an explicit LTR policy if the council's records-retention
  requirements for capital programme governance data exceed 7 days (they almost certainly do,
  given NEC4/SBCC contract administration and committee reporting are core to this platform).
- **System-versioned temporal tables**: `Projects.Project`, `Cost.CostPlan`, `Cost.Forecast`,
  `Programme.Programme`, `Programme.Milestone`, `Risk.Risk`, `Risk.Issue`,
  `Stakeholder.Stakeholder`, `Documents.Document`, `NEC4.*`, `SBCC.*`, `Governance.Approval`,
  `Governance.Gateway` all carry full change history in their `_History` tables (see
  `docs/architecture.md` §6). This is an application-level audit trail, not a backup — a dropped
  database takes its history tables with it. Don't confuse "we can query `FOR SYSTEM_TIME AS OF`"
  with "we have a restore point independent of the live database."

### 2.2 Azure Blob Storage (document active + archive tiers)

- **Redundancy**: `Standard_GZRS` in prod (geo-zone-redundant — synchronous replication across
  availability zones in the primary region, asynchronous replication to the paired region),
  `Standard_LRS` elsewhere (locally redundant only — dev/test environments are not expected to
  survive a regional outage, and that's an acceptable, deliberate trade-off for non-prod).
- **Soft delete / versioning**: **not currently enabled** in `main.bicep`. This is a real gap —
  without blob soft delete or versioning turned on, an application bug or operator error that
  deletes a `DocumentFile`'s blob (not just its SQL row) is unrecoverable. Recommended before
  go-live: enable blob soft delete (7–14 day retention) and container soft delete on the storage
  account.
- **"Never overwrite" application guarantee**: `AzureBlobDocumentStore`/`AzureBlobArchiveStore`
  both refuse to overwrite an existing blob path (verified against a real Azure account — see
  `docs/roadmap.md` Phase 8). This protects against accidental overwrite, not accidental
  deletion — soft delete above is what covers the deletion case.

### 2.3 Key Vault

- **Soft delete**: enabled, 90-day retention (`main.bicep`). A deleted secret/key/certificate is
  recoverable for 90 days via `az keyvault secret recover` (or the Portal's "Deleted objects"
  view) unless purge protection is also enabled — **purge protection is not currently set** in
  `main.bicep`, meaning a sufficiently-privileged actor could permanently purge a soft-deleted
  secret before the 90 days elapse. Enabling `enablePurgeProtection: true` is a one-line, one-way
  change (it cannot be disabled once turned on) worth making deliberately, not by accident.

### 2.4 Application code and infrastructure

- **Source control**: the entire application (API, frontend, EF Core migrations, Bicep IaC) is in
  this Git repository. A total loss of the deployed Azure environment is recoverable by
  redeploying from the last known-good commit — this is the actual DR mechanism for "compute"
  (App Service), since App Service itself holds no durable state.
- **EF Core migrations**: `src/Api/SCPM.Infrastructure/Migrations/` is the schema's single source
  of truth (see `database/schema/README.md`). A fresh database can be brought to the current
  schema via `dotnet ef database update`, but this recreates an **empty** schema — it is not a
  substitute for restoring actual data from a SQL backup.

## 3. Recovery procedures by scenario

### 3.1 Accidental data corruption/deletion in Azure SQL (application bug, bad migration, operator error)

1. Identify the last known-good point in time (check `Audit.ActivityLog`/`Audit.FieldAudit` and
   the temporal `_History` tables first — the corruption may be recoverable via `FOR SYSTEM_TIME`
   queries without a full restore, which is faster and avoids losing unrelated writes made after
   the bad event).
2. If a full restore is needed: Azure Portal → SQL Database → **Restore** → point-in-time restore
   to a **new** database (Azure SQL never restores in place — this is by design, so the corrupted
   database remains available for forensic comparison).
3. Validate the restored database against expected row counts / spot-check key tables.
4. Cut the API over to the restored database (`ConnectionStrings:SqlServer` in App Service
   configuration, or the Key Vault-referenced secret if externalized) — this requires an API
   restart, which is the dominant contributor to RTO for this scenario.
5. Rename/archive the corrupted database rather than deleting it immediately, pending incident
   review.

### 3.2 Accidental blob deletion (a `DocumentFile`'s content, not the SQL row)

1. **If blob soft delete is enabled** (see §2.2 gap — enable this before relying on this
   procedure): Azure Portal → Storage Account → Containers → the affected container → toggle
   "Show deleted blobs" → undelete.
2. **If not enabled**: the blob is gone. The only recovery path is checking whether a copy exists
   in the archive-tier container (if the file had already been through `ArchiveVersionCommand`)
   or asking the original uploader to re-upload. This is exactly the scenario blob soft delete
   exists to prevent — treat a recurrence of this scenario as the trigger to finally enable it.
3. The SQL row (`DocumentFile.StorageUrl`/`BlobArchiveUrl`) will still point at a now-dead blob
   URL until manually cleared or the file is re-uploaded — a dangling reference isn't
   automatically detected by the application today.

### 3.3 Storage account or SQL Server accidentally deleted

1. Both resources have Azure resource-level soft delete behavior in some configurations, but
   **do not rely on this** — verify recoverability immediately in the Portal ("Recover" option on
   the resource type) rather than assuming a grace period exists at all.
2. If genuinely gone: redeploy the resource via `main.bicep` (`az deployment group create`),
   restore SQL from the most recent backup per §3.1, and for Blob Storage, restore from
   whatever external backup exists — **there is currently no cross-account blob backup/replication
   beyond GZRS's same-account geo-replication**, meaning if the storage account itself (not just
   its region) is destroyed, GZRS does not help; only an explicit backup to a separate account or
   Azure Backup for Blobs would. This is not currently configured and is a genuine gap.

### 3.4 Regional Azure outage (the primary region is down)

This is the scenario `main.bicep` is **least prepared for today**:

- Azure SQL Database as provisioned here (`GP_Gen5`, `zoneRedundant` only in prod) has **no
  configured geo-replication or auto-failover group**. A primary-region outage means the database
  is unavailable until either the region recovers or someone manually restores the latest
  geo-redundant backup into a database in a different region (RPO in that case is "up to the last
  backup replicated to the paired region," which can lag by hours, not the 5-minute target in §1).
- Blob Storage (GZRS in prod) has data replicated to the paired region, but **read access to
  that replica during an outage requires manually initiating a storage account failover**
  (`az storage account failover`) — it is not automatic, and Microsoft's guidance is that this is
  a last-resort action (it can result in some data loss for the most recently written objects,
  and the account is read-only in the paired region until the failover is initiated).
- App Service has no secondary-region deployment configured. Recovery means redeploying
  `main.bicep` + the application to a different region and pointing DNS at it.

**Bottom line**: a genuine regional outage today means significant, likely multi-hour downtime
and requires manual, expert-driven recovery — the 1-hour RTO in §1 is not realistically
achievable against this scenario with the current IaC. Closing this gap (SQL auto-failover
groups, a secondary App Service region behind Traffic Manager/Front Door) is real infrastructure
work, not a documentation exercise, and is listed explicitly in §4.

### 3.5 Entra ID / authentication outage

This is a Microsoft 365 service outage, outside this platform's control. There is no workaround —
the application depends on Entra ID for all authentication (see `docs/roadmap.md` Phase 10) and
has no local-credential fallback by design (a deliberate security choice, not an oversight). Track
via the [Microsoft 365 Service Health Dashboard](https://admin.microsoft.com/Adminportal/Home#/servicehealth)
and communicate downtime to users; there is nothing to "recover" on this platform's side once
Entra ID itself recovers.

## 4. Known gaps (not yet closed — tracked here, not hidden)

- [ ] Blob soft delete / container soft delete not enabled on the storage account.
- [ ] Key Vault purge protection not enabled.
- [ ] No SQL long-term retention (LTR) policy beyond the default 7-day PITR window.
- [ ] No SQL auto-failover group / geo-replication for regional outage recovery.
- [ ] No secondary-region App Service deployment or traffic-routing (Front Door/Traffic Manager).
- [ ] No automated DR drill — every procedure above is documented but has not been executed
      end-to-end against a real environment (this sandbox never had a deployed Azure App Service
      to drill against; the SQL/Blob verification done in Phases 5–8 exercised the application
      code against live services, not a full DR restore-and-cutover rehearsal).
- [ ] No on-call/escalation contact list — this runbook has no "who do you actually call" section
      because that's organisational information this project doesn't have, not a technical gap.
      Whoever owns this platform in production needs to add one.

## 5. Recommended next steps, in priority order

1. Enable Blob soft delete and Key Vault purge protection — both are single Bicep property
   changes, no architectural work, and close the two most likely "oops" scenarios (§3.2, and
   accidental permanent secret loss).
2. Decide an actual RPO/RTO with whoever owns Stirling Council's business continuity policy, and
   update §1 to reflect agreed targets rather than proposed defaults.
3. If the agreed RTO for a regional outage is tighter than "multi-hour, manual" — budget for SQL
   auto-failover groups and a secondary App Service region. This is a meaningful cost and
   complexity increase, so it should be a deliberate decision, not a default.
4. Run an actual DR drill once a real environment exists: deploy via `main.bicep`, load
   representative data, then execute §3.1 (SQL point-in-time restore) end-to-end and time it
   against the RTO target. Repeat periodically (quarterly is a common cadence for systems of this
   criticality) — a runbook that has never been executed is a hypothesis, not a plan.
