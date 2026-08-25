# VPureLux Product Task List And Agent Handoff

This file is the single source of truth for implementation order and agent handoff.
Every agent must read and update this file so another agent can continue without a chat summary.

Last updated: 2026-08-25 (Asia/Saigon)
Current product stage: Service VPL UAT complete; production Web hotfix pending
Current active task: S-006
Next task: Obtain explicit approval for a Web-only production redeploy of the antiforgery hotfix; run no DbMigrator and write no production business data
Service implementation gate: OPEN; W-GATE accepted by the user

## 1. Mandatory Agent Protocol

Before changing code:

1. Read `AGENTS.md`, `$vpurelux-engineering`, this file, and the relevant source/design documents.
2. Run the VPureLux skill preflight and inspect `git status --short`.
3. Treat all existing changes as user-owned; never revert or include unrelated files.
4. Verify the selected task is `READY` and all dependencies are `DONE`.
5. Change the task to `IN_PROGRESS` and fill in the Active Work Record before editing.
6. Work on one task only unless this file explicitly groups tasks into one PR.

Before ending a task:

1. Update the task status to `DONE`, `BLOCKED`, or `READY`.
2. Record exact files changed, migration/data impact, tests, build result, and deployment state.
3. Append a Handoff Log entry with the next recommended action.
4. Set `Current active task` and `Next task` at the top of this file.
5. Never mark a task `DONE` when required tests or acceptance criteria have not passed.

Concurrency rule:

- Only one task may be `IN_PROGRESS` in this file.
- Do not overwrite another agent's active record. If it looks stale, verify git/worktree state before claiming it.
- Re-read this file immediately before the final update in case another agent changed it.

Status values:

- `DONE`: implementation and required verification completed.
- `IN_PROGRESS`: one agent currently owns the task.
- `READY`: dependencies complete; may be claimed next.
- `PENDING`: blocked by unfinished dependencies or product gate.
- `BLOCKED`: cannot proceed; the blocking condition is recorded.
- `HOLD`: intentionally deferred or requires separate user approval.

## 2. Approved Product Direction

- Keep the stable Sales module unchanged in behavior.
- Keep Service separate from Sales. Labor/service work must not be Catalog Products.
- Finish Warranty/CustomerCare before beginning Service.
- A replacement schedule belongs to an actual customer machine and component position.
- Sold-machine schedule condition: Product is configured as a machine, installation is confirmed, and the actual position maps to a component with an enabled replacement policy.
- Start the first cycle from actual installation, not sale date.
- Start later cycles from actual replacement/service completion.
- Support machines bought from the company and machines bought elsewhere.
- Keep existing database data unchanged unless a separate, previewed backfill is explicitly approved.
- Use schema-only migrations by default.
- Use ABP ModalManager and server-side DataTables; no browser prompt/alert/confirm and no top-100 pseudo-pagination.

Primary design inputs:

- `docs/service-care-design/VPureLux_Tai_lieu_nghiep_vu_Dich_vu_Cham_soc_khach_hang.docx`
- `docs/service-care-design/VPureLux_Tai_lieu_ky_thuat_Dich_vu_Cham_soc_khach_hang.docx`
- `docs/VPURELUX_SERVER_DEPLOYMENT_RUNBOOK.md`
- `docs/MODULE_MAP.md`

## 3. Verified Current State

Commit baseline reviewed: `aec9f91 feat(warranty): add replacement reminders`

Already present:

- `AppComponentReplacementPolicies`, `AppCustomerAssets`, and `AppAssetReplacementReminders`.
- Replacement policy fields: enabled, cycle months, warning days before due, note.
- Warranty permissions, menu, localization, reminder list, and policy list.
- Database-side count/filter/sort/Skip/Take for current Warranty lists.
- Reminder complete/skip/reschedule application methods.
- Hourly rolling application logs.
- One Sales integration test proving the current MVP creates assets/reminders.

Known incomplete or incorrect behavior:

- `SalesOrderAppService.ConfirmAsync` synchronously calls `WarrantySalesIntegrationService`.
- A Warranty failure can still fail or roll back stable Sales confirmation.
- Every sold product can create an asset; there is no Product-is-machine configuration.
- Asset and reminders are created only when a configured component is found, rather than creating a pending machine first.
- Asset status starts as Active; there is no PendingInstallation/PendingReview workflow.
- `WarrantyStartDate` and first due date use sale/confirmation date.
- `CustomerAsset` requires ProductId, SalesOrderId, and SalesOrderLineId, so external machines are unsupported.
- No customer-asset component/position model exists for Core 1 through Core 9 or other positions.
- No installation confirmation, serial, installation address, technician, mapping review, idempotency, or installation event exists.
- Reminder is tied directly to Sales references and a Component, not to an actual asset position.
- No external machine baseline/missing-baseline workflow exists.
- No customer -> machine -> maintenance history/timeline exists.
- Policy edit and reminder reschedule still use `window.prompt`.
- Reminder complete currently creates the next cycle directly without a maintenance event or service source.
- Warning date is not exposed as a first-class query value; current UI mainly filters DueDate/status.
- Existing legacy Warranty rows need a non-destructive PendingReview treatment after schema expansion.
- Dedicated Warranty domain/application/repository/permission/Web test coverage is incomplete.
- `docs/MODULE_MAP.md` does not yet describe the Warranty module.

Known pre-existing worktree changes at this baseline:

- `src/VPureLux.Web/Pages/Bom/Edit.cshtml`
- `src/VPureLux.Web/Pages/Inventory/Posting.js`
- `src/VPureLux.Web/Pages/Inventory/Receipt.cshtml`
- `test/VPureLux.Web.Tests/Pages/BomPagesTests.cs`
- `test/VPureLux.Web.Tests/Pages/InventoryPagesTests.cs`
- `src/VPureLux.Web/appsettings.Production.json` (untracked at baseline)
- `docs/service-care-design/` (untracked design artifacts at baseline)

These paths are not automatically in scope for W-001. Re-run preflight on every task because this list is only a historical baseline, not a substitute for current `git status`.

## 4. Delivery Order

| ID | Task | Status | Depends on |
|---|---|---|---|
| W-001 | Protect Sales and establish Warranty feature gates | DONE | None |
| W-002 | Add CustomerCare schema foundation with schema-only migration | DONE | W-001 |
| W-003 | Complete machine and component policy configuration UI | DONE | W-002 |
| W-004 | Add idempotent Sales-to-CustomerCare intake worker | DONE | W-002, W-003 |
| W-005 | Implement installation confirmation and first schedules | DONE | W-004 |
| W-006 | Implement external customer machines and component positions | DONE | W-005 |
| W-007 | Complete reminder lifecycle, machine history, and ABP UI | DONE | W-005, W-006 |
| W-008 | Warranty regression, UAT, migration rehearsal, and rollout | DONE | W-001..W-007 |
| W-GATE | Warranty/CustomerCare acceptance gate | DONE | W-008 |
| S-001 | Service module foundation and work catalog | DONE | W-GATE |
| S-002 | Service order aggregate, lines, permissions, and UI | DONE | S-001 |
| S-003 | Service completion, FIFO issue, and schedule integration | DONE | S-002 |
| S-004 | Service payments and receivables | DONE | S-003 |
| S-005 | Service and consolidated reports | DONE | S-003, S-004 |
| S-006 | Service UAT, reconciliation, and rollout | IN_PROGRESS | S-001..S-005 |

## 5. Warranty/CustomerCare Tasks

### W-001 - Protect Sales And Establish Warranty Feature Gates

Status: DONE

Goal:

- Prevent CustomerCare/Warranty failures from causing Sales confirmation HTTP 500 or rollback.
- Establish disabled-by-default feature/config gates for the new intake workflow.

Scope:

