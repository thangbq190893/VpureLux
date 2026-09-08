# Sales Adjustment Release Review 001

## Decision

**ACCEPT.** The exact application source proven by authenticated VPL browser UAT is safe to use as the next production release candidate.

- Current production source: `b0bf197e8525acb2f254995af70b3da8a397a9f8`
- UAT-tested source: `f92899b6725d099c9a722f52d1e1b1e552ee41b9`
- Application release candidate: `f92899b6725d099c9a722f52d1e1b1e552ee41b9`
- Documentation HEAD at review start: `cf0078238e7d56ce6acd3442267a2de1fb3ac166`

This decision accepts the local source candidate only. Production readiness, read-only reconciliation, deployment, tagging, and smoke testing remain separately unauthorized.

## Git And Scope Review

`b0bf197e8525acb2f254995af70b3da8a397a9f8` is an ancestor of `f92899b6725d099c9a722f52d1e1b1e552ee41b9`. The intervening application commits are the Sales adjustment implementation and its Razor validation corrections: `0b20eeb`, `ec5ef4e`, `1bffafa`, `ad51e9a`, and `f92899b`. Other intervening commits modify tests, evidence, or documentation only.

Changed application files are limited to the Sales adjustment contract/orchestration/UI, its CustomerCare reconciliation DI seam, and Vietnamese localization:

- `src/VPureLux.Application.Contracts/Sales/ISalesPostConfirmationAppService.cs`
- `src/VPureLux.Application.Contracts/Sales/SalesPostConfirmationDtos.cs`
- `src/VPureLux.Application/Sales/SalesPostConfirmationAppService.cs`
- `src/VPureLux.Application/Warranty/SalesRevisionCustomerCareReconciler.cs`
- `src/VPureLux.Domain.Shared/Localization/VPureLux/vi-VN.json`
- `src/VPureLux.Web/Pages/Sales/Adjust.cshtml`
- `src/VPureLux.Web/Pages/Sales/Adjust.cshtml.cs`
- `src/VPureLux.Web/Pages/Sales/Adjust.js`

The diff from `f92899b6725d099c9a722f52d1e1b1e552ee41b9` to the documentation HEAD changes only `AGENT_TASKS.md`, `docs/SALES_ADJUSTMENT_FIX_001.md`, and `docs/SALES_ADJUSTMENT_UAT_001.md`. No application source changed after UAT. No unrelated application feature, migration, EF model/snapshot, DbContext configuration, appsettings, connection string, secret, or VPL runtime configuration is included.

## Validation And Security

The final Confirm handler removes only ModelState keys owned by the Start form: `StartInput`, `StartInput.*`, and the observed Razor-flattened `Reason`. `UpdateInput` and its line DTOs do not define a legitimate flattened `Reason` property; their optional reason field is `OverrideReason`. Adjustment-line validation therefore remains active.

The change does not disable validation globally. `OnPostStartAsync` still enforces the required start reason, Razor antiforgery behavior is unchanged, the PageModel remains authorized with `Sales.AdjustConfirmedBeforeInstallation`, and both `SubmitRevisionAsync` and existing mutation contracts retain AppService permission checks. The remaining structured validation warning contains field/error metadata but no posted values and is operational logging, not debug bypass behavior.

## Business And Transaction Review

The operator flow is one primary action: open adjustment, edit, and confirm. `SubmitRevisionAsync` updates the Draft from the current posted values and applies it inside the same `SalesOrderOperationCoordinator` execution when no Warehouse return is required. A stale Draft cannot silently win.

Price-only changes bypass Inventory. Positive quantity and added-product changes issue only their required delta and preserve unchanged factual allocations. Negative deltas remain Draft, do not alter the effective order or Inventory, and require Warehouse confirmation. Final success is shown only for an Applied revision.

The authoritative call chain remains `AdjustModel -> SalesPostConfirmationAppService -> SalesOrderOperationCoordinator -> existing Sales/Inventory/CustomerCare logic`. The coordinator still owns one distributed lock and transactional unit of work through Sales effective lines, FIFO/Inventory, CustomerCare reconciliation, revision state, and audit completion. Existing update/apply contracts remain available. No workflow framework, duplicate transaction boundary, planner/executor layer, or new database object was introduced. GrossPosted/NetPaid behavior is unchanged.

Changed query behavior remains bounded: Product, BOM, price, component, stock-item, lot, transaction, and revision facts are batch-loaded before line processing. No new repository query inside the changed product/line loops, repeated per-component lookup, giant Include graph, View, stored procedure, raw SQL, Dapper path, index, or migration was added.

## Reused Evidence

The accepted `docs/SALES_ADJUSTMENT_UAT_001.md` evidence maps exactly to `f92899b6725d099c9a722f52d1e1b1e552ee41b9`:

- Price `100000 -> 110000`: Applied; Inventory and original allocation unchanged.
- Quantity `1 -> 3`: Applied; exactly FIFO delta `2`; original allocation preserved.
- Product B quantity `2` added: only MAT_B delta `2`; Product A facts unchanged.
- Quantity `3 -> 2`: revision remains Draft; effective quantity remains `3`; no reversal; Warehouse confirmation required.

Authenticated Razor UAT passed 4/4 with matching read-only SQL reconciliation. Focused Web PageModel tests passed 4/4, accepted focused EF evidence passed 8/8, and the Release solution build passed with 0 errors. Because the application tree after UAT is unchanged, this review reused that evidence and ran only `git diff --check`.

## Remaining Risks And Next Task

The known combined Web testhost leak and Sales GrossPosted/NetPaid discrepancy remain separate work and are not part of this candidate. Production has not been accessed or changed, and no production smoke has been performed for this adjustment.

Next task: `SALES-ADJUSTMENT-PRODUCTION-READINESS`, requiring separate explicit production read-only authorization. This review does not authorize deployment.
