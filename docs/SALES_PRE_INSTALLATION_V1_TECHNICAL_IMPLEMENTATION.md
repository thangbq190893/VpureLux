# Sales Pre-Installation V1 - Technical Implementation

Status: Phase 2 implementation, migration rehearsal, and production rollout completed on 2026-09-03 at deployed commit `a4717aa`.

## 1. Current Architecture Audit

- `SalesOrder` is the posted Sales aggregate. Its owned `SalesOrderLine` rows contain confirmation snapshots and the original `SalesIssue` reference.
- Confirm posts one idempotent FIFO `SalesIssue` per Sales line and snapshots revenue, cost and BOM facts.
- Payment history is append-oriented in `SalesOrderPayment`; receivable values are derived from posted rows.
- Confirmed unpaid cancellation currently reverses inventory immediately. Phase 1 replaces that confirmed-order path with an effective cancellation record and separate stock/refund obligations. Draft cancellation stays intact.
- Sold machines are represented by `CustomerAsset`; the first successful installation is the authoritative modification lock.
- Sales reports use confirmed header snapshots and effective line detail. Cancelled orders are already excluded by status.

## 2. Persistence Model

- `SalesOrderRevision` is a companion aggregate with `Draft`, `Applied`, and `Cancelled` states, a per-order revision number, reason, actor/time, idempotency key, before/after totals, refund due, and owned revision lines.
- Each revision line contains the source effective line reference, requested product/BOM/quantity/price, before snapshots, return confirmation where a decrease requires it, and the resulting issue/reversal transaction references.
- Posted Sales lines are retained. Applying a revision updates the effective projection under aggregate control, marks removed lines ineffective, and retains the before/after facts in the revision aggregate. Unchanged lines remain untouched.
- `SalesOrderCancellation` records the effective cancellation plus independent stock and payment obligations. `SalesOrderRefund` records real refunds without deleting or rewriting received payments.

## 3. Revision Delta Algorithm

1. Acquire the shared distributed lock `sales-order:{SalesOrderId}` and reload current state in the Unit of Work.
2. Verify Confirmed status, no Installed asset, no active revision/cancellation, permission, and immutable CustomerId. Machine configuration is not a Sales eligibility condition.
3. Compare effective lines by source line id. Classify unchanged, price-only, quantity increase/decrease, replacement, add, and remove.
4. Leave unchanged lines and their original allocation/cost untouched.
5. Post only required FIFO issues. Reverse decreases/removals/replacements from the effective allocation ledger using original lot and unit-cost facts.
6. Update only changed effective snapshots, mark removed source rows ineffective, and recompute order totals once.
7. Mark the revision Applied with related transaction ids. A unique apply key and transaction boundary make retries idempotent.

Any prerequisite or stock failure rolls back all lot, balance, transaction, revision, and order changes.

## 4. Payment Carry-Forward

Payments remain attached to `SalesOrderId`. Applying a revision derives `NetPaid` from posted payment rows. Remaining is `max(NewTotal - NetPaid, 0)` and `RefundDue` is `max(NetPaid - NewTotal, 0)`. A refund is a separate append-only row; it does not void a real payment.

## 5. Cancellation

Manager approval immediately changes Sales status to Cancelled and creates one cancellation process. Stock return and refund are then handled independently. Returned stock is restored only after Warehouse confirms it is eligible, using original lots/costs; an exception remains pending for explicit resolution. The process closes only when both obligations are complete or not required.

## 6. Installation Lock And Concurrency

Open/update/apply/cancel/install use the same distributed lock name based on `SalesOrderId`, reload state after acquiring it, and rely on rowversion plus unique indexes as database backstops. Installed is not a Sales status and does not create Sales Completed. Explicit HTTP endpoints are exposed by `SalesPostConfirmationController`; Razor UI is deferred.

All Confirmed orders share the same controlled correction rules. `IsMachine` only controls CustomerCare intake and pending-asset reconciliation. An order without assets has no installation lock; V1 intentionally defers a Delivery/Accounting-close terminal boundary for non-machine orders. Negative inventory deltas still require Warehouse confirmation.

## 7. Reporting Impact

Cancelled orders remain excluded immediately. Effective-line filtering prevents superseded snapshots from duplicating revenue/profit detail. Existing report formulas and recognition semantics are unchanged.

## 8. Migration

The migration is schema-only: new companion tables, owned revision-line storage, effective-line metadata, concurrency columns, foreign keys, filtered unique indexes, and report procedure filtering where required. It contains no business-data `UPDATE` or `DELETE` statement.

## 9. Tests