- Add configuration keys for CustomerCare enablement, Sales intake enablement, and go-live boundary.
- Remove the synchronous Warranty creation call from the Sales confirmation transaction.
- Do not delete existing Warranty tables or rows.
- Add regression tests proving Sales confirmation/inventory posting succeeds independently of CustomerCare.
- Replace the old MVP test expectation with the future intake boundary; do not silently remove coverage.
- Add an operational note explaining that no automatic backfill runs in this task.

Acceptance:

- Confirming a machine or non-machine Sales order preserves existing Sales/BOM/FIFO/financial behavior.
- A simulated CustomerCare failure cannot make Sales return HTTP 500.
- No new reminder is created synchronously inside Sales Confirm.
- Existing Warranty data is untouched.
- Focused Sales and Inventory regression tests pass.

No migration is expected unless feature configuration uses a new persisted structure; prefer app configuration in this task.

### W-002 - Add CustomerCare Schema Foundation

Status: DONE

Goal:

- Extend the recent Warranty schema so it can represent machines, installations, positions, history, and external sources without changing stable core tables.

Scope:

- Add `ProductMachineSetting` companion entity/table; default false when no row exists.
- Evolve `CustomerAsset` for SoldByCompany/External source and PendingInstallation/Active/Inactive/Transferred/PendingReview states.
- Make Product/Sales references nullable only where required for external machines.
- Add serial, brand, model, installation fields, source snapshots, and concurrency protection.
- Add `CustomerAssetComponent` for position code/name, optional Component mapping, baseline, status, and snapshots.
- Add append-only `AssetMaintenanceEvent`.
- Evolve reminders to reference `CustomerAssetComponent`; add WarningDate, trigger source, source references, and close reason.
- Add `CustomerCareSyncFailure` for isolated intake errors.
- Add unique/check/index constraints for asset number, serial review, source line + unit index, active position, one open reminder per position, and idempotency.
- Produce a schema-only EF migration and inspect the model snapshot.

Data rule:

- No migration UPDATE/DELETE/backfill.
- Existing assets/reminders remain intact and become PendingReview by interpretation when installation fields are null.

Acceptance:

- Migration Up contains only approved schema operations.
- Existing core Sales/Product/Component/BOM/Inventory/Customer tables are not altered.
- SQL Server and SQLite model/tests pass.
- Down is documented as non-production only.

### W-003 - Complete Machine And Component Policy Configuration UI

Status: DONE

Goal:

- Let authorized users configure which Products are machines and which Components are tracked for replacement.

Scope:

- Add server-side DataTable for Product machine settings.
- Keep no-row/default as not a machine.
- Keep Component replacement default as not tracked.
- Replace `window.prompt` policy editing with an ABP modal containing enabled, cycle months, warning days, and note.
- Add ABP modal for Product machine setting.
- Add permissions, localization, menu placement, validation, antiforgery, and audit.
- Ensure disabling a setting affects only future intake/schedules; do not delete history.

Acceptance:

- No browser prompt/alert/confirm remains in the configuration workflow.
- Lists use database paging/filter/sort.
- Permission and modal Web tests pass.
- Changing a policy does not rewrite existing reminder snapshots.

### W-004 - Add Idempotent Sales-To-CustomerCare Intake Worker

Status: DONE

Goal:

- Create pending customer machines from confirmed Sales without coupling CustomerCare reliability to Sales.

Scope:

- Add an ABP periodic/background worker with distributed lock.
- Query confirmed Sales lines joined to ProductMachineSetting and anti-joined to existing assets.
- Respect configured go-live boundary; do not scan historical orders by default.
- Require positive integer Sales quantity for machine-unit creation; record invalid lines as sync failures.
- Create one PendingInstallation asset per sold unit with source line + unit index idempotency.
- Snapshot customer, product, order, and sold BOM data needed for installation review.
- Add retry/list UI for sync failures with permission and ABP confirmation.

Acceptance:

- Non-machine Sales lines create no assets.
- Machine quantity two creates exactly two PendingInstallation assets.
- Re-running the worker creates no duplicates.
- One failing line does not block the batch.
- Sales confirmation remains successful when worker processing fails.
- No reminders exist before installation confirmation.

### W-005 - Implement Installation Confirmation And First Schedules

Status: DONE

Goal:

- Start replacement schedules only after a technician confirms the actual machine installation and component positions.

Scope:

- Add Pending Installations server-side list.
- Add full-page or large ABP modal workflow for serial, address, installed date, technician, and BOM snapshot review.
- Build actual positions from the sold BOM snapshot; allow omission, replacement mapping, and additional positions.
- Batch-load components and enabled policies; no repository query in loops.
- Persist asset, positions, installation event, and first reminders in one Unit of Work.
- Add installation idempotency and concurrency protection.
- Compute WarningDate = DueDate - WarningDaysBeforeDue and DueDate = InstalledAt + CycleMonths.

Acceptance:

- Product machine + confirmed installation + mapped enabled component are all required for a reminder.
- A repeated confirmation returns the prior result and creates no duplicate event/reminder.
- Current BOM changes after sale do not change the sold BOM snapshot used for review.
- Machine not installed has no active reminder.
- Sales/Inventory regression remains green.

### W-006 - Implement External Customer Machines And Positions

Status: DONE

Goal:

- Manage customer machines bought outside the company and preserve all known/unknown component information.

Scope:

- Add customer asset create/edit/details workflow for External source.
- Support brand/model/serial/address/contact snapshots without Product/Sales references.
- Add positions such as Core 1 through Core 9 and arbitrary real positions.
- Allow position mapping to an internal Component or Unmapped status.
- Allow confirmed last-installed/replaced baseline or MissingBaseline.
- Create initial reminders only when mapping, enabled policy, and confirmed baseline all exist.
- Add duplicate serial warning and controlled merge/transfer design; implement only approved operations.

Acceptance:

- An external machine can retain nine positions when only three are mapped or have baselines.
- Missing baseline never becomes a fabricated date.
- Updating positions 1-3 does not change positions 4-9.
- Lists/details are database-paged and do not assemble history in memory.

### W-007 - Complete Reminder Lifecycle, Machine History, And ABP UI

Status: DONE

Goal:

- Provide the complete operator workflow for warning, due, overdue, skip, reschedule, suspend, and history by customer and machine.

Scope:

- Replace reminder reschedule `window.prompt` with ABP modal.
- Use ABP modal/confirmation with reason and operator audit for complete/skip/suspend/reschedule actions.
- Calculate NotDue/Warning/Overdue from WarningDate/DueDate at query time; do not create daily status rows.
- Record append-only maintenance events for every lifecycle action.
- Preserve cycle/warning snapshots on existing reminders.
- Ensure one open reminder per asset position.
- Add customer -> machines -> machine detail/timeline views with server-side paging.
- Show current positions, mapping, latest event, next due date, source, and audit context.
- Do not complete replacement from the reminder in a way that bypasses Service once Service is enabled; define the transitional Warranty-only action explicitly.

Acceptance:

- No browser popup remains in Warranty pages.
- Status/date filters and sorting execute at the database.
- History for two machines of the same model never mixes.
- Policy changes affect future cycles only.
- Concurrency/idempotency and permission tests pass.

### W-008 - Warranty Regression, UAT, Migration Rehearsal, And Rollout

Status: DONE

Goal:

- Prove the completed Warranty/CustomerCare module is safe before Service development begins.

Scope:

- Complete Domain, Application/EF, permission, Web, API, and query tests.
- Add explicit Sales/BOM/Inventory/report regression runs.
- Rehearse migration on a database copy; verify no data DML and legacy PendingReview behavior.
- Perform Release publish and verify generated static assets/icons.
- Execute desktop/mobile UI and DataTables UAT.
- Deploy only after explicit user request using full Web + DbMigrator artifacts and atomic release.
- Smoke-test health, login, icons, Sales confirm, machine intake, installation, reminder actions, and logs.
- Update `docs/MODULE_MAP.md` and operational documentation.

