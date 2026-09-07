# Sales Adjustment Fix 001

## Current Decision

Implementation and captured EF evidence are complete, but final acceptance is **BLOCKED** on the known Web test-host leak: the focused PageModel/UI run did not produce a final test result in this environment. Do not deploy from this checkpoint. Resume by obtaining a captured final result for the three focused `Sales_Adjust_Page` tests, then perform operator review.

## Confirmed Root Cause

The old page exposed two operator actions: Save and Apply. Save only persisted a Draft revision; Apply later used that stored Draft and ignored the current unsaved form. This made a successful-looking sequence possible while the effective order remained unchanged.

The confirmed-order adjustment page now has one primary operator command: **Xac nhan dieu chinh**. It submits the values currently posted by the form.

## Operator Flow

1. A manager opens a confirmed, pre-installation order and edits lines, quantities, or prices.
2. The manager selects **Xac nhan dieu chinh**.
3. The server validates and refreshes the Draft from the posted values inside the existing Sales order coordination boundary.
4. If no return confirmation is needed, it applies the revision immediately in the same transaction.
5. If a negative inventory delta needs Warehouse confirmation, it leaves the revision as Draft and tells the manager that the effective order has not changed.
6. Warehouse confirmation continues to use the existing return-confirmation flow; it is not silently bypassed.

The existing `UpdateRevisionAsync` and `ApplyRevisionAsync` contracts remain for compatible internal/existing callers. Draft remains an internal persistence state only when Warehouse return is pending; it is not a user-facing Save step. The page cannot report final success unless the returned revision status is `Applied`.

## Implementation Shape

Call chain: `AdjustModel.OnPostConfirmAsync` -> `ISalesPostConfirmationAppService.SubmitRevisionAsync` -> `SalesOrderOperationCoordinator.ExecuteAsync` -> existing revision delta engine, Inventory/FIFO, CustomerCare reconciliation.

`SubmitRevisionAsync` is the only added public command. It uses the posted form to update the Draft and either applies immediately under the same coordinator transaction or returns the Draft unchanged when Warehouse confirmation is required. No workflow service, command service, planner, executor, new database object, or migration was introduced. The small reconciler interface is only a DI seam; the fault probe/decorator exists only in the EF test assembly.

## Atomicity And Performance

`SubmitRevisionAsync` reuses `SalesOrderOperationCoordinator` and its transactional unit of work. A single accepted immediate adjustment covers revision changes, effective Sales lines, Inventory/FIFO movements, CustomerCare reconciliation, totals, and audit work.

The update and apply paths retain the existing bounded, set-based product, BOM, price, stock, and lot loading. No new product/BOM/component lookup is introduced in a line loop, and no giant Include graph is introduced. The pre-existing engine retains a per-affected-line Inventory idempotency lookup; it is bounded and necessary to preserve the existing per-revision-line factual key semantics, so it was not refactored in this narrow UX fix. No view, stored procedure, raw SQL, schema change, migration, backfill, or business-data operation was added.

## Regression Evidence

Focused SQLite EF test runs on 2026-09-08:

- `Submit_Revision_Should_Apply_Current_Price_Without_Inventory_And_Not_Use_Stale_Draft`
- `Submit_Revision_With_Negative_Delta_Should_Wait_For_Warehouse_And_Keep_Order_Effective`
- `Submit_Revision_Failure_After_Inventory_Or_CustomerCare_Should_Roll_Back_Everything` (two injected late-failure cases)

Result: 4 passed. The fault-injection tests verify that a failure after Inventory posting and a failure after CustomerCare reconciliation leave the effective order unchanged, keep the revision Draft, add no revision inventory transaction, and create no customer asset.

Additional existing Sales regression evidence run individually: quantity increase and same-key replay (1 passed), product replacement reversal/issue (1 passed), add/remove only affected deltas (1 passed), and quantity decrease Warehouse return using factual cost (1 passed). Total captured EF evidence for this task: **8 passed**.

The focused Web test project builds with 0 warnings and 0 errors. Three new PageModel/UI tests cover current-form submission, Details redirect only after `Applied`, Warehouse-pending redirect, and the single Confirm action. Its focused host was started, but did not return a final test summary before process exit, matching the already documented Web test-host leak. It is deliberately not counted as a pass; the code and test source remain available for a stable-host rerun.

No dedicated business-audit completion fault probe was added. The Inventory and CustomerCare late-failure tests exercise the shared coordinator transaction. Adding an audit-only fault seam would require disproportionate test-only production infrastructure for this narrow fix.

The separate known Sales `GrossPosted`/`NetPaid` reporting discrepancy remains out of scope and unchanged.

## Environment Boundary

Only local source, Release builds, and SQLite test infrastructure were used. VPL, VPureLux production, migrations, deployment, restart, and push were not used.

## Remaining Risk

The new manager workflow has not yet received browser/operator acceptance, and the known Web test-host leak prevents claiming a captured runtime PASS for the focused PageModel tests in this environment. This is a review/UAT follow-up, not a reason to change Sales delta or payment behavior.
