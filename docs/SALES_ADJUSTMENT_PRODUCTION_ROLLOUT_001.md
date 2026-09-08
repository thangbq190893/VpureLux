# Sales Adjustment Production Rollout 001

## Decision

**PRODUCTION ROLLOUT COMPLETE.** The accepted Sales confirmed-order adjustment candidate `f92899b6725d099c9a722f52d1e1b1e552ee41b9` was deployed application-only and is production RELEASED / ACCEPTED.

## Release Identity

- Previous source/release: `b0bf197e8525acb2f254995af70b3da8a397a9f8` / `/opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197`.
- New active source/release: `f92899b6725d099c9a722f52d1e1b1e552ee41b9` / `/opt/vpurelux/releases/web-20260908-181200-sales-adjustment-f92899b`.
- Deployment switch: atomic symlink replacement at `2026-09-08T18:12:17+07:00`, followed by restart of `vpurelux-web` only.
- Immediate rollback path retained: `/opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197`.
- Earlier Sales V1 rollback baseline also remains available: `/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa`.

## Backup And Artifact

- Fresh production backup: `/var/opt/mssql/data/VPureLux-pre-sales-adjustment-20260908-180614.bak`.
- Backup mode/result: `COPY_ONLY, CHECKSUM`; `RESTORE VERIFYONLY WITH CHECKSUM` passed.
- Backup size/time: 43,547,136 bytes at `2026-09-08 18:06:17 +07`.
- Artifact was published from a clean detached worktree at the exact candidate. Web only was published; DbMigrator was not built or run.
- Archive: `web-sales-adjustment-f92899b-20260908-1806.tar.gz`, 96,006,209 bytes, 5,910 entries.
- Local SHA-256: `c77452d51646dd37fd1a95ce87ab8d2153fa9685497c2d61a82126837ba55249`.
- VPS SHA-256: `c77452d51646dd37fd1a95ce87ab8d2153fa9685497c2d61a82126837ba55249`.
- Production `appsettings*.json` and `openiddict.pfx` were preserved byte-for-byte from the previous active release. Development/UAT settings, logs, and local artifacts were excluded.

## Runtime And Smoke

- Runtime environment: `Production`; effective database catalog: `VPureLux`.
- Service: active; process working directory resolves to the new immutable release; restart count is zero.
- Health: three sequential probes returned `Healthy`.
- HTTP smoke: root, login, Sales list/details/adjustment, Returns, Refunds, Service, Warranty, ABP assets, adjustment JavaScript, and Font Awesome assets passed 16/16 authenticated/non-mutating GET checks.
- Adjustment UX served by production contains the single `Xác nhận điều chỉnh` action and no old Apply/Save action.
- No business mutation POST was submitted during smoke.

## Reconciliation

- Database identity before/after: `VPureLux` / `VPureLux`.
- EF migrations before/after: 24 / 24; latest remained `20260907021302_AddServicePaymentSettlement`.
- Sales orders remained 50 total: 0 Draft, 42 Confirmed, 8 Cancelled.
- Revisions remained 1 Draft and 4 Applied. Draft revision `B2AE43B7-45A0-22DD-398F-3A238BE3C2CF` for `SO-202609-000006` remained unchanged, with 9 unchanged lines and no Applied or Inventory facts.
- Applied/effective-line, revision timestamp/orphan, Inventory/FIFO, lot/balance, and CustomerCare targeted anomaly counts remained zero.
- Revision-owned Inventory transactions remained zero. Payment facts on revision orders remained 4 Posted, 0 Voided, and 0 revision-linked refunds.
- No concurrent legitimate business-data change was observed during the bounded deployment window, and the rollout caused no business mutation.

## Logs And Release Seal

- Post-deploy application error lines: 0; ModelState warning lines: 0; journal priority errors: 0; Nginx HTTP 500 responses: 0.
- Two pre-existing non-blocking EF startup mapping warnings were observed for `SalesOrderRevisionLine` and `SalesOrderRevisionAllocation`; neither is a deployment error.
- Annotated tag: `release-2026-09-08-sales-adjustment`.
- Tag object: `5e2f6aea469b4b6be9e47c9a668131f43ddc1443`.
- Peeled target: `f92899b6725d099c9a722f52d1e1b1e552ee41b9`.
- Branch and documentation commit were pushed normally without force after this record was committed; remote verification is recorded in the final rollout report.

## Safety Boundary

No migration, DbMigrator, seed, fixture, INSERT/UPDATE/DELETE/MERGE business DML, backfill, repair, FIFO rebuild, production test order/revision, or mutating browser/API smoke was performed. GrossPosted/NetPaid and unrelated application source were not changed. Rollback was not required.
