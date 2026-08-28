# Sales Pre-Installation V1 - Technical Implementation

Status: Phase 1 backend foundation completed locally on 2026-08-26. Not deployed.

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

Focused Domain/Application/EF tests cover state invariants, delta behavior, exact lot/cost reversal, atomic rollback, payment/refund calculations, cancellation obligations, installation lock, permissions, idempotency, and existing Sales/Inventory/CustomerCare regressions. Final local evidence: Application Sales `2/2`, Domain Sales/Inventory `30/30`, EF Sales/Inventory `84/84`, full solution build `0` errors, and no pending EF model changes.

## 10. Intentionally Deferred

- Full Razor/DataTables/ABP modal UI.
- `CancelAndClone` action; the backend copy boundary is documented but not exposed in Phase 1.
- Reconciliation of already-created pending CustomerCare assets when an applied revision changes machine product or unit count. Cancellation already cancels pending assets, installation is blocked while a Draft revision exists, and intake reads only effective lines; the applied-revision asset reconciliation belongs with the Phase 2 operator workflow.
- Warehouse quarantine/inspection subsystem and accounting-ledger redesign.
- Any new Reservation, Fulfillment, Delivery, Completed, workflow-engine, or event-sourcing model.

## 11. Known Risks

- Phase 1 must not be enabled for production operators until the Phase 2 UI and pending-asset reconciliation are implemented and UAT-approved.
- Non-machine Confirmed orders have no terminal modification boundary in V1; permission, audit, warehouse confirmation, and atomic posting are the deliberate controls until a separately approved delivery/accounting-close boundary exists.
- Existing SQL Server report procedures require the same effective-line predicate as the EF/SQLite fallback.
- Multi-instance safety depends on both the configured distributed lock provider and database uniqueness/concurrency constraints.
