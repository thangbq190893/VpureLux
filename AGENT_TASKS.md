# VPureLux Product Task List And Agent Handoff

This file is the single source of truth for implementation order and agent handoff.
Every agent must read and update this file so another agent can continue without a chat summary.

Last updated: 2026-08-24 (Asia/Saigon)
Current product stage: Warranty/CustomerCare completion
Current active task: W-008
Next task: W-008 authenticated production UAT and user acceptance
Service implementation gate: CLOSED until W-GATE is DONE

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
| W-008 | Warranty regression, UAT, migration rehearsal, and rollout | IN_PROGRESS | W-001..W-007 |
| W-GATE | Warranty/CustomerCare acceptance gate | PENDING | W-008 |
| S-001 | Service module foundation and work catalog | HOLD | W-GATE |
| S-002 | Service order aggregate, lines, permissions, and UI | HOLD | S-001 |
| S-003 | Service completion, FIFO issue, and schedule integration | HOLD | S-002 |
| S-004 | Service payments and receivables | HOLD | S-003 |
| S-005 | Service and consolidated reports | HOLD | S-003, S-004 |
| S-006 | Service UAT, reconciliation, and rollout | HOLD | S-001..S-005 |

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

Status: IN_PROGRESS

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

Status: PENDING

This gate may be marked DONE only when:

- W-001 through W-008 are DONE.
- The user accepts the Warranty/CustomerCare behavior.
- Sales, Inventory, BOM, and existing reports remain regression-green.
- Migration/data reconciliation and deployment smoke evidence are recorded.
- No unresolved Severity 1 or Severity 2 Warranty defect remains.

When W-GATE becomes DONE, change S-001 from HOLD to READY. Do not start any Service task earlier.

## 6. Service Tasks - Locked Until W-GATE

### S-001 - Service Module Foundation And Work Catalog

Status: HOLD

- Add separate Service bounded module, permissions, menus, feature flag, number sequence, and non-inventory work/labor catalog.
- Do not add service/labor products to Sales or Catalog Product.
- Use schema-only migration and ABP modal/server-side DataTable UI.

### S-002 - Service Order Aggregate, Lines, Permissions, And UI

Status: HOLD

- Add ServiceOrder with Draft -> Confirmed -> InProgress -> Completed/Cancelled state machine.
- Support mutually exclusive Material and Labor lines.
- Bind one customer machine per order in phase one.
- Snapshot prices/cost assumptions and use full-page workflow plus server-side list.

### S-003 - Service Completion, FIFO Issue, And Schedule Integration

Status: HOLD

- Add InventoryTransactionType.ServiceIssue without changing existing enum values.
- Batch-load FIFO lots for all material lines; no N+1.
- Atomically issue actual materials, snapshot cost/revenue, append maintenance events, close old reminders, and create new cycles.
- Only performed Material lines touch stock/schedules; Labor never touches stock.

### S-004 - Service Payments And Receivables

Status: HOLD

- Add separate Service payment ledger with Posted/Void/idempotency behavior.
- Treat payment before completion as customer advance, not service revenue.
- Keep SalesOrderPayments unchanged.

### S-005 - Service And Consolidated Reports

Status: HOLD

- Add Service revenue/profit reports and consolidated Sales + Service read model with source dimension.
- Keep existing Sales stored procedures unchanged.
- Reconcile completed Service only and exclude advances from revenue.

### S-006 - Service UAT, Reconciliation, And Rollout

Status: HOLD

- Test external machine Core 1-3 replacement plus Labor, FIFO, missing stock rollback, concurrency, payment, reminders, and consolidated reports.
- Publish/deploy only with explicit approval and full regression/smoke evidence.

## 7. Active Work Record

Task ID: W-008
Agent/task name: Codex - Warranty regression, migration rehearsal, publish, and rollout
Started at (Asia/Saigon): 2026-08-24
Branch and starting commit: main / aec9f91
Goal for this run: Prove the Warranty/CustomerCare wave is regression-safe, inspect migration/publish outputs, update operations/module documentation, commit/push, and deploy only to a proven isolated test database target.
Files expected to change: regression/UAT evidence, module and operations documentation, this handoff file, and generated Release publish artifacts outside source control.
Database/data impact: Test database `VPL` was backed up and migrated from 12 to 18 migrations. After an explicit production deployment instruction, production database `VPureLux` was backed up and migrated from 17 to 18 migrations with the schema-only CustomerCare foundation. Captured business row counts remained unchanged.
Verification planned: full focused Domain/EF/Web suites, Sales/BOM/Inventory/report regression, no pending EF model changes, schema-only migration SQL review, Release publish/static assets, runtime target proof, commit/push, isolated DB migration and smoke tests.
Current blocker: No technical deployment blocker. Production deployment and unauthenticated visual/static/health smoke tests are complete. Authenticated Sales/CustomerCare UAT and explicit user acceptance remain required before W-008 and W-GATE can be marked DONE.

## 8. Handoff Log

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