Acceptance:

- All required tests and UAT cases are recorded as passed.
- Existing data count/reconciliation is unchanged except explicitly created UAT records.
- No Sales 500 regression and no broken static asset/icon regression.
- Rollback release/path is recorded if deployed.

### W-GATE - Warranty/CustomerCare Acceptance Gate

Status: DONE

This gate may be marked DONE only when:

- W-001 through W-008 are DONE.
- The user accepts the Warranty/CustomerCare behavior.
- Sales, Inventory, BOM, and existing reports remain regression-green.
- Migration/data reconciliation and deployment smoke evidence are recorded.
- No unresolved Severity 1 or Severity 2 Warranty defect remains.

When W-GATE becomes DONE, change S-001 from HOLD to READY. Do not start any Service task earlier.

## 6. Service Tasks - Locked Until W-GATE

### S-001 - Service Module Foundation And Work Catalog

Status: DONE

- Add separate Service bounded module, permissions, menus, feature flag, number sequence, and non-inventory work/labor catalog.
- Do not add service/labor products to Sales or Catalog Product.
- Use schema-only migration and ABP modal/server-side DataTable UI.

### S-002 - Service Order Aggregate, Lines, Permissions, And UI

Status: DONE

- Add ServiceOrder with Draft -> Confirmed -> InProgress -> Completed/Cancelled state machine.
- Support mutually exclusive Material and Labor lines.
- Bind one customer machine per order in phase one.
- Snapshot prices/cost assumptions and use full-page workflow plus server-side list.

### S-003 - Service Completion, FIFO Issue, And Schedule Integration

Status: DONE

- Add InventoryTransactionType.ServiceIssue without changing existing enum values.
- Batch-load FIFO lots for all material lines; no N+1.
- Atomically issue actual materials, snapshot cost/revenue, append maintenance events, close old reminders, and create new cycles.
- Only performed Material lines touch stock/schedules; Labor never touches stock.

### S-004 - Service Payments And Receivables

Status: DONE

- Add separate Service payment ledger with Posted/Void/idempotency behavior.
- Treat payment before completion as customer advance, not service revenue.
- Keep SalesOrderPayments unchanged.

### S-005 - Service And Consolidated Reports

Status: DONE

- Add Service revenue/profit reports and consolidated Sales + Service read model with source dimension.
- Keep existing Sales stored procedures unchanged.
- Reconcile completed Service only and exclude advances from revenue.

### S-006 - Service UAT, Reconciliation, And Rollout

Status: IN_PROGRESS

- Test external machine Core 1-3 replacement plus Labor, FIFO, missing stock rollback, concurrency, payment, reminders, and consolidated reports.
- Publish/deploy only with explicit approval and full regression/smoke evidence.

## 7. Active Work Record

Task ID: S-006
Agent/task name: Codex - full Service UAT on isolated VPL data
Started at (Asia/Saigon): 2026-08-25
Branch and starting commit: main / c3df0a1
Goal for this run: Run the complete Service workflow on test database `VPL`, including positive flow, insufficient-stock rollback, payment/revenue boundaries, reminder/notify behavior, and database reconciliation, without accessing production data.
Files changed: Service `Details`, `Index`, and `Works` Razor/JavaScript antiforgery handling; focused Web regression tests; this handoff file.
Database/data impact: UAT records were intentionally created only in `VPL`. Production database `VPureLux` was neither queried nor written in this run. A verified `VPL` backup exists before UAT.
Verification completed: Authenticated browser workflow, AppService reconciliation, direct read-only SQL against explicit database `VPL`, focused Domain/EF/Web tests, and Release Web build.
Current blocker: The antiforgery fix found during UAT is not deployed to production. A production Web-only redeploy remains pending; do not run DbMigrator or write production business data for this fix.

## 8. Handoff Log

### 2026-08-25 - Full Service UAT Passed On VPL; Antiforgery Hotfix Ready

- Agent/authorization: Codex. The user explicitly authorized full testing on UAT database `VPL` and prohibited all production-data access. Every mutating database operation in this run targeted only `VPL`; database `VPureLux` was not queried or changed.
- Safety/backup: Baseline on `VPL` was Customers 0, Components 0, Warehouses 0, lots 0, customer assets 0, Service works/orders/payments 0, with latest migration `20260824113235_AddServiceModule`. Created `/var/opt/mssql/data/VPL-pre-service-uat-20260825-170432.bak` using `COPY_ONLY` + `CHECKSUM`; `RESTORE VERIFYONLY WITH CHECKSUM` passed. Size is 11 MiB and SHA-256 is `239403b0b5e9f1e7bb9e8468c275fe558190dd9c50cc3df9762a2d52e3c35ce9`.
- UAT master data: Customer `CUS-202608250001`; replacement-tracked Component `MAT-202608250001` with cycle 3 months and warning lead 15 days; warehouse `UAT-WH-01`; stock item `ae92473d-0474-a938-3624-3a234a9250ca`; receipt lot `LOT-202608250001`, quantity 10 at cost 100,000; external machine `EXT-2F4C53A7241C` with Core 1 mapped to the Component; work `UAT-LABOR-01` at 150,000.
- Defect/root cause/fix: The first UI Confirm action returned a generic error because Service pages posted Razor handlers through `abp.ajax` without a `RequestVerificationToken`; the server log proved antiforgery validation returned HTTP 400, not a business 500. Added hidden Razor POST token forms and explicit antiforgery headers to Service Details actions/void payment, Index cancellation, and Works activation/deactivation. Added Web regression assertions for all three pages.
- Happy path: `SVC-202608250001` progressed Draft -> Confirmed -> InProgress -> Completed through the authenticated UI. One Material at 300,000 plus one Labor at 150,000 produced revenue 450,000, cost 100,000, profit 350,000. A 100,000 advance was recorded before completion and remained excluded from revenue; a final 350,000 receipt made paid 450,000 and remaining 0.
- Inventory/customer care: FIFO created one posted `ServiceIssue` transaction `e9a71e5a-1b2e-ec73-8573-3a234aa02f80` with one unit allocated from `LOT-202608250001` at 100,000. Balance and lot both moved 10 -> 9 and inventory value is 900,000. Machine history contains the Service replacement event sourced from `SVC-202608250001`. One pending reminder exists with due date 2026-11-25 and warning date 2026-11-10.
- Notify/report verification: Temporarily rescheduled the UAT reminder to due-now; notification summary became Warning 1 / Total 1. Restored the due date to 2026-11-25; final notification count returned 0 and the 15-day warning date returned to 2026-11-10. Consolidated report contains exactly one completed document with Service revenue 450,000, cost 100,000, profit 350,000, paid 450,000, remaining 0; Sales revenue remains 0.
- Failure/rollback path: `SVC-202608250002` requested 10 units while only 9 were available. Completion returned `INV_001`; the order remained Confirmed, stock remained 9, reminder count remained 1, and report document count remained 1. The test order was then cancelled and remains as an explicit cancelled UAT record.
- Database reconciliation: A read-only `sqlcmd` batch connected with `-d VPL` and confirmed database name `VPL`, completed/cancelled statuses 4/5, two posted payments totaling 450,000, balance 9, lot available 9, exactly one posted Service issue costing 100,000, one pending reminder, and one Service-sourced maintenance event.
- Verification: Service Web tests 5/5, Service Domain tests 5/5, Service workflow/model EF tests 3/3, Release Web build 0 warnings/0 errors, browser Service list/details/consolidated report/machine history all rendered expected values. The only browser error retained in history is the expected SignalR disconnect during the intentional local server restart.
- Deployment boundary: Production was not accessed or redeployed in this UAT run. The currently deployed production release predates this antiforgery fix. Next action is a Web-only production hotfix deployment after explicit approval; no migration, DbMigrator, or production business-data write is required.

