# Sales Adjustment Production Readiness 001

## Decision

**CONDITIONAL ACCEPT.** Current production runtime, schema, and business facts are compatible with application release candidate `f92899b6725d099c9a722f52d1e1b1e552ee41b9`. No code, data, schema, or transaction ambiguity was found.

The remaining conditions are operational only: a separate explicit rollout authorization, fresh verified production backup, immutable artifact verification, rollback preparation, deployment, and post-deploy smoke/reconciliation. None is authorized or performed by this audit.

## Identity And Runtime

- `SELECT DB_NAME()` returned exactly `VPureLux` before business-data reads.
- SQL instance: `VPureLux`; SQL Server `16.0.4255.1`, Express Edition.
- Active symlink and release: `/opt/vpurelux/app` -> `/opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197`.
- Runtime source identity: `b0bf197e8525acb2f254995af70b3da8a397a9f8`, reconciled through the immutable release path and accepted Service V1 rollout record.
- `vpurelux-web` is active with zero restarts since `2026-09-07 15:17:40 +07`; working directory is `/opt/vpurelux/app` and environment is `Production`.
- Effective runtime catalog is `VPureLux`; CustomerCare and Sales intake are enabled; Service is enabled.
- Health endpoint returned `Healthy`; retained rollback `/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa` exists.
- Capacity snapshot: root disk 59 GiB total/42 GiB available; RAM 9.5 GiB total/6.8 GiB available including cache.

## Migration And Schema

Production has 24 EF migrations, with no duplicate IDs. Latest migration is `20260907021302_AddServicePaymentSettlement`; the four accepted Service V1 migrations and `20260826051356_AddSalesPreInstallationV1Foundation` are present exactly once.

The bounded schema check found all Sales revision/order/payment/refund, Inventory transaction/FIFO/lot/balance, and CustomerCare asset/position objects and required columns/indexes. `AppInventoryBalances` correctly uses its configured composite key rather than an `Id` column. No unexplained migration, missing adjustment schema, or candidate schema dependency exists. Candidate deployment is Web/application-only.

## Sales Revision State

- Sales orders: 50 total; 0 Draft, 42 Confirmed, 8 Cancelled.
- Sales revisions: 5 total; 1 Draft, 4 Applied, 0 Cancelled.
- Applied revisions missing `AppliedAt`: 0.
- Non-Applied revisions with `AppliedAt`: 0.
- Missing order/revision-line parents or duplicate active revisions: 0.
- Applied/effective-line mismatches, removed lines still effective, or Inventory-rule mismatches: 0.

All 25 lines across the four Applied revisions are unchanged lines; none has or requires revision Inventory movement. Their effective Sales values match the applied revision facts and all Applied revisions have business-audit evidence.

The single Draft is revision `B2AE43B7-45A0-22DD-398F-3A238BE3C2CF` for `SO-202609-000006`. Its 9 lines remain unchanged from their before snapshots, no Warehouse return is pending, no effective/applied/Inventory fact exists, and the effective order remains unchanged. Candidate `SubmitRevisionAsync` reloads current posted values under the coordinator, so this internally consistent Draft can coexist safely with the new UI and cannot silently override a future submitted form.

## Inventory, CustomerCare, And Money

There are currently zero Inventory transactions owned by `SalesOrderRevisionLine`. Consequently there is no premature reversal or positive-delta posting to reconcile. Targeted checks found zero duplicate revision transaction references, missing transaction references, orphan transaction lines/allocations/lots, or non-positive revision quantities.

CustomerCare checks found zero installed assets predating a revision, duplicate sold-machine source-line/unit keys, non-terminal sold assets referencing missing/non-effective Sales lines, orphan asset components, or pending-installation assets with an installation timestamp. No changed machine revision line exists in the current production revision set.

Revision orders have 4 posted payments, 0 voided payments, and 0 revision-linked refunds. No orphan refund exists and the candidate does not modify Sales payment/refund or GrossPosted/NetPaid behavior. There is no payment/refund blocker for this release.

## Logs

The application/journal window starts at the current service activation time. Twenty-seven hourly application log files, the matching systemd journal, and retained Nginx adjustment entries contain:

- Sales adjustment/revision errors: 0.
- Confirm ModelState warning matching the old defect: 0.
- relevant Inventory/FIFO or CustomerCare revision errors: 0.
- application HTTP 500 entries: 0.
- journal priority-error entries: 0.
- Nginx Sales Adjust requests inspected: 22; HTTP 500 responses: 0.

No recurring startup/configuration error affecting rollout readiness was found.

## Safety And Next Action

Production mutations performed: **NONE**. No business request, fixture, SQL write, temporary table, migration, DbMigrator, seed, backup, configuration edit, service restart, symlink change, publish, upload, deployment, tag, or push was performed. Application source was not changed.

Next task: `SALES-ADJUSTMENT-PRODUCTION-ROLLOUT`, currently HOLD until the user gives separate explicit deployment authorization. That task must create and verify a fresh production backup, build and verify an immutable artifact from the exact candidate, preserve the current release as rollback, deploy atomically, and run bounded health/login/static-assets/Sales-adjustment smoke and reconciliation.
