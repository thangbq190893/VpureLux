# Service V1 Production Rollout - 2026-09-07

## Decision

Service V1 was released to production successfully. The active application source is `b0bf197e8525acb2f254995af70b3da8a397a9f8`, sealed by annotated tag `release-2026-09-07-service-v1`.

## Deployment State

- Active release: `/opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197`
- Immediate rollback release retained: `/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa`
- Production database proof: SQL Server `VPureLux`, catalog `VPureLux`
- EF history: `20 -> 24`; the four expected Service migrations are recorded exactly once, and historical `20260824113235_AddServiceModule` remains recorded once.
- Runtime Service flag: `false -> true`; all unrelated systemd environment values were preserved.
- Production certificate and app configuration were copied byte-for-byte from the previous active release. The effective signing key id remained `9B3FA93C18BEED439B2CCE30984E918CECFC0CBE`.

## Safety Evidence

- Fresh backups preserved:
  - `/var/opt/mssql/data/VPureLux-pre-service-v1-20260907-152000.bak`
  - `/var/opt/mssql/data/VPureLux-pre-service-v1-20260907-151324.bak`
- Both used `COPY_ONLY`, `CHECKSUM`, and passed `RESTORE VERIFYONLY WITH CHECKSUM`.
- Production-derived rehearsal `VPureLux_SERVICE_REHEARSAL_20260907_152000` applied the same normal EF command path and passed schema/history reconciliation. It was dropped only after final evidence was captured.
- Migration reconciliation passed `41/41` pre-existing business tables. No seed, backfill, FIFO rebuild, payment/refund rewrite, reminder rewrite, or manual history repair was performed.
- Production activity continued during the rollout. Six audited Sales POSTs at 15:41-15:43 changed existing Sales/BOM/Inventory rows after the migration proof. These requests were outside rollout tooling. A new baseline taken after that activity then passed `42/42` tables across final read-only smoke, proving rollout probes did not mutate production facts.

## Artifact And Runtime Verification

| Artifact | SHA-256 |
| --- | --- |
| Web | `3810C6D6B97E5872408850B33D3B8103DE91B9418BE316462AEA8BA05C9BBB02` |
| DbMigrator | `F7A5A41E4767A837295EC2D7035A2C8FA2F134899D23A3116D914735CC11694E` |

The published source included all seven Service/report scripts and four local FontAwesome fonts. Archive SHA verification and extraction succeeded. Appsettings, secrets, local PFX, logs, and UAT artifacts were excluded from upload; production equivalents were retained in the release.

Three sequential enabled-mode health probes passed. Service Orders, Works, Service Revenue, Consolidated Revenue, Sales list/details/adjust route, Sales Revenue/Profit, Warranty assets/policies/pending installations, and Inventory balances/ledger/lots all loaded by GET successfully. Production currently has no historical Service orders, payments, or refunds, so their detail/history views had no existing rows to inspect.

For 2026, Service Revenue returned zero completed Service documents and Consolidated Revenue returned 41 Sales documents with `213,125,500` revenue; the consolidated result equals Sales plus Service. No `COUNT_BIG(NULL)` failure recurred.

Startup emitted pre-existing ABP module registration warnings. A single `SALES_008` log entry at 15:45 came from an initial smoke probe that omitted the mandatory cancellation modal `Id`; the valid route with `Id` returned HTTP 200. It did not mutate data and is not a Service/runtime failure.

## Capacity And Cleanup

- Initial free disk: `15,186,628,608` bytes.
- Deleted 167 obsolete release directories, retaining runtime logs separately; deleted two obsolete Sales archive uploads, three completed VPL/UAT backups, and four completed VPL rehearsal databases.
- Free disk before candidate deployment: `44,570,972,160` bytes.
- Deleted temporary Service Web and DbMigrator uploads after SHA validation/extraction and healthy rollout.
- Final free disk: `44,396,654,592` bytes; inode availability remains 96%.

Production `VPureLux`, test `VPL`, both new production backups, the active Service release, and the Sales V1 rollback release were retained.

## Scope Guard

No fake production UAT records or business data repair were used. The known Sales adjustment and Sales `GrossPosted`/`NetPaid` naming defects remain separate, untouched work items.

Sanitized operational evidence is under `docs/evidence/service-v1-rollout/`; raw production evidence remains local and ignored under `artifacts/service-v1-rollout/`.