### 2026-08-25 - Production UAT Preparation And WebSocket Fix

- Agent: Codex
- Nginx: Diagnosed the SignalR failure as `Upgrade` being forwarded while `Connection` was fixed to `keep-alive`. Backed up `/etc/nginx/sites-available/vpurelux` to `/etc/nginx/sites-available/vpurelux.pre-websocket-20260825-134138`, changed the proxy header to `Connection "upgrade"`, added `proxy_read_timeout 300s`, passed `nginx -t`, and reloaded Nginx without stopping Web.
- Verification: Authenticated production browser UAT reopened Service and reported no WebSocket/transport or other console error. Nginx and `vpurelux-web` remained active; final health returned 200 in 29 ms. No application release, migration, or database write was required for this operations fix.
- Read-only UAT discovery: Production currently has 0 customer machines, 0 machine positions, 0 open replacement reminders, and 0 Service works. It has 14 machine-enabled Products and 32 enabled Component replacement policies. No Customer code/name contains TEST/UAT/DEMO. Existing machine Sales belong to real named customers and were intentionally not reused or backfilled.
- Safety boundary: Do not fabricate an external machine, labor catalog item, or completed inventory/financial transaction under a real customer. Full production UAT must wait for the user to approve a dedicated UAT customer/machine and exact Material/Labor values, or identify a real operator-owned service case.

### 2026-08-25 - Service Production Deployment Complete

- Agent: Codex
- Authorization/target: The user explicitly authorized production deployment and supplied the SSH credential for `root@180.93.99.150`. Runtime was proven before any write as local SQL Server `127.0.0.1,1433`, database `VPureLux`; active Web was `/opt/vpurelux/releases/web-20260824-165930`.
- Backup: Created `/var/opt/mssql/data/VPureLux-pre-service-20260825-105316.bak` with `COPY_ONLY` and `CHECKSUM`. `RESTORE VERIFYONLY WITH CHECKSUM` reported the backup set valid. File size is 35 MiB and SHA-256 is `4b0220dfb78bbe5b92cda5478bd88af0ed9a5e90a76cfd9e4db18b12b629c5ae`.
- Migration/reconciliation: DbMigrator applied only `20260824113235_AddServiceModule` and completed host seed successfully. Before/after core counts are identical: Customers 37, Sales Orders 31, Sales lines 171, Inventory transactions 249, Inventory transaction lines 1195, Inventory lots 303, and BOM items 701. New Service Orders/lines/works/payments are all 0. Thirteen Service/report permission grants exist. No business backfill or data correction ran.
- Source/artifacts: Service source commit `bcc1b36` and report hotfix commit `be4a5ad` are pushed to `main`. Both Web and DbMigrator were published from detached clean worktrees with no machine-local `appsettings.Production.json`; required certificate/static libraries were included. Local/VPS SHA-256 matched before extraction.
- Deployment: Initial schema release was `/opt/vpurelux/releases/web-20260825-110141`. Authenticated production UAT then exposed an ABP minifier scope bug in `BusinessRevenue.js` (`ReferenceError: i is not defined`). Root cause was a function declaration hoisted outside the minifier-created block. It was changed to a lexical arrow expression, covered by a Web regression assertion, committed as `be4a5ad`, and redeployed. Final active release is `/opt/vpurelux/releases/web-20260825-111401`; immediate rollback is `/opt/vpurelux/releases/web-20260825-110141`; the earlier Warranty release remains `/opt/vpurelux/releases/web-20260824-165930`.
- Verification: Pre-deploy Service regression passed Domain 5/5, EF 61/61, and Web 4/4. The hotfix passed `node --check`, rebuilt Web tests 4/4, and Release Web build with 0 warnings/errors. Three post-release health checks returned 200 in about 40-44 ms; root, login, Service JS, report JS, and Font Awesome font returned 200. Final one-minute calm-period check found 0 ERR/FTL, 0 HTTP 500, and health 200 in 30 ms.
- Authenticated production UAT: Service list/menu and create action render without page overflow. Service revenue loads its table and three summary values. Consolidated revenue loads its table and current values (revenue 133,448,000; paid 67,579,000; remaining 65,869,000). Customer-machine and stable Sales lists load without HTTP 500. The report `ReferenceError` is absent after hotfix.
- Residual operations note: Browser console still reports the pre-existing SignalR WebSocket transport failure through Nginx; normal pages and reports work and SignalR can fall back, but Nginx WebSocket proxy configuration should be reviewed separately before relying on realtime Chat. It is not a Service schema/data failure.
- Next action: An operator should create one controlled production Service order for an existing customer machine with one Material and one Labor line, then confirm/complete/pay it and verify FIFO quantity, Service ledger, maintenance history, next replacement reminder, and both Service/consolidated reports. Do not fabricate this business transaction as an automated deployment smoke test.

### 2026-08-24 - Service Module Source Complete And UAT Passed

- Agent: Codex
- Product/UX delivered: One primary `Dich vu` workflow uses the existing customer machine (`CustomerAsset`) instead of creating a duplicate customer-product module. A service order contains Material and Labor lines, while the small work catalog is opened from the Service context. Customer-machine details can start a preselected service order. Simple daily actions use full pages or ABP modals; there are no browser prompts or top-100 lookup shortcuts.
- Business behavior: Draft orders can be confirmed, worked, completed, or cancelled through guarded transitions. Completion posts only actually performed Material quantities through FIFO `ServiceIssue`; Labor never touches stock. The same transaction snapshots revenue/cost, appends machine maintenance history, closes the replaced position's old reminder, and starts the next cycle from the actual completion date using the current enabled policy. Posted payments are a separate ledger; pre-completion receipts are advances, voiding is idempotent, and only completed orders enter Service/consolidated revenue.
- Architecture/query behavior: Added ABP Domain, Application Contracts, Application, EF Core, permissions, localization, Razor Pages, server-side DataTables, remote Select2 lookups, reports, and focused tests. Completion batch-loads warehouses, stock items, FIFO lots, component policies, machine positions, and reminders; there is no repository query inside a line loop. Existing Sales tables, Sales payment ledger, Sales confirmation, and Sales report procedures were not changed.
- Schema/data boundary: Migration `20260824113235_AddServiceModule` creates only `AppServiceOrders`, `AppServiceOrderLines`, `AppServiceWorks`, and `AppServicePayments` with their keys, indexes, and foreign keys. It contains no business DML/backfill and alters no existing table. DbMigrator applied it successfully to test database `VPL` only. Production database `VPureLux` was not migrated, written, or deployed for this module.
- Verification: Service Domain passed 5/5; Service EF Release passed 61/61; Service Web passed 4/4; broader Release runs passed Domain 97/97, EF 119/119, and Application 30/30. Full solution Release build passed with 0 errors and two pre-existing warnings. JavaScript syntax, localization JSON, migration/model inspection, generated idempotent SQL, and `git diff --check` passed.
- Authenticated visual UAT: Against local Web connected explicitly to `VPL`, desktop and 390x844 mobile layouts passed. The Service list and Create/Edit line grids use contained horizontal scrolling without page overflow; remote Select2 controls render once without LeptonX wrappers; the Service report shows only Service revenue, paid, and remaining cards and omits the redundant source column. Browser logs had no new module error; historical SignalR 1006 entries correspond exactly to intentional local server restarts.
- Configuration warning: `VPureLux.Web/appsettings.Development.json` and `VPureLux.DbMigrator/appsettings.json` currently point to different SQL hosts while both database names are `VPL`. For local UAT, set `ConnectionStrings__Default` explicitly from the DbMigrator configuration before starting Web. Never infer safety from the database name alone; prove both host and catalog before any migration.
- Deployment state: Production rollout was intentionally not executed because this request authorized implementation on the test database, not a production migration/deploy. The next agent must obtain explicit approval, follow the deployment runbook, back up and reconcile production, and preserve the current rollback release.

