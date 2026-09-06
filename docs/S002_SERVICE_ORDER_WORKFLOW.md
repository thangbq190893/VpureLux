# S-002 Service Order Workflow Verification

Date: 2026-09-07 (Asia/Saigon). Branch: `codex/warranty-release-review`.
Starting HEAD: `9a0967b486c1ebd9414a04bcbf8a55dcca46ebe5`.
Implementation commit: `2f27ed81618403d7375b2af237025e6e931bbe3f`.
Decision: S-002 COMPLETE. S-003 is READY but was not claimed.

## Delivered Workflow

- One ServiceOrder belongs to one existing CustomerAsset and derives immutable customer/asset snapshots. Service does not create, transfer, remap or reactivate a machine.
- Executable transitions are Draft -> Confirmed -> InProgress. Draft, Confirmed and InProgress may be cancelled with a required reason. Completed and Cancelled are terminal.
- Completed remains in the historical schema and enum, but S-002 exposes no Complete/CompleteLine domain method, application contract, handler or UI action. Direct Confirmed -> Completed is impossible.
- Draft is the only editable state. Header, schedule, technician, address, note and planned lines are editable there. Confirmed/InProgress planned facts are immutable.
- Every Update, Confirm, Start and Cancel command carries the expected ABP ConcurrencyStamp. A stale command receives `VPureLux:SERVICE_019`; Cancel returns the message inside its modal rather than a generic 500.
- OrderNo uses the shared BusinessCodeGenerator as `SVC-yyyyMMdd####`, a database-side max seed and the existing unique index backstop. It does not load all codes into memory.

## Lines And Historical Facts

- Material requires one Component and may reference a matching active position on the selected CustomerAsset. It has no ServiceWork. New/replaced material must have an active inventory-enabled Component StockItem. No stock is issued in S-002.
- Labor requires one active ServiceWork with a known Unit. It has no Component or machine position. Labor remains outside Sales/Catalog Product.
- Planned quantity is a positive integer for both types. UnitPrice uses decimal money semantics. Labor StandardCostSnapshot is nullable: null means unknown and zero means known zero.
- Item code, name, unit, price and labor cost are document snapshots copied only at create or explicit item replacement. A Work/Component/BOM/policy edit never refreshes an existing line.
- Update input carries LineId. Unchanged lines keep their entity and ID; quantity/note/price changes touch only mutable fields; explicit item replacement keeps the line ID but intentionally takes new item snapshots; remove deletes only that line; add creates only the new line.
- Existing inactive Work/Component values continue to render and permit header/note edits. Current eligibility is checked only for a new line or explicit replacement.

## Queries, Permissions And UI

- Service Order list performs database Count, search/customer/status/date filtering, whitelisted sorting, stable tie breaking, Skip and Take. Unsupported sorts fall back to the documented default.
- Asset, Material, Work and Technician lookups are server-filtered and paged. Create/Edit expose Select2 page/continuation responses. Asset selection derives customer consistently; positions are loaded for that asset only.
- Permissions are `Service.View`, `Service.Create`, `Service.Edit`, `Service.Confirm`, `Service.Cancel` and S-001 `Service.ManageWorks`; Start intentionally reuses Confirm. The default-disabled Service feature blocks APIs even when permission is granted.
- UI is limited to Service Index, Create, Edit, Details and Cancel modal, with Works retained under the same primary Service menu. It uses server-side DataTables, dynamic Material/Labor rows, integer quantity inputs, ABP confirmation/modal patterns and no browser prompt/alert/native confirm.
- Razor output encodes customer, asset, catalog and note text. All mutation routes are antiforgery protected. The Service-scoped money binder accepts invariant HTML values and vi-VN decimal commas without changing global or Sales binding.

## Migration And Data Boundary

Forward migration: `20260906185742_AddServiceOrderWorkflow`.

Its Up has exactly two nullable AddColumn operations:

- `AppServiceOrders.CancellationReason` nvarchar(1000).
- `AppServiceOrderLines.StandardCostSnapshot` decimal(18,2).

There is no business DML, backfill, default, table recreation, check constraint against unknown legacy data, or Sales/Catalog/Warranty/Inventory core-table change. The migration was generated and inspected only; it was not applied. EF tooling used the unreachable dummy target `127.0.0.1,1 / S002_OFFLINE_ONLY` and reports no pending model changes after the Release build.

No connection was made to VPL or production. No DbMigrator, database update, runtime configuration, deployment, restart, symlink action, push, FIFO issue, maintenance/reminder mutation, revenue recognition or payment operation occurred.

## Verification

- Release solution build: 0 errors, four pre-existing warnings (two OpenIddict nullable warnings, Scriban NU1903 and Web test entrypoint CS7022).
- Domain Service: 14/14.
- Application Service contracts: 6/6.
- EF Service: 72/72, including S-002 workflow 7/7.
- EF Sales/Inventory/Warranty/CustomerCare regression: 110/110.
- Web Service: 18/18. Warranty Web regression: 13/13.
- Node syntax: Index.js, EditForm.js and Details.js PASS.
- EF pending model changes: NONE. `git diff --check`: PASS.

Coverage includes transition and terminal rules, cancellation reason, Material/Labor exclusivity, integer quantity, null-versus-zero cost, snapshot preservation after catalog changes, explicit replacement, line add/update/remove identity, inactive historical selections, stale edits/transitions, permission deny/allow, disabled feature, list and lookup page 2, translated Material eligibility query, no Inventory/CustomerCare/revenue side effects, antiforgery reject/success, vi-VN submit, safe encoding and stale Cancel modal feedback.

Tests exposed and fixed three implementation defects before completion: generic EF graph update treated a new owned line as Modified; a projected private record prevented Material lookup translation; Cancel caught concurrency but initially did not render the message in its modal.

No combined full Web suite is claimed. A local SQLite/in-memory browser listener was attempted, did not start from the test project entrypoint, and was stopped; therefore no S-002 screenshot or browser-interaction claim is made. HTTP integration renders the compiled Razor pages and exercises valid mutations without external data.

## Deferred To S-003+

S-003 owns atomic completion, actual Material quantities, FIFO ServiceIssue, inventory cost snapshots, idempotency, maintenance events, reminder close/successor behavior and rollback rules. S-004 owns payments/receivables; S-005 owns Service/consolidated reports; S-006 owns authorized SQL Server legacy rehearsal, migration application, UAT and rollout.

S-003 must preserve S-002 line IDs and historical snapshots. It must batch-load stock/lot/care dependencies, avoid N+1 queries, and never derive old facts from current Work, Component, BOM or replacement policy settings.