Focused Domain/Application/EF tests cover state invariants, delta behavior, exact lot/cost reversal, atomic rollback, payment/refund calculations, cancellation obligations, installation lock, permissions, idempotency, and existing Sales/Inventory/CustomerCare regressions. Final 2026-09-03 evidence: Application `2/2`, Domain `35/35`, EF `95/95`, focused Web regressions `15/15`, full solution build `0` errors, and no pending EF model changes. The combined Web filter did not complete because the known testhost memory leak grew to about 3.6 GB; it was stopped and is not reported as a full pass.

## 10. Intentionally Deferred

- `CancelAndClone` convenience action.
- Warehouse quarantine/inspection subsystem and accounting-ledger redesign.
- A separate Delivery/Accounting-close terminal boundary for non-machine orders.
- Any new Reservation, Fulfillment, Delivery, Completed, workflow-engine, or event-sourcing model.

## 11. Known Risks

- Non-machine Confirmed orders have no terminal modification boundary in V1; permission, audit, warehouse confirmation, and atomic posting are the deliberate controls until a separately approved delivery/accounting-close boundary exists.
- Existing SQL Server report procedures require the same effective-line predicate as the EF/SQLite fallback.
- Multi-instance safety depends on both the configured distributed lock provider and database uniqueness/concurrency constraints.
- The combined Web test filter has a known testhost memory leak. Small focused Web groups complete successfully, but the combined run must not be represented as passed.

## 12. Migration Rehearsal

- Rehearsal database: `VPL_SALES_REHEARSAL_20260903`, restored from `/var/opt/mssql/data/VPL-pre-service-uat-20260825-170432.bak`. Production database `VPureLux` was not accessed.
- Baseline before rehearsal ended at `20260824113235_AddServiceModule` and did not contain Sales V1. The extra Service migration is present in the restored backup but is not part of the current branch migration chain.
- Applied exactly `20260826051356_AddSalesPreInstallationV1Foundation` with `dotnet ef database update`; no later migration was required.
- The migration `Up()` contains schema DDL and stored-procedure alteration only. It contains no business-table `UPDATE`, `DELETE`, `MERGE`, backfill, historical rebuild, or automatic Revision/Cancellation/Refund/Asset creation.
- The two report procedures deliberately started as `CREATE   PROCEDURE` with irregular whitespace. Migration execution succeeded and both definitions gained `l.IsEffective = 1`.

## 13. Legacy Compatibility Evidence

Synthetic legacy fixtures were created through the pre-Sales-V1 application at release commit `55aaf24`, then left untouched during migration and reconciliation. Counts before and after were identical:

| Table | Before | After |
|---|---:|---:|
| `AppSalesOrders` | 2 | 2 |
| `AppSalesOrderLines` | 3 | 3 |
| `AppSalesOrderPayments` | 1 | 1 |
| `AppInventoryTransactions` | 3 | 3 |
| `AppInventoryTransactionLines` | 5 | 5 |
| `AppInventoryLots` | 2 | 2 |
| `AppInventoryLotAllocations` | 3 | 3 |
| `AppCustomers` | 1 | 1 |
| `AppCustomerAssets` | 1 | 1 |
| `AppBomVersions` | 2 | 2 |
| `AppBomItems` | 3 | 3 |

Supplemental fingerprints also covered inventory balances, Sales BOM snapshots, and customer-asset positions. All 14 deterministic SHA-256 fingerprints over pre-existing columns matched before and after. All 3 legacy lines remained effective and had null revision history. New Revision/Cancellation/Refund tables remained empty before new UAT data was created.

Legacy samples:

- `SO-202608-000001`: Confirmed, 2 lines, total 6,500,000, cost 480,000, profit 6,020,000, one posted payment totaling 3,000,000, and 2 original inventory references. Values and references were unchanged.
- `SO-202608-000002`: Draft, 1 line, zero posted total/payment and no inventory reference. Values were unchanged.
- External asset `EXT-651E6EC58E13` and its 2 positions were unchanged.
- Post-migration API returned both legacy orders; Sales list and Details returned HTTP 200, Details rendered the confirmed order number and 2 lines, and payment summary remained 3,000,000 paid / 3,500,000 remaining.

## 14. Stored Procedure And Report Verification

- Both `sp_VP_ReportSalesRevenue` and `sp_VP_ReportSalesProfit` compiled and executed before and after migration.
- Legacy results were identical: revenue 6,500,000; quantity 3; paid 3,000,000; remaining 3,500,000; cost 480,000; profit 6,020,000.
- UAT report totals matched the effective projection exactly: 6 active Confirmed orders, quantity 8, revenue 41,500,000, cost 850,000, and profit 40,650,000.
- The cancelled order was excluded, and the superseded machine line was not double-counted. Unchanged and price-only line cost snapshots remained intact.