### 2026-08-24 - S-001 Through S-005 Implemented; S-006 Verification In Progress

- Agent: Codex
- Product/UX: Reused the existing `CustomerAsset` as the single customer-machine record. Operators get one primary `Dich vu` list and one order flow; Material/Labor lines share the document, while the small work catalog is reached from that context rather than becoming another top-level product/menu. No duplicate customer-product aggregate or machine screen was added.
- Architecture: Added the ABP-layered Service aggregate, owned lines, work catalog, separate payment ledger, repositories/read models, server-side DataTables, ABP action modals, permissions/localization, `ServiceIssue` FIFO posting, replacement-history integration, and separate Service plus consolidated Sales/Service reports. Stable Sales tables and confirmation behavior were not changed.
- Data: Migration `20260824113235_AddServiceModule` is schema-only: four Service tables plus keys/indexes, with no DML/backfill and no alteration to existing tables. It was applied successfully by DbMigrator to test database `VPL` only. Production database `VPureLux` was not accessed or changed.
- Design decision: The historical task wording mentioned a separate feature flag and number-sequence table. The implementation keeps rollout permission-gated and reuses the established `IBusinessCodeGenerator` for `SVC` numbers, avoiding redundant operator/configuration surfaces while preserving authorization and unique-number safeguards.
- Verification so far: Service Domain 5/5; Service EF model/permission 2/2; full Service workflow 1/1 including advance exclusion from revenue, payment void, FIFO issue, current-policy schedule restart, consolidated report inclusion, and atomic rollback when stock is short; Service Web source tests 4/4. Broader Release runs passed Domain 97/97, EF 119/119, Application 30/30, and full solution build with 0 errors (only pre-existing warnings). JavaScript syntax, localization JSON, migration inspection, and idempotent SQL generation passed.
- Remaining: Rebuild/reseed after the final admin-permission correction, run desktop/mobile browser UAT against local `VPL`, inspect the final diff, then commit/push. Do not migrate or deploy production without an explicit user request.

### 2026-08-24 - Warranty Gate Accepted; Service Work Authorized

- Agent: Codex
- User decision: The user explicitly accepted moving from the completed Warranty/CustomerCare work into the Service and customer-product area. W-008 and W-GATE are therefore closed, and S-001 is claimed.
- UX direction: Keep the operator flow minimal. Reuse the existing CustomerAsset as `May khach hang`; do not add a duplicate customer-product aggregate or menu. Simple flags/settings stay in existing Create/Edit forms. Service uses one primary server-side list and a focused order workflow; advanced fields appear only when required by the current action.
- Skill update: The installed `vpurelux-engineering` skill now requires shortest-safe-workflow design, progressive disclosure, no separate screen for simple companion settings, and proof before adding any menu/page/step. The skill validator and repository preflight passed after the update.
- Safety boundary: Development may use `VPL` only. No production migration, data mutation, or deployment is authorized yet.

### 2026-08-24 - Current Warranty Snapshot Released

- Agent: Codex
- Release marker: Annotated Git tag `release-2026-08-24-warranty-notifications` identifies the accepted source snapshot. It includes the deployed Warranty notification implementation and all handoff records through this entry; it does not claim that the future Service module is implemented.
- Documentation: Added the approved business and technical Service/CustomerCare design DOCX files under `docs/service-care-design/` so a new agent can review the same design inputs referenced by `AGENTS.md` and the VPureLux engineering skill.
- Credential hygiene: Added `.codex/` and `**/appsettings.Production.json` to `.gitignore`. The local Codex system-skill cache and machine-specific production configuration remain outside Git and outside the release.
- Verification: All apparent BOM/Inventory modifications were timestamp-only false positives; their working-tree hashes exactly matched the index and no code delta was committed. `dotnet build VPureLux.slnx -c Release --no-restore` passed with 0 errors and four pre-existing warnings, including the known `Scriban 7.2.1` advisory in the ConsoleTestApp dependency graph.
- Runtime boundary: This source release adds no application-code, schema, migration, or business-data change beyond the already deployed Warranty notification commit `817e6fd`. Production remains on `/opt/vpurelux/releases/web-20260824-165930`; no redeploy or DbMigrator run is required for the documentation-only snapshot commit.

### 2026-08-24 - W-008 Warranty Notifications Deployed (Operator UAT Pending)

- Agent: Codex
- Behavior delivered: Authenticated users with `VPureLux.Warranty.View` now receive an ABP toolbar bell. It shows the total actionable replacement reminders, separates `Qua han` and `Canh bao`, caps the badge at `99+`, refreshes every 60 seconds while the page is visible, and deep-links to the Warranty server-side DataTable with the corresponding timing filter selected. Empty state and `Xem tat ca canh bao` are included. No browser popup, duplicate notification table, or per-user notification receipt was added.
- Query/architecture: The toolbar calls the Warranty application contract through a ViewComponent. The EF read repository calculates Warning and Overdue counts in one `AsNoTracking` grouped SQL aggregate over pending reminders whose `WarningDate` has arrived. The existing Warranty authorization protects both rendering and the Razor handler. No N+1 query and no database schema change were introduced.
- Verification: Warranty + Sales EF integration passed 46/46; Warranty Web source/UI regression passed 9/9; Release Web build passed with 0 warnings/errors in the working tree; JavaScript syntax, Vietnamese localization JSON, `git diff --check`, and EF pending-model check passed. Local authenticated browser inspection against test DB `VPL` confirmed the bell/dropdown, empty state, `timingStatus=3` deep-link selecting `Qua han`, responsive layout without overlap, and zero console warnings/errors.
- Test-runner caveat: The combined Web Warranty/Sales run and the isolated `SalesPagesTests` runner were stopped because `testhost` continuously consumed CPU and grew beyond 7 GB/3 GB RAM without returning results. This is recorded as a Web test-runner resource leak; focused Warranty Web and the integrated Sales/Warranty EF coverage completed successfully.
- Commit/deployment: Code commit `817e6fd` was pushed to `main`. It was published from a detached clean worktree and deployed as `/opt/vpurelux/releases/web-20260824-165930`; rollback is `/opt/vpurelux/releases/web-20260824-154709`. The local and VPS archive SHA-256 matched: `F415B92581043B2F7E173F45C2CABB874D39A40CA75A6B9D378FFE4530D98901`.
- Database/data boundary: No migration, DbMigrator, SQL write, backfill, or business-data edit ran for this notification deployment. EF reports no model drift. Local visual verification used test DB `VPL`; production was accessed only for runtime/read-only proof and normal application startup/smoke. Production runtime remains database `VPureLux`.
- Production smoke: Service is active. Health, root, login, notification JavaScript/CSS, and Font Awesome solid font returned HTTP 200. Three warm health checks returned 200 in 24-44 ms. The deployed commit marker is `817e6fd`, the 60-second poll script is present, and journal contains zero errors since restart.
- Required next action: Hard-refresh an authenticated production session. With one controlled material policy/reminder, verify badge counts and both Warning/Overdue links. Keep W-008/W-GATE open until the user accepts the complete installation/reminder/notification workflow; do not start Service before that acceptance.

### 2026-08-24 - W-008 Replacement-Cycle List Filtered And Redeployed

