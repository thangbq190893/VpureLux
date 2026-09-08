# Sales Adjustment Fix 001

## Current Decision

**DONE - READY FOR RELEASE REVIEW.** The focused Razor Page defect was repaired locally, the PageModel suite passes 4/4, and authenticated browser UAT plus read-only VPL reconciliation passes all four required operator cases. This is not production deployment approval.

## Confirmed Root Cause

The old page exposed two operator actions: Save and Apply. Save only persisted a Draft revision; Apply later used that stored Draft and ignored the current unsaved form. This made a successful-looking sequence possible while the effective order remained unchanged.

The confirmed-order adjustment page now has one primary operator command: **Xac nhan dieu chinh**. It submits the values currently posted by the form.

The later browser diagnosis confirmed a second, handler-specific cause. Razor Pages validates all bound properties before `OnPostConfirmAsync`. `StartInput.Reason` is required for the Start handler but is not posted by the Confirm form, leaving an invalid `StartInput.Reason` ModelState entry. The page therefore re-rendered before it called `SubmitRevisionAsync`; no Sales, Inventory, or CustomerCare mutation occurred.

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

For the Confirm handler only, `AdjustModel` removes `StartInput` ModelState entries before validating `UpdateInput`. This keeps Start validation intact for its own handler while ensuring a Confirm request validates only facts that its form actually posts. No Sales application/domain path or delta-engine behavior changed.

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

The focused Web test project builds successfully with 0 errors. The only warning is external to project code: `Microsoft.NET.Test.Sdk` `CS7022`. `Sales_Adjust_Page` now passes **4/4**: direct current-form submission/redirect, Warehouse-pending behavior, no final-success redirect before `Applied`, and the exact `StartInput.Reason` ModelState regression. The PageModel helper now supplies ABP's `IAbpLazyServiceProvider`, so these are handler executions rather than failures in the test harness.

No dedicated business-audit completion fault probe was added. The Inventory and CustomerCare late-failure tests exercise the shared coordinator transaction. Adding an audit-only fault seam would require disproportionate test-only production infrastructure for this narrow fix.

The separate known Sales `GrossPosted`/`NetPaid` reporting discrepancy remains out of scope and unchanged.

## Environment Boundary

Implementation evidence used local source, Release builds, and SQLite test infrastructure. The later UAT attempt used VPL only after an explicit `DB_NAME()` guard, through normal browser/application flows plus read-only SQL reconciliation. VPureLux production, migrations, deployment, restart, and push were not used.

## Browser/VPL UAT Attempt - 2026-09-08

Source tested: `0b20eeb fix(sales): simplify confirmed order adjustment`.

The local Web runtime was explicitly configured for the authorized UAT catalog. A read-only SQL guard returned `DB_NAME() = VPL`. The normal Development configuration was not used because it targets a separate VPL instance with a stale Warranty schema; production `VPureLux` was not accessed.

Fixture `SO-202609-000005` (`1b799b75-4419-40d3-8e9e-3a239169fd08`) was created and confirmed through the actual authenticated Razor flow with existing UAT-prefixed non-machine catalog/warehouse/customer records. The browser then opened Adjust, supplied reason `UAT price-only`, changed the visible unit price from `100000` to `110000`, and selected the only primary submit action, **Xac nhan dieu chinh**. Browser AX snapshots before submit and after the response are retained in the Codex UAT task transcript.

Result: **Case 1 failed safely**. The POST to `Sales/Adjust/...?...handler=Confirm` returned HTTP 200 in about 1.7 seconds and re-rendered the same form; it did not show a final success message or redirect to Details. Read-only VPL reconciliation found the revision `3eeb6f2a-9eba-c576-b809-3a23916acd23` still Draft, `AppliedAt = NULL`, the effective line still quantity `1` / price `100000`, and no revision Inventory transaction. No unhandled server exception was logged. Local source diagnosis subsequently proved the cause: automatic cross-handler validation of required `StartInput.Reason` prevented `OnPostConfirmAsync` from calling `SubmitRevisionAsync`.

Cases 2-4 were deliberately not run because the approved UAT rule requires stopping on the first failure. No application code, migration, direct data manipulation, deployment, production/VPS access, or push occurred.

## Remaining Risk

Final browser/VPL evidence is recorded in `docs/SALES_ADJUSTMENT_UAT_001.md`: price-only, quantity increase, add product, and decrease waiting for Warehouse all PASS. Production remains unchanged and requires a separate release review. The known full/combined Web test-host leak remains separate and is not claimed as resolved. GrossPosted/NetPaid also remains out of scope.