## 15. Sales V1 UAT

All UAT records use prefix `UATSALE3_20260903_` and were created only after legacy reconciliation:

- A/E: price-only 10m -> 12m with 5m paid produced 7m remaining and no inventory transaction.
- B: quantity 1 -> 3 issued only delta 2 through FIFO.
- C: quantity 3 -> 1 was blocked until Warehouse confirmation, then reversed 2 to original lot and unit cost.
- D: machine product replacement reversed the old component, issued the new component, cancelled only the old pending asset, and created the new pending asset against the effective line.
- F: 10m total / 8m paid -> 6m total produced 2m refund due; the original payment stayed Posted and the refund was append-only.
- G/H: a paid non-machine Confirmed order cancelled immediately; return and refund appeared as independent tasks and both queues returned to zero after processing. Other non-machine orders were also adjustable.
- I: after machine installation, order state reported `HasInstalledMachine=true`; adjust and cancel both returned `SALES_018` and remained blocked.

## 16. Production Readiness Gate

Decision: `READY FOR PRODUCTION ROLLOUT` as of 2026-09-03. This means the migration, legacy compatibility, report behavior, Sales V1 UAT, build, focused automated tests, and EF model gate passed with no Severity 1/2 blocker. It does not authorize or imply a push, production migration, or deployment. A rollout still requires an explicit instruction, a fresh production backup, read-only baseline capture, exact artifact isolation, and post-deploy smoke/reconciliation under the deployment runbook.

## 17. Production Rollout Evidence

- Source: pushed `codex/warranty-release-review` to `origin` without force at `a4717aa361e931aaa2bb09fd55d20d0efd9599c2`. Web and DbMigrator were published from a detached clean worktree at that exact commit.
- Artifact: Web SHA-256 `5185335BA99BF7D16ACC00D44346A3EA989903F655AD726CACF074D8C60C0393`; DbMigrator SHA-256 `9ED876037E0B5D8501260D9835E2035F88316ED22EC324D9C6AE14F48AE8910E`. Local and VPS hashes matched. The Web artifact contained `openiddict.pfx`, generated client assets, and Font Awesome webfonts.
- Production target: runtime configuration and SQL connection both confirmed database `VPureLux`. The previous release was `/opt/vpurelux/releases/web-20260825-111401`.
- Backup: `/var/opt/mssql/data/VPureLux-pre-sales-v1-20260903-180246.bak`, created with `COPY_ONLY, CHECKSUM`; `RESTORE VERIFYONLY WITH CHECKSUM` reported a valid backup set.
- Migration: DbMigrator ran at 2026-09-03 18:08 Asia/Saigon. Latest migration changed from `20260824113235_AddServiceModule` to exactly `20260826051356_AddSalesPreInstallationV1Foundation`. No custom business DML or backfill ran.
- Legacy reconciliation: all 14 pre-existing-column fingerprints matched before migration, immediately after migration, and after Web smoke. Counts remained Orders 35, lines 202, payments 24, BOM snapshots 938, inventory transactions 286, inventory lines 1316, lots 303, allocations 961, balances 138, customers 37, assets 0, asset positions 0, BOM versions 132, and BOM items 701. All 202 legacy lines are effective with null revision references; all 24 legacy payments have null void metadata.
- Process tables: Revision, RevisionLine, RevisionAllocation, Cancellation, and Refund tables were empty after migration and remained empty after smoke. No `PRODSALESV1_20260903_` record was created.
- Reports: complete Revenue and Profit procedure outputs matched byte-for-byte before/after migration and after deploy. Both report pages returned HTTP 200 after authenticated login.
- Web: active release is `/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa`; rollback release remains `/opt/vpurelux/releases/web-20260825-111401`. Service `vpurelux-web` is active and three sequential post-warm-up health probes returned `Healthy`.
- Smoke: root, login, CSS, JS, Font Awesome font, Sales list, one legacy detail, Adjust page, cancellation modal, Returns, Refunds, Revenue, and Profit returned HTTP 200. Sales/Returns/Refunds server-side DataTable handlers returned valid paged JSON; Returns and Refunds were empty. The sampled non-machine Confirmed order exposed Adjust and Cancel actions, proving Sales eligibility is not machine-gated.
- Production limitations: there were no Draft orders and no Installed customer assets available for non-destructive live checks. Draft behavior, installed-machine lock, and Vietnamese decimal submission remain covered by the accepted rehearsal and focused automated tests; production smoke did not create data merely to repeat destructive scenarios.
- Logs: no HTTP 500, unhandled exception, or error-level journal entry was observed after deployment. Several transient Nginx 502 responses occurred only during process warm-up before the first healthy probe and did not recur.