- Agent: Codex
- Reported defect/root cause: `Warranty/Policies` sent a nullable `IsEnabled` filter and exposed an `All` option. The existing SQL `LEFT JOIN` therefore returned every active Component, including Components without a replacement policy or with a disabled policy.
- Fix: The Policies PageModel now always sets `IsEnabled = true` before calling `WarrantyAppService`; the status selector and its client payload were removed. Search, Count, sorting, Skip, and Take remain server-side, and the repository applies the enabled predicate before paging.
- Regression evidence: Warranty Web tests passed 8/8, Warranty EF tests passed 9/9, and the focused enabled-policy paging test passed 1/1. Release Web build passed with zero warnings/errors, `node --check` and `git diff --check` passed, and no migration/model file changed.
- Commit/deployment: Code commit `300cf6e` was pushed to `main`. It was published from a detached clean worktree and deployed as `/opt/vpurelux/releases/web-20260824-154709`; rollback is `/opt/vpurelux/releases/web-20260824-151442`. The local and VPS archive SHA-256 values matched before extraction.
- Database/data boundary: No DbMigrator or SQL write command ran. Runtime database was proven as `VPureLux`. A production read-only query found 149 active Components, 0 enabled replacement policies, and 149 disabled/unconfigured Components, so the corrected screen is expected to be empty until operators enable materials from Catalog.
- Production smoke: Service is active. Health, root, login, Policies JavaScript, and Font Awesome solid font returned HTTP 200; three warm health checks returned 200 in 20-39 ms. The deployed script has no client `isEnabled` override, and journal/application logs contain zero errors after deployment. Browser navigation reached login but had no authenticated session, so authenticated DataTable UAT is intentionally still open.
- Required next action: Hard-refresh, enable one controlled material in `Danh muc -> Vat tu`, then verify that only that material appears in `Chu ky thay the`. Keep W-008/W-GATE open until the user accepts this and the installation/reminder workflow.

### 2026-08-24 - W-008 Catalog Configuration UX Redeployed

- Agent: Codex
- Commit/deployment: Commit `c6e7839` was pushed to `main`. Production release is `/opt/vpurelux/releases/web-20260824-151442`; rollback release is `/opt/vpurelux/releases/web-20260824-143228`.
- Artifact isolation: Published from a detached clean worktree at exactly `c6e7839`, then copied only the required local `openiddict.pfx` and generated `wwwroot/libs`. Pre-existing uncommitted BOM/Inventory files and local `appsettings.Production.json` were not included. The archive SHA-256 matched on the VPS before extraction.
- Database boundary: Runtime environment was proven as `VPureLux` before switching the symlink. No DbMigrator or SQL write command ran, because EF reports no model change and this correction uses existing companion tables. A read-only post-deploy snapshot shows Customers 37, Sales Orders 31, Sales lines 171, Inventory transactions 249, Inventory lines 1195, lots 303, BOM items 707, and machine settings/policies/assets/reminders all 0. The core counts differ from older evidence while users are actively testing; this deployment did not capture a same-turn pre-count and makes no claim that those user changes came from deployment.
- Production smoke: Service is active. Root, login, replacement-policy JS, and Font Awesome solid font return HTTP 200. Three warm health checks returned 200 in 27-30 ms. Journal and hourly application log contain zero errors since deployment. RAM has about 7.2 GiB available and disk has 17 GiB free. Browser DOM shows the expected home/menu icon glyphs and console has no warnings/errors; screenshot capture timed out twice, so no screenshot evidence is claimed.
- Verification carried forward: Release build passed; Catalog EF 13/13, integrated Catalog/Warranty/Sales EF 58/58, Catalog/Warranty Web 28/28, final configuration/join tests 2/2, JS syntax, diff check, and EF pending-model check all passed.
- Required operator UAT: In `Danh mục -> Sản phẩm`, open Create/Edit and verify `Là máy cần quản lý lắp đặt` is saved without another screen. In `Danh mục -> Vật tư`, open Create/Edit, enable `Theo dõi bảo hành/thay thế`, verify cycle/warning/note appear and save. Confirm a controlled new machine sale, then use `Bảo hành -> Máy chờ lắp đặt -> Xác nhận lắp đặt` and verify first reminders start only after installation. Keep Service locked until explicit acceptance.

### 2026-08-24 - W-008 Catalog Configuration UX Corrected (Deployment Pending)

- Agent: Codex
- User feedback: Product-machine was only configurable through a redundant Warranty screen/row action, while Component replacement configuration was not discoverable in the normal material workflow.
- UX correction: Product create/edit now contains the `IsMachine` checkbox directly. Component create/edit now contains `TrackedForReplacement`; cycle, warning lead, and note appear only when tracking is checked. Both the ABP create/edit modals used by the list and the full-page image-management fallbacks contain the same controls.
- Navigation: Removed the separate `Warranty -> Machines` and `Warranty -> Policies` menu entries and removed their Catalog row actions. Status columns remain on the Product/Component DataTables for quick scanning. `Warranty -> Pending Installations` remains because installation confirmation is a real operational transition and the only start event for first schedules.
- Architecture/data: Core Product and Component tables remain unchanged. Catalog AppServices write the existing `AppProductMachineSettings` and `AppComponentReplacementPolicies` companion tables in the same Unit of Work. Optional nested configuration DTOs prevent a Catalog edit by a user without Warranty configuration permission from disabling existing settings. Catalog lists use database `LEFT JOIN` projections with Count/Skip/Take; no per-row configuration query remains in PageModels.
- Verification: Release Web build passed. Catalog EF tests passed 13/13, focused integrated Catalog/Warranty/Sales EF regression passed 58/58, focused Catalog/Warranty Web tests passed 28/28, and the two new configuration/join tests passed after their final assertions. Three touched JavaScript files pass `node --check`; `git diff --check` passes; EF reports no pending model changes and no migration file changed. Local health returned HTTP 200 against the test `VPL` configuration; no production data was touched during development verification.
- Database impact: No migration, no production SQL, and no production business-data change. Local app startup wrote only its normal health/localization operational state to test database `VPL`.
- Next action: Commit/push this isolated change, publish from a clean commit worktree so unrelated dirty BOM/Inventory files are excluded, deploy a new immutable production Web release without running DbMigrator, then verify health/static assets/logs and retain the current release for rollback.

### 2026-08-24 - W-008 Catalog/Installation Workflows Exposed And Redeployed

- Agent: Codex
- Reported defect: The user could not find Product-is-machine, Component replacement tracking, or installation confirmation after deployment.
- Root cause: Backend/pages existed, but the admin permission seed granted only Warranty View, ManagePolicies, and ManageReminders. It omitted ManageMachines, ManageInstallations, ManageAssets, and ManageSyncFailures, so ABP `RequirePermissions` hid the corresponding menu items. Product and Component configuration also existed only as separate Warranty lists instead of being visible in the primary Catalog lists the user expected.
- Code fix: Added all missing admin seed grants. Catalog Products now shows an `IsMachine` column and opens `MachineSettingModal` from its row actions. Catalog Components now shows replacement-tracking status plus cycle/warning lead and opens `PolicyModal` from its row actions. Existing `Warranty -> Pending Installations -> Confirm Installation` remains the controlled start event that creates first reminders only after installation.
- Query behavior: Product and Component page handlers pass the current server-paged IDs to dedicated batch application methods. Each companion setting/policy set is loaded with one filtered SQL query; there is no repository call inside the row projection or loop.
- Verification: Release Web build passed with only two pre-existing OpenIddict nullable warnings. Focused EF Warranty + Sales tests passed 29/29; focused Web Warranty + Catalog permission tests passed 9/9. Both changed JavaScript files pass `node --check`; localization JSON parses; `git diff --check` passes.
- Commit/deployment: Code commit `6eefa04` was pushed to `main`. Production release is `/opt/vpurelux/releases/web-20260824-143228`; rollback release is `/opt/vpurelux/releases/web-20260824-135947`.
- Permission/data impact: DbMigrator was run idempotently from its published working directory. Admin permission grants increased from 216 to 220 and all eight Warranty grants now exist. No schema migration was added. Customers 37, Sales Orders 30, Sales lines 170, Inventory transactions 247, Inventory lines 1193, lots 302, and BOM items 709 remain unchanged. Machine settings/assets/positions/reminders/sync failures remain zero.
- Production smoke: Service is active and still points to `VPureLux`. Health and Product/Component/PendingInstallation JS return HTTP 200. Three sequential health checks completed in 32-41 ms; after one worker period, logs contain no new error.
- Operator flow: In Catalog Products, use the row action to check `IsMachine`. In Catalog Components, use the row action to enable replacement tracking and set cycle + warning days. Confirm a new Sales order after the configured go-live boundary; after intake, open `Warranty -> Pending Installations`, choose `Confirm Installation`, review actual component positions, and submit. Only then are the installation event and eligible reminders created.
- Required next action: User logs out/in or hard-refreshes once, performs the operator flow above with controlled production data, and reports acceptance or defects. Keep W-008/W-GATE open until this UAT is accepted; do not begin Service yet.

### 2026-08-24 - W-008 Production Deployment Complete (Authenticated UAT Pending)

- Agent: Codex
- Authorization change: The user explicitly requested deployment to the existing production service after local regression, superseding the initially isolated Web-test rollout. The temporary test Web service was stopped and fully removed before completion.
- Production backup: `/var/opt/mssql/data/VPureLux-pre-customercare-20260824-135807.bak`; created with `COPY_ONLY` and checksum and passed `RESTORE VERIFYONLY`.
- Production migration: `VPureLux` advanced from 17 migrations (`20260822181709_AddWarrantyReplacementModule`) to 18 (`20260824050543_AddCustomerCareFoundation`). Only the reviewed schema-only CustomerCare migration was applied.
- Data reconciliation: Before/after counts are unchanged: Customers 37, Sales Orders 30, Sales Order lines 170, Inventory transactions 247, Inventory transaction lines 1193, Inventory lots 302, and BOM items 709. New machine settings, assets, positions, reminders, maintenance events, and sync failures all remain zero after the intake worker ran; no historical Sales backfill occurred.
- Production release: `/opt/vpurelux/releases/web-20260824-135947`; active symlink `/opt/vpurelux/app`. Rollback release retained at `/opt/vpurelux/releases/web-20260823-013141`. Deployed application code corresponds to commit `9bf129a`; later commits only update handoff documentation.
- Runtime: `vpurelux-web` is active on port 5000 behind Nginx port 80 and still targets database `VPureLux`. CustomerCare and Sales intake gates are enabled with a deployment-time go-live boundary, so only newly confirmed Sales qualify for automatic intake.
- Smoke evidence: Health, root page, login page, Warranty JS, and the Font Awesome solid webfont return HTTP 200. Browser visual inspection confirmed menu/home/login icons render and reported no console warnings/errors. Three sequential post-start health probes returned 200 in approximately 40-47 ms with no new errors.
- Health-note: One probe returned transient 503/`TaskCanceledException` while three health requests overlapped during startup; two concurrent requests returned 200 at the same instant. Sequential checks after warm-up were consistently healthy, so this was recorded as startup probe contention rather than a deployment/schema failure.
- Test cleanup: Removed `vpurelux-web-test`, its Nginx site, environment file, port 8080 listener, `/opt/vpurelux-test`, and the uploaded temporary archive. Production service remained healthy throughout cleanup.
- Required next action: Run authenticated production UAT using a controlled new order: configure one machine product and replacement policy, confirm Sales, verify pending installation intake, confirm installation, exercise reminder lifecycle/history, and confirm one stable existing Sales approval path. Obtain user acceptance before marking W-008/W-GATE DONE or starting Service.

### 2026-08-24 - W-008 VPL Migration Rehearsal Complete (Web Deployment Awaiting SSH)

- Agent: Codex
- Database boundary confirmed by user: `VPL` is test; `VPureLux` is production. Every mutating SQL command was connected explicitly to `VPL`; `VPureLux` received read-only verification only.
- Pre-migration state: `VPL` had 12 migrations ending at `20260706173514_AddSalesOrderPayments`, 72 MB data + 8 MB log, and zero rows in Customers, Sales Orders/lines, Inventory transactions/lines/lots, and BOM items.
- Backup: A `COPY_ONLY` backup with checksum was created and passed `RESTORE VERIFYONLY` at `/var/opt/mssql/data/VPL-pre-warranty-20260824-134046.bak`. SQL Express rejected the first compression attempt before creating a file; the verified retry intentionally omitted compression.
- Migration: Applied six pending migrations through EF directly, ending at `20260824050543_AddCustomerCareFoundation`. The migrations include Sales report procedures, BOM line number, Suppliers/Inventory Lot Suppliers, Operating Cost, Warranty Replacement, and CustomerCare foundation.
- Migration incident: EF's generated idempotent script could not parse the stored-procedure migration because `CREATE OR ALTER PROCEDURE` was wrapped inside an `IF` batch. SQL transaction/history checks proved that attempt made no changes. Running `dotnet ef database update` sent the procedure commands as valid standalone batches and completed all six migrations.
- Post-migration reconciliation: `VPL` has 18 migrations; both Sales report procedures exist; `AppBomItems.LineNo` exists; new CustomerCare tables exist and contain zero rows. All captured core business table counts remained zero.
- Production proof: `VPureLux` still has 17 migrations ending at `20260822181709_AddWarrantyReplacementModule`; it was not migrated or written.
- Server/deployment changed: SQL schema on test `VPL` only. Web artifact has not been uploaded or started because no SSH password/key is available in this workspace and the old instructions require manual password entry.
- Required next action: Obtain an explicit SSH/WinSCP credential for `root@180.93.99.150`, then create an isolated test release/service/env/port pointing only to `VPL`, deploy commit `31f0e45`, and complete health/login/icon/Sales/CustomerCare/log smoke tests. Do not start Service work or close W-GATE before user acceptance.

### 2026-08-24 - W-008 Local Regression And Publish Rehearsal (Deployment Pending)

- Agent: Codex
- Result: Local regression, EF model/migration inspection, Release Web/DbMigrator publish, static asset verification, and operations/module documentation are complete. Deployment remains pending by data-safety design.
- Verification performed: Domain 93/93 passed; Application 30/30 passed; EF Core 175/175 passed including Sales/BOM/Inventory/reports and new CustomerCare flows; focused Warranty Web 6/6 passed. Full Web additionally exposed one pre-existing `DynamicRowDropdownRowsTests` source assertion and then stopped producing output, so the hung process was terminated. The failing source file was not changed in this wave.
- Migration evidence: EF reports no pending model changes. Idempotent SQL for `20260824050543_AddCustomerCareFoundation` has no business INSERT/UPDATE/DELETE/MERGE, drop, or rename; only the standard `__EFMigrationsHistory` insert appears.
- Publish evidence: Release Web and DbMigrator artifacts exist under ignored `artifacts/warranty-wave`; all Warranty page JS files, `openiddict.pfx`, and Font Awesome webfont assets are present.
- Database changed: No migration execution or data mutation. A read-only SQL inventory found `VPL` at `20260706173514_AddSalesOrderPayments` and `VPureLux` at `20260822181709_AddWarrantyReplacementModule`; neither is proven to be the approved test database, so both were left untouched. All integration persistence used SQLite in-memory.
- Server/deployment changed: No. SSH BatchMode failed and no credential environment variable/key is available. The active server/database was not touched.
- Required next action: Obtain SSH access and explicit confirmation of which database is test (or create an explicitly named `VPureLux_Test`), plus a separate env file/service/port. Then migrate and deploy the test instance with both CustomerCare gates disabled, smoke-test it, and request user acceptance before marking W-008/W-GATE done.

### 2026-08-24 - W-007 Reminder Lifecycle And Machine History

- Agent: Codex
- Result: Added SQL-derived NotDue/Warning/Overdue/Closed timing, ABP modal actions for complete/skip/reschedule/suspend with required reason and idempotency, append-only events, transactional next-cycle creation, machine suspension, and server-paged history on each machine detail page.
- Transition rule: Direct reminder completion remains available only as the documented Warranty-only transition before Service is enabled; the modal tells operators that Service orders will become the source once that module is active.
- Verification performed: Lifecycle integration covers policy snapshot preservation, one next reminder, action replay, overdue filtering, suspend cancellation, and history isolation between two machines. Six Warranty Web tests passed and no browser prompt/alert/confirm remains under Warranty.
- Database changed: No migration and no active database access.
- Server/deployment changed: No.
- Existing user worktree changes touched: No.
- Next action: W-008 is claimed; run broad regression, migration/publish rehearsal, then commit/push and deploy only after proving an isolated test database target.

### 2026-08-24 - W-006 External Customer Machines And Positions

- Agent: Codex
- Result: Added server-side customer-machine list, external-machine create/edit/detail workflows, nine default core positions with arbitrary add/remove support, nullable internal Component mapping, nullable confirmed baseline, and serial duplicate warning without automatic merge or transfer.
- Scheduling rules: External onboarding and edits batch-load Components/policies. Initial reminders are created only for mapped positions with enabled policy and explicit baseline; unmapped/missing-baseline positions remain recorded without fabricated dates. Existing open reminders block mapping/baseline removal to preserve history.
- Verification performed: Integration test covers nine positions, only three mapped/baselined schedules, no fabricated baseline for six positions, replay, database-paged lookup/next due, duplicate serial warning, and updating cores 1-3 without changing cores 8-9. Permission tests and Web build passed. SQLite in-memory only.
- Database changed: No migration and no active database access.
- Server/deployment changed: No.
- Existing user worktree changes touched: No.
- Next action: W-007 is claimed; complete reminder lifecycle and per-machine history with ABP modal UI.

### 2026-08-24 - W-005 Installation Confirmation And First Schedules

- Agent: Codex
- Result: Added a database-paged Pending Installations list and full-page technician workflow for serial, address, installation time, sold-BOM review, position omission/remapping, and additional real positions. Confirmation persists the asset, actual positions, one installation event, and eligible first reminders atomically with concurrency and hashed idempotency keys.
- Scheduling rules: A reminder requires a sold-company asset, a Product still marked as machine, confirmed installation, mapped active Component, and enabled replacement policy. DueDate and WarningDate snapshot the policy from the actual InstalledAt; disabled or unmapped positions create no reminder.
- Verification performed: Focused integration covers no reminder before installation, machine gate, changed current BOM not altering the sold snapshot, omitted/additional positions, policy filtering, date snapshots, event creation, and replay without duplicates. Warranty schema/permission tests and 4 Warranty Web tests passed; all 52 SalesWorkflow + InventoryWorkflow regression tests passed. SQLite in-memory only.
- Database changed: No migration and no active database access.
- Server/deployment changed: No.
- Existing user worktree changes touched: No.
- Next action: W-006 is claimed; implement external customer machines and explicit position baselines without fabricating dates.

### 2026-08-24 - W-004 Idempotent Sales Intake

- Agent: Codex
- Result: Added an ABP periodic worker, distributed batch lock, explicit runtime/go-live gates, bounded SQL machine/anti-join intake, one transaction per Sales line, deterministic source line + unit idempotency, BOM position snapshots, and isolated retryable sync failures.
- UI behavior: Added server-side sync-failure DataTable with dedicated permission, explicit ABP confirmation, antiforgery token, and retry scheduling.
- Verification performed: 6 CustomerCare Domain tests passed; 4 focused intake/permission EF tests passed; 3 Warranty Web tests passed. Cases cover non-machine exclusion, quantity-two unit creation, replay, one invalid line not blocking another, failure list/search/retry, and no Sales dependency. SQLite in-memory only.
- Query behavior: Candidate headers use ProductMachineSetting join plus asset/failure anti-joins; all BOM rows for the bounded candidate set are loaded in one filtered include query, with no per-line repository query.
- Database changed: No new migration and no active database access.
- Server/deployment changed: No.
- Existing user worktree changes touched: No.
- Next action: W-005 is claimed; confirm installation and create first position-based reminders from actual InstalledAt only.

### 2026-08-24 - W-003 Machine And Replacement Policy Configuration

- Agent: Codex
- Result: Added a Product-machine companion configuration API and database-paged left-join list, ManageMachines permission/menu, and ABP modal workflows for Product-machine and Component replacement-policy configuration including warning lead time.
- UI behavior: Both configuration lists use ABP server-side DataTables, ModalManager, typed DTO validation, permission guards, and reload after save. No browser prompt/alert/confirm remains in either configuration workflow.
- Verification performed: Web Debug build passed; 5 CustomerCare schema/query/permission tests passed; 2 Warranty Web modal/DataTable tests passed. SQLite in-memory only.
- Database changed: No additional migration and no data backfill.
- Server/deployment changed: No.
- Existing user worktree changes touched: No.
- Known deferred item: Reminder reschedule still contains the legacy browser prompt and is assigned to W-007 with the reminder lifecycle work.
- Next action: W-004 is claimed; implement gated, idempotent background Sales intake and isolated failure handling.

### 2026-08-24 - W-002 CustomerCare Schema Foundation

- Agent: Codex
- Result: Added machine settings, sold/external asset source and installation fields, actual asset positions, append-only maintenance events, position-based reminder fields, isolated sync failures, concurrency tokens, and approved unique/check/index guards.
- Migration: `20260824050543_AddCustomerCareFoundation`; `Up` has zero business DML, drop, rename, or automatic backfill operations and changes no core table. Existing Active assets without `InstalledAt` are interpreted as PendingReview.
- Verification performed: 5 Domain tests passed; 2 EF schema/persistence tests passed on SQLite in-memory; focused Sales boundary remained green; EF reported no pending model changes; offline SQL script generation succeeded; DbMigrator Release build succeeded with only two pre-existing nullable warnings.
- Database changed: No. Migration was generated and scripted with a fake localhost code-generation connection string; DbMigrator/database update was not run.
- Server/deployment changed: No.
- Existing user worktree changes touched: No.
- Next action: W-003 is claimed; implement server-side machine configuration and ABP modal policy editing without browser popups.

### 2026-08-24 - W-001 Sales Protection And CustomerCare Gates

- Agent: Codex
- Result: Removed the synchronous Warranty dependency and side effect from Sales confirmation; added disabled-by-default CustomerCare/Sales-intake options and an explicit no-backfill operations note.
- Files changed: `src/VPureLux.Application/Sales/SalesOrderAppService.cs`, deleted `src/VPureLux.Application/Warranty/WarrantySalesIntegrationService.cs`, added `src/VPureLux.Domain.Shared/CustomerCare/CustomerCareOptions.cs`, `src/VPureLux.Application/VPureLuxApplicationModule.cs`, `src/VPureLux.Web/appsettings.json`, `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Sales/SalesWorkflowTests.cs`, and `docs/CUSTOMERCARE_OPERATIONS.md`.
- Verification performed: 2 focused boundary tests passed; all 19 `SalesWorkflowTests` passed; all 30 `InventoryWorkflowTests` passed. All test persistence used SQLite `Data Source=:memory:`.
- Database changed: No migration and no data mutation.
- Server/deployment changed: No.
- Existing user worktree changes touched: No.
- Next action: W-002 is claimed; add and verify the schema-only CustomerCare foundation without touching stable core tables or running against the active database.

### 2026-08-24 - Initial Product Backlog Baseline

- Agent: Codex
- Result: Created the ordered Warranty-first and Service-second product backlog.
- Verification performed: Read current Warranty Domain/Application/EF/Web code, migration, current Sales integration, current Warranty test coverage, design documents, skill, and repository preflight.
- Code changed: Documentation/control files only (`AGENT_TASKS.md`, `AGENTS.md`).
- Database changed: No.
- Server/deployment changed: No.
- Existing user worktree changes touched: No.
- Next action: Claim W-001, update the Active Work Record, then protect Sales from synchronous Warranty integration with focused regression tests.
