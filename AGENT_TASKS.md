# VPureLux Product Task List And Agent Handoff

This file is the single source of truth for implementation order and agent handoff.
Every agent must read and update this file so another agent can continue without a chat summary.

Last updated: 2026-09-08 (Asia/Saigon)
Current product stage: Service PRODUCTION ROLLOUT COMPLETE; Sales Post-Confirmation V1 remains RELEASED / ACCEPTED
Current active task: None
Next task: Resolve the focused Web test-host result for SALES-ADJUSTMENT-FIX-001, then review and accept it; keep the separate Sales GrossPosted/NetPaid reporting defect as a new scoped task
Service implementation gate: W-GATE DONE; SERVICE-INVENTORY-AUDIT DONE; S-001/S-002/S-003/S-004/S-005/S-006 DONE
Service foundation milestone source: `babc96fc5ecba242e3f23d0612c3a46df3916dc3`; originally completed locally, with its accepted implementation included in production release `b0bf197e8525acb2f254995af70b3da8a397a9f8`; see `docs/S001_SERVICE_FOUNDATION.md`.
Service order workflow milestone source: `2f27ed81618403d7375b2af237025e6e931bbe3f`; originally completed locally, with its accepted implementation included in production release `b0bf197e8525acb2f254995af70b3da8a397a9f8`; see `docs/S002_SERVICE_ORDER_WORKFLOW.md`.
Service completion milestone source: `3413fc9a56f05e812bd4c19102b70ff69157836e`, including main implementation `18e9f02a279709ca01018f06933aec12de64cf12` plus the final UTC+07 calendar/history correction; originally completed locally, with its accepted implementation included in production release `b0bf197e8525acb2f254995af70b3da8a397a9f8`; see `docs/S003_SERVICE_COMPLETION.md`.
Service payment/settlement milestone source: `2a93dfb847201fe87749d6dc7d4b267db16fad4c`; originally completed locally, with its accepted implementation included in production release `b0bf197e8525acb2f254995af70b3da8a397a9f8`; see `docs/S004_SERVICE_PAYMENTS.md`.
Service reporting milestone source: `e40aed2e9b8e1072e3958d9a47ae52d780fd0592`; originally completed locally, with its accepted implementation included in production release `b0bf197e8525acb2f254995af70b3da8a397a9f8`; see `docs/S005_SERVICE_REPORTS.md`.
Service production release: `b0bf197e8525acb2f254995af70b3da8a397a9f8`, tag `release-2026-09-07-service-v1`, deployed at `/opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197`; rollback `/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa`. See `docs/SERVICE_V1_PRODUCTION_ROLLOUT_20260907.md`.

Accepted Warranty implementation baseline:

- W-008 source/test/evidence commit: `d1e8b5684d21eca3ee5586fe75913b60e24de190` (`fix(warranty): finalize accepted customer care safeguards`). Baseline SEALED on 2026-09-07, local only; not pushed or deployed.
- Separate Service audit documentation commit: `6ef1def1812c6b25ed1ffd6caaff3eaa46c1a709`. This is not the W-008 implementation commit.
- W-008 and W-GATE remain accepted historical baselines. S-001 through S-006 and SERVICE-V1-ROLLOUT are DONE. Service V1 is production RELEASED / ACCEPTED; its deployed source, release tag, and active release path are recorded above. Sales V1 remains the immediate rollback baseline, not the active production release. This does not imply that the W-008 baseline commit was independently deployed.

Sales V1 release source:

- Production code commit: `a4717aa361e931aaa2bb09fd55d20d0efd9599c2`.
- Annotated release tag: `release-2026-09-03-sales-v1`, pushed and verified on `origin`.
- Production release: `/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa` (rollout 2026-09-03).
- Migration rehearsal, deployment, and legacy reconciliation: PASS; 14/14 fingerprints match, with no production business-data mutation from migration.
- CancelAndClone, a non-machine terminal modification lock, and the Web testhost memory leak are separate future tasks, not unfinished work in this accepted release. The combined Web suite is still not claimed as passed.

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
- Configuration/templates suggest or initialize new business records only. Persisted business facts are independent historical snapshots; later configuration changes must not rewrite, recalculate, resync, or backfill them. Changes require an explicit permitted, audited business action. New cycles may copy the current policy without altering old cycles. User reaffirmed this source-of-truth rule on 2026-09-07.
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

## 3. Historical Baseline Before W-001

This is a point-in-time baseline used to derive W-001 through W-008. Its "Known incomplete or incorrect behavior" list is historical evidence and must not be interpreted as current product state; the top-level release summary, Delivery Order, Active Work Record, and latest Handoff entries define current state.

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
| SALES-V1-REHEARSAL | Sales V1 migration rehearsal and legacy-data safety gate | DONE | SALES-V1-PHASE2 |
| SALES-V1-ROLLOUT | Sales V1 production migration, deployment, smoke, and reconciliation | DONE | SALES-V1-REHEARSAL |
| SALES-V1-RELEASE | Seal accepted Sales V1 source tag and documentation | DONE | SALES-V1-ROLLOUT |
| SERVICE-INVENTORY-AUDIT | Audit existing Service implementation before reuse | DONE | W-GATE |
| S-001 | Service module foundation and work catalog | DONE | SERVICE-INVENTORY-AUDIT |
| S-002 | Service order aggregate, lines, permissions, and UI | DONE | S-001 |
| S-003 | Service completion, FIFO issue, and schedule integration | DONE | S-002 |
| S-004 | Service payments, advances, receivables and refund settlement | DONE | S-003 |
| S-005 | Service and consolidated reports | DONE | S-003, S-004 |
| S-006 | Service UAT, reconciliation, and rollout | DONE | S-001..S-005 |
| SERVICE-V1-ROLLOUT | Service V1 production rehearsal, migration, deployment, reconciliation, and release seal | DONE | S-006 |
| SALES-ADJUSTMENT-FORENSIC-001 | Read-only forensic investigation of confirmed-order adjustment effectiveness and cross-module atomicity | DONE | None |
| SALES-ADJUSTMENT-FIX-001 | Simplify confirmed-order adjustment submission and prove transactional rollback | BLOCKED | SALES-ADJUSTMENT-FORENSIC-001 |

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

Status: DONE - final blocker resolution completed on 2026-09-06. D01 uses the current enabled policy for successors without rewriting existing reminders, D02 remains an accepted documented test-harness exception, and intake-race/permission/focused UI evidence is green. Technical verification remains separate from user acceptance at W-GATE.

Current run boundary: Test/UAT writes only on database `VPL`, restricted to newly created, prefixed fixtures. `VPureLux` is production and is not an authorized write or UAT target. No deployment, migration, restart, production connection, or historical backfill is part of this run. Reuse accepted migration/rollout evidence; do not rerun it automatically. Evidence matrix: `docs/W008_WARRANTY_UAT_20260904.md`.

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

Status: DONE. User explicitly accepted the Warranty/CustomerCare workflow on 2026-09-07 (Asia/Saigon). W-008 technical evidence remains recorded separately. Accepted implementation is sealed in `d1e8b5684d21eca3ee5586fe75913b60e24de190`, not pushed or deployed as a standalone release. At W-GATE closure, production was still the Sales V1 release; Service was deployed later by the separate SERVICE-V1-ROLLOUT task, and current production state is recorded at the top of this file.

This gate may be marked DONE only when:

- W-001 through W-008 are DONE.
- The user accepts the Warranty/CustomerCare behavior.
- Sales, Inventory, BOM, and existing reports remain regression-green.
- Migration/data reconciliation and deployment smoke evidence are recorded.
- No unresolved Severity 1 or Severity 2 Warranty defect remains.

When W-GATE becomes DONE, make SERVICE-INVENTORY-AUDIT READY first. Keep S-001 on HOLD until that audit identifies the reusable implementation and remaining gaps. Do not start any Service task earlier.

## 6. Service Tasks - Audit Before Implementation

### SERVICE-INVENTORY-AUDIT - Reconcile Existing Service Implementation

Status: DONE. Promoted HOLD -> READY -> IN_PROGRESS -> DONE on 2026-09-07 after explicit W-GATE acceptance. Evidence and exact S-001..S-006 scope: `docs/SERVICE_INVENTORY_AUDIT.md`.

- After W-GATE only, inventory existing commits `bcc1b36` and `220d41c`, the Service migration, tests, and contracts against the accepted baseline.
- Document what to reuse and what remains for S-001 through S-006; do not blindly reimplement or cherry-pick the old module.
- This handoff adds no Service implementation authorization.
- Key finding at audit time: historical Service migration `20260824113235_AddServiceModule` was absent from source but applied according to accepted rollout evidence; isolated VPL evidence also preserves existing Service rows. S-001 has now recovered the exact original pair and merged its passive mappings without duplicate tables or replacing the Sales V1 snapshot. Actual deployed-schema reconciliation remains an authorized S-006 rehearsal, not a claim made by offline inspection.
- Reuse module separation, snapshot fields, current FIFO allocator, enum meanings, ABP UI and antiforgery/minifier fixes. Refactor state/version/replay, payment serialization, labor cost completeness, lookup/report queries and care integration. Discard silent remap/reactivation and blanket draft-line snapshot recreation.

### S-001 - Service Module Foundation And Work Catalog

Status: DONE. Completed 2026-09-07 on sealed W-008 baseline 83fb3f5; source `babc96fc5ecba242e3f23d0612c3a46df3916dc3`. S-002 READY, unclaimed; S-003..S-006 HOLD. No migration applied, runtime enabled on an external target, deploy or push.

- Separate Service foundation, permissions, menu, default-disabled config guard and non-inventory Work Catalog are implemented. Work codes remain manually entered as historically; no new sequence/max-string algorithm is required. Any future automatic numbering must reuse current BusinessCodeGenerator.
- Do not add service/labor products to Sales or Catalog Product.
- Use schema-only migration and ABP modal/server-side DataTable UI.
- Begin with historical Service schema compatibility, preserve the original migration ID and current Sales V1 model, then add work unit/optional standard cost without filling old historical facts from current templates. See audit section 10 for exact files, tests, dependencies and migration expectations for every Service task.

### S-002 - Service Order Aggregate, Lines, Permissions, And UI

Status: DONE. Completed 2026-09-07 on baseline `9a0967b`; implementation source `2f27ed81618403d7375b2af237025e6e931bbe3f`. S-003 is READY and unclaimed; S-004..S-006 remain HOLD. No migration was applied, runtime enabled externally, deploy or push.

- Add ServiceOrder with Draft -> Confirmed -> InProgress -> Completed/Cancelled state machine.
- Support mutually exclusive Material and Labor lines.
- Bind one customer machine per order in phase one.
- Snapshot prices/cost assumptions and use full-page workflow plus server-side list.
- Current order/line/payment types are passive schema shells only. Do not mistake them for a delivered workflow; keep completion disabled until S-003.

### S-003 - Service Completion, FIFO Issue, And Schedule Integration

Status: DONE at source `3413fc9a56f05e812bd4c19102b70ff69157836e`, including implementation `18e9f02` and final calendar/history correction. UTC instants preserve the operator's UTC+07 cycle date and new Service history display. No migration/VPL/production/deploy/push. Evidence: `docs/S003_SERVICE_COMPLETION.md`.

- Add InventoryTransactionType.ServiceIssue without changing existing enum values.
- Batch-load FIFO lots for all material lines; no N+1.
- Atomically issue actual materials, snapshot cost/revenue, append maintenance events, close old reminders, and create new cycles.
- Only performed Material lines touch stock/schedules; Labor never touches stock.

### S-004 - Service Payments And Receivables

Status: DONE. Claimed and completed 2026-09-07 on baseline 38cb145; implementation `2a93dfb847201fe87749d6dc7d4b267db16fad4c`. Local only; no push, migration application, external database access or deployment. S-005 READY, unclaimed.

- Delivered separate Service payment ledger, immutable factual partial refunds, reason/actor/time-audited void and canonical replay. Add retry after void never reposts money.
- Payment before completion is advance, not revenue. Completed obligation uses S-003 actual snapshot; cancellation preserves cash and exposes refund due without automatic void/refund.
- Shared existing ServiceOrder lock protects payment/void/refund with Cancel/Complete. Database-side order/customer projection, server-paged histories, ABP modals, permission/antiforgery and strict vi-VN binding delivered. SalesOrderPayments unchanged.
- Verification, formulas, additive migration and known combined-Web limitation: `docs/S004_SERVICE_PAYMENTS.md`. Real SQL Server/Redis race and legacy migration rehearsal remain S-006.

### S-005 - Service And Consolidated Reports

Status: DONE. Completed 2026-09-07 on baseline 3725ec7; implementation `e40aed2e9b8e1072e3958d9a47ae52d780fd0592`. Reports only; no Sales workflow fix, external database access, migration application, deploy, push or S-006.

- Delivered Completed-Service recognition and effective-line Sales composition with factual settlement, persisted historical costs, null-versus-zero labor cost, server permissions and database paging/totals.
- Existing Sales reports/stored procedures remain unchanged. Actual Sales posted-versus-net API discrepancy is documented, not patched in frozen Sales workflows.
- Verification: 329 focused tests passed; build 0 errors; offline EF drift NONE; browser desktop/mobile and minifier PASS. Full evidence/limitations: `docs/S005_SERVICE_REPORTS.md`.

### S-006 - Service UAT, Reconciliation, And Rollout

Status: DONE. Decision READY FOR SERVICE PRODUCTION ROLLOUT after explicit C01 acceptance on 2026-09-07. Claimed from0a7eb72 with explicit VPL-only authorization. Production rollout has not occurred and requires separate explicit authorization; no production access, migration, restart, deploy or push was performed.

- Test external machine Core 1-3 replacement plus Labor, FIFO, missing stock rollback, concurrency, payment, reminders, and consolidated reports.
- Publish/deploy only with explicit approval and full regression/smoke evidence.

## 7. Active Work Record

Task ID: SALES-ADJUSTMENT-FIX-001
Agent/task name: Codex - Sales adjustment UX and atomicity fix
Started at (Asia/Saigon): 2026-09-07
Branch and starting commit: codex/warranty-release-review / eeba88a
Goal: Make the current submitted adjustment authoritative through one primary action while preserving Sales V1 delta semantics and proving late Inventory/CustomerCare rollback.
Status: BLOCKED. The implementation is complete and focused EF evidence is green, but the focused Web PageModel/UI host did not emit a final result due to the known test-host leak. Do not claim FIX READY FOR REVIEW until that final result is captured.
Authorization: Local source, builds, and isolated SQLite tests only. VPL, production, deployment, migration, push, and business-data operations are forbidden.
Safety boundary: Reuse `SalesOrderOperationCoordinator`; price-only never posts Inventory; negative deltas retain Warehouse confirmation; no stale draft may be silently applied.
Protected files: `docs/VPureLux_Sales_Flow_Design_Review_for_Codex_5_6_Sol.docx` and `docs/html.txt` remain untracked and must not be staged.
Implementation: Added `SubmitRevisionAsync`, which updates from the posted form and applies in one coordinator/UoW when no Warehouse return is pending. The Adjust page exposes one primary `Xac nhan dieu chinh` action and validates the posted model before submission. A negative delta persists Draft only and communicates that the effective order is unchanged.
Verification: Release Web and EF test-project builds passed with 0 warnings and 0 errors. Captured SQLite EF evidence: 8 passed across current-value-over-stale-draft price change, Warehouse-pending negative delta, injected failure after Inventory, injected failure after CustomerCare, increase/replay, replacement, add/remove delta isolation, and factual decrease reversal. Three focused Web PageModel/UI tests compile but the known Web test-host leak did not return a final runtime summary, so they are not counted as passing evidence. `docs/SALES_ADJUSTMENT_FIX_001.md` records the exact behavior, query review, and remaining review risk.
Data/deployment: No migration, database access, VPL, production access, deploy, restart, push, or business-data mutation. Protected user files remain untracked and excluded.
Next action: Run only `Sales_Adjust_Page` focused Web tests in a stable host and capture the final count; then perform product review/acceptance of the simplified manager workflow. Keep GrossPosted/NetPaid separate; do not reopen this adjustment change merely to address reporting.

### Previous Completed Sales Adjustment Forensic Record

Task ID: SALES-ADJUSTMENT-FORENSIC-001
Agent/task name: Codex - Sales adjustment forensic investigation
Started at (Asia/Saigon): 2026-09-07
Branch and starting commit: codex/warranty-release-review / fb01661666b2d53c4a9f27fd747dd50910951a06
Goal: Trace confirmed-order adjustment from UI through persistence and prove or disprove cross-module split-brain risk. Produce `docs/SALES_ADJUSTMENT_FORENSIC_001.md`; no fix is authorized.
Status: DONE. Decision AT RISK, not a confirmed split-brain possibility. `docs/SALES_ADJUSTMENT_FORENSIC_001.md` records the exact source call chain, UoW evidence, explicit answers, test gaps, and recommended separate fix scope.
Authorization: Source, Git, and focused local/SQLite test inspection only. Production, VPL, deployment, migration, business mutations, and code fixes are forbidden.
Safety boundary: Determine whether revision-effective facts, Inventory/FIFO, CustomerCare, payment, and audit writes share one rollback boundary. Do not infer atomicity from a nominal ABP UnitOfWork without source evidence.
Protected files: `docs/VPureLux_Sales_Flow_Design_Review_for_Codex_5_6_Sol.docx` and `docs/html.txt` remain untracked and must not be staged.
Current evidence: Preflight identified only the two protected user-owned untracked files. Source trace shows the coordinator transactional boundary covers revision, Inventory/FIFO, CustomerCare, and audit-event completion. The most likely reported success is the intentionally non-effective draft Save action; no source evidence proves an Apply-success/effective-order mismatch. The focused test-host command produced no captured final result and is not claimed as passing evidence.
Next action: Do not fix from this forensic task. A separate task must add operator-sequence and post-side-effect rollback regressions before making the smallest approved UI/API change.

### Previous Completed Service V1 Rollout Record

Task ID: SERVICE-V1-ROLLOUT
Agent/task name: Codex - Service V1 production rollout
Started at (Asia/Saigon): 2026-09-07
Branch and starting commit: codex/warranty-release-review / 043f849811b0b7b6536932cd31b355051e851b82
Deployment source: b0bf197 (accepted application/publish checkpoint; later commits are documentation only)
Status: DONE. Service V1 is production released and enabled. Tag `release-2026-09-07-service-v1` points exactly to deployed application source `b0bf197e8525acb2f254995af70b3da8a397a9f8`.
Authorization: inventory and bounded cleanup of obsolete VPureLux artifacts; production read-only baseline; verified backup; production-derived rehearsal clone; the four accepted Service migrations; exact-source publish/upload/deploy; Service enablement; restart; read-only smoke/reconciliation; branch/tag push after success. No business backfill or repair, fake production data, raw migration-history repair, force push, or destructive production smoke.
Safety sequence: verify active and rollback releases/config/certificate; preserve both; prove database identity/history/schema; create and verify a fresh production backup; rehearse the exact migration path on an isolated production clone; compare business fingerprints; only then migrate production and deploy immutable source b0bf197. Stop on ambiguity, mismatch, partial migration, unexpected business mutation, or unclear rollback.
Protected files: docs/VPureLux_Sales_Flow_Design_Review_for_Codex_5_6_Sol.docx and docs/html.txt remain untracked and must not be staged.
Current evidence: branch history was pushed. Production history is24 with all four expected Service migrations exactly once; clone and production schema match. The 41/41 pre-migration fingerprint reconciliation PASSed. Active immutable release started with Service false, disabled-mode smoke PASS30/30, then Service true with three enabled-mode health probes PASS and read-only Service/Sales/Warranty/Inventory/report smoke PASS. Certificate/config and signing key remained unchanged. Final read-only reconciliation against a post-user-activity baseline PASS42/42. A concurrent user issued six audited Sales POSTs during rollout (15:41-15:43), which explain intervening Sales/BOM/Inventory fingerprint changes; no rollout code/probe mutation occurred. Temporary uploads and clone removed only after success; both fresh production backups and Sales rollback remain. Full sanitized account: docs/SERVICE_V1_PRODUCTION_ROLLOUT_20260907.md. Raw local evidence remains ignored under artifacts/service-v1-rollout.
Next action: None. Observe production. Do not start either known Sales defect from this release task.

### Previous Completed S-006 Record

Task ID: S-006
Agent/task name: Codex - Service final data-safety and readiness gate
Started at (Asia/Saigon): 2026-09-07
Branch and starting commit: codex/warranty-release-review / 0a7eb72
Status: DONE. Decision READY FOR SERVICE PRODUCTION ROLLOUT. C01 accepted as projection-only coverage with operator workflow N/A by design. No active task and no production rollout permission.
Authorization: VPL read/write, verified backup, isolated VPL-derived rehearsal databases, reviewed pending migration only after rehearsal PASS, explicitly prefixed fixture mutations, real SQL Server/Redis and local VPL runtime allowed. Production database VPureLux completely forbidden, even read-only. No production service/config/symlink/smoke/deploy or push.
Safety sequence: explicit catalog guard before opening connections; DB_NAME/@@SERVERNAME proof; read-only initial inventory; schema/history reconciliation; deterministic baseline; backup/VERIFYONLY; clone rehearsal; no legacy DML; only then VPL migration/UAT. Any schema/history mismatch stops writes, without automatic repair.
Scope: preserve S001-S005 semantics and Sales frozen baseline. Planned10/Actual12 remains projection-only coverage with workflow N/A by design; it is not permission to expand current completion quantities/prices. Both separate Sales defects remain open and untouched.
Verification/evidence: SQL 180.93.99.150, DB_NAME VPL (instance name VPureLux is not catalog). Verified COPY_ONLY/CHECKSUM backup VPL-pre-service-s006-20260907_113345.bak. Rehearsal VPL_SERVICE_REHEARSAL_20260907_113345 and empty VPL_SERVICE_EMPTY_20260907_113345 migrated to current; 4 historical tables/86 columns/18 indexes match; old material cost 1/1 matches allocation. Clone and VPL 41/41 original-table fingerprints unchanged. VPL four pending Service migrations applied via EF without seed, history 20->24. No production access. See docs/S006_SERVICE_UAT_20260907.md and artifacts/s006.
Findings: raw GO batch script failed filtered-index binding on the clone; EF separate-command execution succeeds without migration changes. Real SQL exposed COUNT_BIG(NULL) in Sales-only report summary; reproducing test added and fixed with conditional bigint sum. SQL read scopes now pass and 22.8m Sales + .45m Service = 23.25m. Both separate Sales business/API defects untouched.
Live UAT checkpoint: local evidence runtime at localhost:5196 uses VPL + real local Redis database 13, no background jobs/workers or shared admin/role modifications. New operator/role and UATSVC_20260907 fixtures recorded in ignored artifacts/s006/uat-auth.json (secrets) and fixtures.json (business IDs/evidence). Seed, Money, Core and six HTTP SQL/Redis money/terminal race groups PASS. Advance8m/actual6m/refund2m, cancel-preserves-money, retry-after-void, refund page2 and Core1-3-only cycle changes verified. Runtime-only failure/permission controls are isolated in docs/evidence/s006/Runtime, never production assembly/config. Fix source 5e7727a.
Later checkpoint: shortage and injected AFTER stock+care SaveChanges rollback match all 8 fixture-state groups; FIFO 3 lines x 2 lots exact cost; MissingBaseline actual replacement PASS. Security native HTTP deny/masked fields/page2 PASS after Service-only CSRF fix 2583dbe (Chrome missing-token POST reproduced before, now 400; 8 mutation routes protected, valid token accepted). Legacy Details incorrectly added 7h; c2663ac adds factual IsLegacyCompletion flag and leaves old wall times intact, with 12 new/legacy boundary renders and EF mapping regression PASS. No migration/data rewrite for either fix. First long Web order group OOM:12 pass/8 fail; Sales report group20 pass/1 fail under 1.5GiB; Run-WebSplit.ps1 now reruns methods independently, do not call the earlier grouped runs green. Native UI initial screenshots/no-overflow/XSS/work-create/payment1.5m PASS; BrowserUat.cjs resumes remaining Void/Complete/Refund by existing fixture references (not blind re-creation). Run-TimePolicy resumes completed time0000 by exact completion key.
Final live checkpoint: all nine live SQL/Redis race groups, 6 new VPL timestamps plus 6 synthetic legacy clone boundaries (rolled back), policy/inactive/unmapped/re-enable matrix, header/metadata snapshot immutability, two-line shortage and injected post-stock rollback PASS. Native browser money flow completed; current screenshots wait for DataTables/KPI/CSS transitions. Feature-disabled Warranty transition and enabled Skip pass. Real plans/IO captured for five queries; small-data CPU9-15ms, missing-statistics warnings documented, no tuning migration. Final VPL and clone original full/factual fingerprints PASS41/41. Yearly reports reconcile22.8m Sales+15.9m Service=38.7m. Final build0errors; EF236 rerun PASS; split Web42 plus other28 PASS, original OOMs retained. Local runtime stopped. Sanitized IDs/hashes in docs/evidence/s006/verification-evidence.json; full evidence and C01 in docs/S006_SERVICE_UAT_20260907.md.
Final artifact gate: owned evidence checkpoint b0bf197; clean tracked source published full Web5,634files and DbMigrator316files, no execution/upload. Seven Service/report scripts byte-match and parse, generated libs/fonts/certificate present, minifier regression preserved. SHA-256 and certificate handling are in docs/S006_SERVICE_UAT_20260907.md and committed verification-evidence.json. Final EF236/236; total394 accepted focused/broad tests, full Web not claimed. Runtime and required execution sessions stopped; only user-owned files remain untracked after final docs commit.
Acceptance closeout: user explicitly accepted C01 as projection-only/N/A workflow coverage. S003 remains unchanged; Actual > Planned is not enabled. No additional UAT, VPL/production access, build, test, migration, publish, deployment or push occurred during closeout.
Next action: create a separate Service production-rollout authorization/review task if requested. READY does not mean PRODUCTION RELEASED. A changed excess-charge workflow requires a separate business design. Keep the confirmed Sales adjustment-success-without-change defect and the Sales GrossPosted-as-NetPaid API naming/projection defect open as separate tasks. Do not stage user documents, push, access production or deploy without explicit authorization.

### Previous Completed S-005 Record

Task ID: S-005
Agent/task name: Codex - Service and consolidated reporting
Started at (Asia/Saigon): 2026-09-07
Branch and starting commit: codex/warranty-release-review / 3725ec7cd616ccb08503a307502161f24f1ac0eb
Goal: Completed-Service recognition and effective Sales read-model composition, historical cost completeness, database paging/totals, cross-source permissions and simple report UI.
Status: DONE - local implementation `e40aed2e9b8e1072e3958d9a47ae52d780fd0592`; no active task. S-006 READY, unclaimed.
Database/data boundary: Only offline EF tooling and disposable SQLite fixtures. No VPL/production/Redis access, migration application, DbMigrator, deploy, push or S-006.
Changed files: 25 explicitly staged implementation/test files in the source commit: Reports contracts/AppService/EF/Web, permission/localization/menu declarations, focused tests and the matching-policy authorization test fake. Separate docs update AGENT_TASKS, MODULE_MAP and S005_SERVICE_REPORTS. Reused S004 SQL money expression; no migration.
Invariants: no revenue from advance/refund; no current catalog cost refresh; null cost != zero; effective Sales lines only; immutable cash facts; server cost/profit protection and stable database paging.
Verification completed: Release build 0 errors (existing Scriban NU1903 and Web CS7022 warnings). Domain 39/39, Application 14/14, EF 236/236; split Web report/money 6/6, Sales reports 21/21, Warranty 13/13. Total 329 passed, no duplicate rerun counts. SQL Server ToQueryString translation, historical/null/zero/legacy/unperformed costs, date/page2, real Sales adjustment/refund and mixed reconciliation covered. Browser Chrome 1440x960 and 390x844 passed source/search/page2, safe text, vi-VN currency and horizontal access to financial columns; screenshots inspected, temporary SQLite-only proxy stopped. Node/NUglify PASS, offline EF drift NONE, diff check PASS. Failed earlier fixture/fake runs retained in artifacts/s005, corrected and rerun; no combined full-Web pass claimed.
Current findings: Current Sales APIs call posted receipts NetPaid without subtracting factual refunds. New report exposes factual GrossPosted/GrossRefunded/NetPaid separately, without modifying frozen Sales APIs/SPs. Current Sales recognition date convention is retained; new S003 completion instants use UTC+07 calendar conversion, legacy wall times remain unshifted. No blocker for the local S005 scope; real SQL Server query plans and legacy-schema/operator acceptance remain S006.
Separate task: User-reported Sales post-confirm adjustment defect is outside S005 and remains uninvestigated/unfixed here. The existing Sales gross-versus-net projection discrepancy also requires its own Sales scope; no workflow/API patch is authorized here.
Next action: Stop after S005. Claim S006 only on a separate instruction, beginning with explicit environment/data permissions, migration-history reconciliation and the S001-S005 docs. Do not infer external DB/deployment authorization from READY. Do not stage docs/html.txt or the user-owned Sales review DOCX.

### Previous Completed S-004 Record

Task ID: S-004
Agent/task name: Codex - Service payments and settlement
Started at (Asia/Saigon): 2026-09-07
Branch and starting commit: codex/warranty-release-review / 38cb1451b9e4acf2ee805eb66351df95e4b0098a
Goal: Service-owned payments/advances, immutable factual refunds, reasoned void, canonical money projection, server-paged histories and ABP financial modals.
Status: DONE - local implementation `2a93dfb847201fe87749d6dc7d4b267db16fad4c`; no active task. S-005 READY, unclaimed.
Database/data boundary: Offline builds/design-time EF and isolated SQLite fixtures only. No VPL/production access, migration application, DbMigrator, deploy or push. No Sales payment changes or S-005.
Intended files: Service Domain.Shared/Domain/Contracts/Application/EF/Web and focused tests; additive Service-only migration; S004 documentation and this handoff.
Invariants: shared existing VPureLux:ServiceOrder lock; posted cash is not revenue; Void is not Refund; no overpay/over-refund/negative NetPaid; replay never resurrects voided receipts; no legacy rewrite.
Verification completed: Final Release solution build 0 errors/1 existing Scriban NU1903 warning; Domain Service/Inventory 49/49; Application Service 11/11; EF Service/Sales/Inventory/Warranty/CustomerCare 226/226. Isolated Web groups: money/parser 11/11, order 9/9, completion 6/6, works/source 6/6, Warranty 13/13. Browser desktop 1440/mobile 390: payment, void, refund POST 204; both histories page 2, correct 1.38m refund remainder, no JS errors/document overflow; screenshots inspected. EF model drift NONE; diff/JS syntax PASS. Generated-only migration 20260907021302_AddServicePaymentSettlement is additive Service-only with no DML/backfill. Raw ignored evidence artifacts/s004 and artifacts/s004-browser; reproducible source tests and exact aborted-Web inventory in docs/S004_SERVICE_PAYMENTS.md.
Verification caveat: One combined Service Web run aborted under bounded 1.5 GiB testhost memory with 13 PASS/9 FAIL; exact host was stopped. Pure parser no longer starts a host; split groups pass. No combined/full Web pass claimed. SQL Server query translation verified offline, not live distributed execution. Temporary SQLite browser proxy stopped; no 5099 listener remains.
Current blocker: None. The requested Actual > Planned example is a projection/legacy test only because accepted S-003 caps actual quantities at planned; S-004 does not change completion rules. Old DOCX cancel-before-refund requirement is superseded by the user's explicit cancel-with-outstanding-refund rule.
Next action: Stop after S-004. S-005 may be claimed separately; read S003/S004 docs and reuse persisted revenue/cost facts plus canonical settlement projections. Do not call advances revenue or subtract refunds from recognized revenue; preserve unknown labor cost. No deployment or VPL/production access without new authorization. Never stage docs/html.txt or the user-owned Sales review DOCX.

### Previous Completed S-003 Record

Task ID: S-003
Agent/task name: Codex - Atomic Service completion
Started at (Asia/Saigon): 2026-09-07
Branch and starting commit: codex/warranty-release-review / bd8be2a533bb69bc7973497c7cca07bf244314a0
Goal: Atomic actual quantities, current Inventory FIFO, nullable actual cost facts, canonical replay and CustomerCare-owned replacement integration.
Status: DONE at final source `3413fc9a56f05e812bd4c19102b70ff69157836e`. Main implementation `18e9f02` (44 files) plus calendar/history follow-up (8 files) are committed. Final audit's 00:00-06:59 UTC+07 boundary case is fixed and tested without rewriting legacy timestamps. No active task; S-004 READY/unclaimed.
Database/data boundary: Offline builds and isolated SQLite tests only. No VPL/production access, migration execution, DbMigrator, deploy, push or S-004. Additive schema-only migration permitted.
Verification completed: Final Release solution build 0 errors, 2 pre-existing warnings (Scriban NU1903 and Web test entrypoint CS7022). Domain Service/Inventory 33/33; Application Service/CustomerCare 6/6; EF combined Service/Inventory/Warranty/CustomerCare/Sales 204/204; Web Service 25/25; focused Warranty Web 13/13. Includes 01:30 UTC+07 completion, local cycle/baseline date and local history display; legacy time display is not reinterpreted. No combined/full Web-suite claim. Offline EF model drift NONE; staged diff check PASS; three changed JavaScript files passed node --check.
Browser evidence: Actual ABP completion modal on the SQLite/in-memory Web fixture at 1440x960 and 390x844; mobile rows were adjusted after visual review. No page overflow or JavaScript errors; actual total recomputed 300000 -> 150000; browser submission returned 204 and reloaded Completed with actual amount and no mutation buttons. Screenshots: `artifacts/s003-completion-browser/modal-1440.png`, `modal-390.png`, `completed.png` (local ignored evidence). Temporary loopback proxy/test host stopped; port 5099 no longer listening. The test fixture's existing duplicated menu contributors were not changed in application code.
Migration/data impact: `20260906200825_AddServiceCompletionFacts`, seven nullable additive fields only, no business DML/default backfill or Sales schema change; generated but not applied. No external database/cache or production service touched. Only isolated SQLite fixtures changed during tests.
Current blocker: None for S-003 or claiming S-004. SQL Server/Redis multi-process races and legacy schema rehearsal remain S-006; SQLite barrier/stale-writer tests are not represented as live distributed proof. Unknown labor cost remains nullable and future reports must preserve provisional-versus-final cost semantics.
Next action: Claim S-004 separately if instructed. Read the S-003 document before adding payments; reuse ServiceOrder locks, preserve immutable actual facts, keep schema-only/no-backfill discipline. Do not deploy or access VPL/production without explicit task scope. User-owned DOCX and docs/html.txt remain excluded.

### Previous Completed S-002 Record

Task ID: S-002
Agent/task name: Codex - Service order operator workflow
Started at (Asia/Saigon): 2026-09-07
Branch and starting commit: codex/warranty-release-review / 9a0967b486c1ebd9414a04bcbf8a55dcca46ebe5
Goal for this run: Deliver Draft/Confirmed/InProgress/Cancelled Service orders with identity-preserving Material/Labor lines, optimistic concurrency, database-paged lookups/list, ABP UI and no S-003 side effects.
Status: DONE. S-002 COMPLETE at `2f27ed81618403d7375b2af237025e6e931bbe3f`. No active task; S-003 READY and unclaimed.
Database/data boundary: No VPL/production connection, migration application, DbMigrator, deploy, restart, push, FIFO issue, reminder/event mutation, revenue recognition or payment workflow. Two user-owned files remain excluded.
Verification completed: Release solution build 0 errors/4 pre-existing warnings (two OpenIddict nullable warnings, Scriban NU1903 and Web test entrypoint CS7022). Domain Service 14/14; Application Service 6/6; EF Service 72/72 (S-002 workflow 7/7); EF Sales/Inventory/Warranty/CustomerCare regression 110/110; Web Service 18/18; Warranty Web 13/13. JavaScript syntax passed for all three S-002 scripts; offline EF model drift NONE; diff check PASS. HTTP tests cover valid vi-VN decimal submit, safe encoding, permissions, all mutation antiforgery routes and stale-cancel error rendering. Browser listener could not be started from the test entrypoint, so no S-002 screenshot/browser claim is made; the attempted local test process was stopped.
Current blocker: None for S-002 or claiming S-003. Migration `20260906185742_AddServiceOrderWorkflow` only adds nullable CancellationReason and nullable StandardCostSnapshot, has no DML, and was not applied. SQL Server legacy rehearsal remains S-006. Production and VPL remain untouched.

### Previous Completed S-001 Record

Task ID: S-001
Agent/task name: Codex - Service foundation and historical schema compatibility
Started at (Asia/Saigon): 2026-09-07
Branch and starting commit: codex/warranty-release-review / 83fb3f50232113a03a4af2a8f85cc74a87b7cac3
Goal for this run: Restore original Service migration identity, passive schema mapping, default-disabled runtime and Work Catalog only.
Status: DONE. S-001 COMPLETE at `babc96fc5ecba242e3f23d0612c3a46df3916dc3`. No active task; S-002 READY and unclaimed.
Database/data boundary: No VPL/production connection, no migration application, DbMigrator, deploy, restart or push. No order/payment/FIFO/reminder/report workflow. Two user-owned files remain excluded.
Verification completed: Release solution build 0 errors/2 pre-existing warnings; local publish passed. Domain Service/Sales/Inventory 38/38; Application Work contracts 4/4; full EF 215/215 (including 7 Service foundation/application/runtime/model/snapshot/harness tests); focused Service Web 5/5 and Warranty Web 13/13 after the test-connection correction. No full Web suite claim. Original migration pair matches historical Git blobs exactly; forward migration only adds nullable Unit/StandardCost to AppServiceWorks; model drift NONE. Offline empty/legacy/current/idempotent SQL checks pass; all prior Sales V1/CustomerCare/Warranty/Inventory entity metadata is preserved. Browser review exercised modal create/save, invariant decimal input, unknown versus zero cost and real page 2 on desktop/mobile; local test host was stopped. Exact scope (41 implementation/test/migration files) and evidence are in docs/S001_SERVICE_FOUNDATION.md.
Current blocker: None for S-001 or starting S-002. SQL Server schema/data/distributed-lock rehearsal remains S-006 and is not authorized now. Two initial EF groups failed an existing shared-connection SQLite initialization race; the test-only connection lifecycle was corrected without changing Sales assertions/business code, and full EF then passed 215/215. Original failed TRX evidence is retained. External Google Fonts could not load in browser review; fallback text/local icons were visible. Production remains unchanged.

### Previous Completed W-008 Baseline Seal Record

Task ID: W-008-BASELINE-SEAL
Agent/task name: Codex - seal accepted Warranty implementation
Started at (Asia/Saigon): 2026-09-07
Branch and starting commit: codex/warranty-release-review / 6ef1def1812c6b25ed1ffd6caaff3eaa46c1a709
Goal for this run: Commit only accepted W-008 implementation/tests/evidence without changing business behavior or starting S-001.
Status: DONE. W-008 BASELINE SEALED at d1e8b5684d21eca3ee5586fe75913b60e24de190. No active task; S-001 READY, not claimed.
Database/data boundary: No connection to VPL or production, no UAT harness execution, no migration generation/application, deploy, restart, or push. Focused tests use SQLite in-memory; model comparison used an unreachable dummy connection override.
Verification completed: Release solution build (--no-restore -m:2) passed with 0 errors and 4 warnings (Scriban NU1903, two OpenIddict CS8604, Web test entrypoint CS7022). Release --no-build focused tests: Domain Warranty/CustomerCare 6/6; EF Warranty/CustomerCare/Sales 74/74; Web Warranty 13/13, no failures/skips. Web completed in 19 seconds with a 1.5 GiB GC heap cap and 90-second hang bound; no full/combined Web suite claim. EF has no pending model changes; git diff --check passed. All 19 accepted file hashes were unchanged through validation; five harness scripts parsed, three evidence JSON files parsed, and six local evidence artifact hashes matched the manifest. TRX evidence: artifacts/w008-baseline-seal-20260907/{domain,ef,web}.trx (ignored/local).
Current blocker: None for sealing or starting S-001. Historical Service schema compatibility remains a required S-001 scope item, not permission to migrate now. Two user-owned files remain untracked and excluded; no Service code/migration was staged.

### Previous Completed Service Audit Record (2026-09-07)

Task ID: SERVICE-INVENTORY-AUDIT
Agent/task name: Codex - source audit of historical Service implementation
Started at (Asia/Saigon): 2026-09-07
Branch and starting commit: codex/warranty-release-review / 5c5d1de
Goal for this run: Inspect bcc1b36 and 220d41c against HEAD plus accepted W-008 working-tree fixes; record reuse, defects, migration impact, and the exact S-001..S-006 plan.
Status: DONE. SERVICE AUDIT COMPLETE. W-GATE DONE by explicit user acceptance; S-001 READY and not started; no active task.
Database/data boundary: Source/documentation only. No DB connection/mutation, migration generation/application, production deployment, or Service implementation.
Verification completed: Read-only preflight; actual Domain/Contracts/Application/EF/migration/UI/payment/report/test inspection at bcc1b36 and 220d41c, comparison to current HEAD plus W-008 fixes. Identified 13 historical test methods (not rerun); preserved historical D02 40/41 and R2 41/41 evidence. No build/test/runtime or DB operation was needed for this documentation audit. Audit defines all ten required assessments and the S-001..S-006 plan.
Current blocker at audit completion: None for S-001 foundation. Historical Service schema must be reconciled before future migration application. W-008 application/test changes were still uncommitted at that point; the later baseline-seal record above supplies their source commit. The audit documentation commit is not a production deployment.

### Previous Completed Warranty Record (2026-09-06)

Task ID: W-008
Agent/task name: Codex - Warranty regression and VPL UAT on frozen Sales V1
Started at (Asia/Saigon): 2026-09-04
Branch and starting commit: codex/warranty-release-review / 5c5d1de
Goal for this run: Audit inherited evidence, verify missing Sales/CustomerCare and external-machine cases, reconcile legacy VPL records, and report technical readiness separately from operator acceptance.
Status: DONE. READY FOR USER ACCEPTANCE = YES. W-GATE remains PENDING for the user's separate acceptance decision.
Database/data boundary: VPL only for newly prefixed UAT records; VPureLux production is not touched. No production deploy/migration/restart. Background intake must not process historical records; prove target and go-live before starting a runtime.
Verification completed: Build 0 errors; Domain 102/102, Application 30/30, EF 208/208, focused Warranty Web 13/13, Sales/Inventory API Web 6/6. D01 proves existing reminder 3/15 remains immutable while Complete on 2026-09-04 uses current 6/21 for a successor due 2027-03-04; disabled/deleted policy and inactive/unmapped position complete without successor; replay/re-enable neither duplicate nor backfill. Intake revalidates authoritative Sales state inside the shared order lock; deterministic selected-before-cancel/replacement barriers skip stale assets, while normal/retry creates once. All seven Warranty permissions have server-side deny/allow execution, four Razor mutation endpoints reject missing antiforgery tokens, and server paging/filter/sort reaches page 2. Real VPL R2 evidence remains 41/41; no new VPL fixture/write occurred in the close-out. Final model check has no changes. No migration, production access, deployment, restart, commit, or push.
Current blocker: None in W-008. W-GATE remains PENDING solely for user/operator acceptance. W008-D02 preserves the original 40/41 run as an accepted test-harness exception and uses isolated R2 41/41 as official data-safety evidence; no metadata was repaired or hidden. Generic SALES_015/016 race text remains a deferred low-severity UX issue with correct 403/state behavior.

### Previous Completed Release Record

Task ID: SALES-V1-RELEASE
Agent/task name: Codex - seal accepted Sales V1 release source
Started at (Asia/Saigon): 2026-09-04
Branch and starting commit: codex/warranty-release-review / 5df9fa8
Goal for this run: Annotate and push the exact production code commit, then record release acceptance in documentation only.
Status: DONE. Sales Post-Confirmation V1 is RELEASED / ACCEPTED. Annotated tag `release-2026-09-03-sales-v1` was created and pushed; its local and remote peeled target is `a4717aa361e931aaa2bb09fd55d20d0efd9599c2`.
Database/data boundary: No production access, database access, migration, deployment, restart, or symlink change is authorized for this task.
Verification completed: Read-only local preflight passed; no tracked changes existed; the two user-owned untracked files remain excluded. Target commit exists; tag was absent locally and remotely before creation. `git show`, `git rev-list`, `git cat-file`, and remote peeled-ref checks confirm the annotated tag and exact target. Both release documents are updated; diff check passed. Application code and business specifications are unchanged. No server or database connection, deployment, migration, restart, or symlink action was performed.
Current blocker: None.

### Previous Completed Rollout Record

Task ID: SALES-V1-ROLLOUT
Agent/task name: Codex - deploy accepted Sales V1 to production with rollback gates
Started at (Asia/Saigon): 2026-09-03
Branch and starting commit: codex/warranty-release-review / a4717aa (`docs(sales): record final migration rehearsal`)
Goal for this run: Push the accepted Sales V1 commits, protect `VPureLux` with a verified backup, apply only `20260826051356_AddSalesPreInstallationV1Foundation`, reconcile legacy data and reports, deploy an immutable Web release, and complete production smoke without mutating legacy business records.
Status: DONE. Decision `PRODUCTION ROLLOUT COMPLETE`. Source, backup, migration, reconciliation, immutable Web deployment, health, authenticated read-only Sales smoke, reports, and post-deploy data gates all passed.
Database/data boundary: Production schema migration and standard ABP system seeding are explicitly authorized; no custom business DML, backfill, legacy-order mutation, FIFO rebuild, or production cleanup is permitted.
Verification completed: Branch pushed without force at `a4717aa`; detached artifacts matched local/VPS SHA-256. Production backup `/var/opt/mssql/data/VPureLux-pre-sales-v1-20260903-180246.bak` passed VERIFYONLY. DbMigrator applied only `20260826051356_AddSalesPreInstallationV1Foundation`. All 14 legacy fingerprints, sampled orders, Revenue, and Profit matched before/after/final. New process tables stayed empty; no production smoke records were created. Release `/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa` is active, rollback `/opt/vpurelux/releases/web-20260825-111401` is retained, three sequential health probes were Healthy, authenticated Sales/list/detail/action/queue/report checks passed, and no HTTP 500 or error-level journal entry was observed.
Current blocker: None. Production had no Draft orders or Installed assets for non-destructive live coverage; those scenarios remain covered by accepted rehearsal evidence. W-GATE remains open and Service remains on HOLD.

## 8. Handoff Log

### 2026-09-08 - SALES-ADJUSTMENT-FIX-001 Validation Checkpoint

- Decision: `BLOCKED` only on focused Web test-host evidence. The three new `Sales_Adjust_Page` PageModel/UI tests compile, and the focused host begins discovery/execution but exits without a final result, matching the existing Web test-host leak. A policy-restricted attempt to capture it from a background runner was rejected before execution. No Web test is claimed as passed.
- Captured evidence remains green: Release EF and Web test-project builds have 0 warnings/errors; 8 focused SQLite EF tests passed, including current-form authority, Warehouse wait, positive delta/retry, replacement, add/remove isolation, factual decrease reversal, and both late rollback injection points.
- Code remains a local checkpoint only. No migration, VPL/production access, deployment, restart, push, or protected-file staging occurred. Resume only the focused Web evidence; do not redesign Sales or touch the separate GrossPosted/NetPaid defect.

### 2026-09-07 - SALES-ADJUSTMENT-FIX-001 Initial Implementation Checkpoint (Superseded)

- Initial implementation decision: `FIX READY FOR REVIEW` pending final verification. The 2026-09-08 validation checkpoint supersedes this status because focused Web test-host output was not captured.
- Negative Inventory deltas still require Warehouse return confirmation. In that case the revision remains Draft and the effective Sales order is not changed; the page communicates this explicitly.
- Verification: Release Web build and Release EF test-project build passed with 0 warnings/errors. Captured SQLite EF evidence passed 8 tests: current values override a stale draft without Inventory posting, negative delta waits for Warehouse, late failures after Inventory and CustomerCare roll back revision/effective order/inventory/customer assets, quantity-increase replay, replacement, add/remove isolation, and factual decrease reversal. Three focused Web PageModel/UI tests compiled, but their known leaking host did not return a final runtime result and are not claimed as passed.
- No migration, VPL/production access, deployment, restart, push, or database/business-data mutation occurred. `docs/SALES_ADJUSTMENT_FIX_001.md` is the technical/operator handoff. The known Sales GrossPosted/NetPaid reporting discrepancy remains a separate unresolved task.

### 2026-09-07 - SALES-ADJUSTMENT-FORENSIC-001 Complete

- Decision: `AT RISK`, not a confirmed split-brain possibility. Source-level evidence shows `SalesOrderOperationCoordinator` owns one new transactional UoW for revision, effective Sales lines, Inventory/FIFO, CustomerCare reconciliation, and local business-audit completion. No production/VPL/server/data access or code change occurred.
- Most likely operator-visible cause is the two-step UI: `Save` persists a Draft revision and explicitly says to review before Apply; only `Apply` changes the effective order. `OnPostApplyAsync` intentionally ignores unsaved form fields, so it applies only the last saved revision.
- Existing normal/shortage regressions cover final Sales/Inventory outcomes, but no browser operator-sequence test or injected late Inventory/CustomerCare/audit failure proves the full failure boundary. The bounded focused SQLite host did not return a captured final summary and is not claimed as passing evidence.
- Next action: review `docs/SALES_ADJUSTMENT_FORENSIC_001.md`, then authorize a separate narrowly scoped fix task. Keep Sales `GrossPosted`/`NetPaid` discrepancy separate and open.

### 2026-09-07 - Service V1 Production Release Accepted

- Production rollout was reviewed and ACCEPTED. Application source `b0bf197e8525acb2f254995af70b3da8a397a9f8`, tag `release-2026-09-07-service-v1`; Service is enabled in production. The authoritative rollout evidence is `docs/SERVICE_V1_PRODUCTION_ROLLOUT_20260907.md`.
- This docs-only closeout made no implementation or database change. Service implementation and release work are CLOSED; Current active task remains None. The two known Sales defects remain separate and open: confirmed adjustment may report success without changing the effective order, and an existing API labels GrossPosted as `NetPaid` without deducting factual refunds.

### 2026-09-07 - S-006 C01 Accepted And Readiness Gate Closed

- Decision READY FOR SERVICE PRODUCTION ROLLOUT; S-006 DONE, active task None. C01 is ACCEPTED - NOT A SERVICE DEFECT.
- Planned10m / actual12m operator workflow is N/A by design because accepted S003 caps performed quantity at planned quantity and preserves price snapshots. The independent monetary projection12m -5m =7m receivable remains PASS. S003 and application code are unchanged; Actual > Planned was not enabled.
- All technical/VPL/migration/SQL/Redis/UAT/regression/publish evidence from the preceding S006 handoff remains authoritative. This docs-only closeout did not rerun UAT, access VPL or production, run SQL/Redis/DbMigrator, change/apply a migration, build, publish, deploy or push.
- Production rollout has NOT occurred and is NOT authorized by this acceptance. A separate explicit rollout review/authorization is the only next Service task.
- Both Sales defects remain open and separate: confirmed adjustment may report success without changing the order; an existing Sales API labels GrossPosted as `NetPaid` without subtracting factual refunds. No Sales code changed.

### 2026-09-07 - S-006 Technical Gate Executed; C01 Acceptance Pending

- Decision CONDITIONAL, S-006 BLOCKED on acceptance C01 only; active task None. Baseline0a7eb72, fixes5e7727a/2583dbe/c2663ac, owned harness/evidence checkpointb0bf197, final docs in this commit. No automatic rollout or next module.
- Authorized SQL endpoint180.93.99.150, catalogVPL; actual instance nameVPureLux must not be confused with forbidden production catalog. Verified backup, clone/empty chain and four VPL migrations20->24 PASS, no business DML/backfill. Final original full/factual fingerprints41/41 unchanged on VPL and clone. New prefixed fixtures retained; no cleanup/restore.
- Real SQL/Redis nine race groups, FIFO/rollback/Core1..9/care/policy/money, native HTTP permissions/antiforgery, browser desktop/mobile, six new and six synthetic legacy timestamp boundaries, snapshot/report reconciliation passed. Total394 accepted tests; bounded split Web only, initial OOM results retained. Final EF driftNONE. Query plans warn missing statistics on small data; no speculative index or tuning mutation.
- Clean tracked b0bf197 published full Web/DbMigrator, local archives verified and hashed, no upload/start. Local UAT runtime stopped. Final Service revenue15.9m+Sales22.8m=38.7m. Exact IDs, fingerprints, artifact hashes and steps: docs/S006_SERVICE_UAT_20260907.md and docs/evidence/s006/verification-evidence.json.
- C01: requested planned10m->actual12m completion conflicts with accepted S003 cap/price snapshots. Domain receivable formula12-5=7 passes; no operator bypass added. User must accept projection-only coverage or request separate excess-charge design. Recommend the former. Do not mark DONE/READY silently, rerun fixture mutations, or deploy just to resolve this documentation/acceptance condition.
- Production catalog/service/config/symlink untouched, no production migration/deploy, no push, both separate Sales defects untouched, both user documents untracked/excluded.

### 2026-09-07 - S-005 Service and consolidated reports sealed

- Decision: S-005 COMPLETE. Baseline `3725ec7cd616ccb08503a307502161f24f1ac0eb`; implementation `e40aed2e9b8e1072e3958d9a47ae52d780fd0592` (`feat(service): add service and consolidated reports`, 25 files). Separate documentation commit; no amend/squash/push. S-006 READY, unclaimed, active task None.
- Recognition is Completed-Service actual revenue plus confirmed effective Sales lines. Advances/refunds do not add/rewrite revenue. Historical actual FIFO/nullable labor costs remain snapshots; unknown costs make profit null. Sales 10,000 + Service 3,000 reconciles to 13,000 through real workflow fixtures.
- Database-side Count/filter/sort/Skip/Take/UNION ALL/totals; Service and cross-source cost/profit permissions enforced in API responses and sorting. Two ABP report pages add source/date/search filters and safe text rendering without replacing existing Sales reports.
- Final gates: Release build 0 errors; Domain 39, Application 14, EF 236, split Web 40 passed (329 total). Browser desktop/mobile and real NUglify passed. No EF drift, migration, DML, backfill, external DB/Redis access, runtime enablement, deploy or push. SQLite-only browser proxy stopped. User-owned documents remain untracked/unstaged.
- Current Sales API gross-versus-net discrepancy and the separately reported Sales adjustment defect remain separate future work; neither Sales workflow was patched. S006 must explicitly authorize its external targets and verify live schema/locking/performance/permissions/operator acceptance. See `docs/S005_SERVICE_REPORTS.md` for exact semantics and commands.

### 2026-09-07 - S-004 Service payments and settlement sealed

- Decision: S-004 COMPLETE. Baseline `38cb1451b9e4acf2ee805eb66351df95e4b0098a`; implementation `2a93dfb847201fe87749d6dc7d4b267db16fad4c` (`feat(service): add payments and settlement`, 37 files). Documentation is committed separately, without amend/squash/push. S-005 READY and unclaimed, S-006 HOLD, no active task.
- Service-owned posted payments/advances, immutable factual refunds, auditable reasoned void, invariant command hashes, exact replay after void, caps and canonical DB-side order/customer monetary projection are delivered. No Sales payment/report change. Revenue stays zero before completion; completion uses S-003 actual snapshot independently of settlement.
- Existing ServiceOrder coordinator is reused with Cancel/Complete. Deterministic payment/payment, payment/cancel, payment/complete, refund/refund and void/refund tests pass, including both authoritative orderings where relevant and rollback after business-audit insertion. Cancel with advance preserves receipt and creates refund due without waiting for accounting.
- Fixed during verification: tracked principal stamp mutation by using no-tracking header reads; obsolete passive payment test assertion; SQL Server nested aggregate translation and nullable LEFT JOIN materialization. Audit failure injection is scoped explicitly to its test host. No completion/FIFO/care engine redesign.
- Final build 0 errors, Domain 49/49, Application 11/11, EF regression 226/226. Separate Web money/parser 11/11, order 9/9, completion 6/6, works/source 6/6, Warranty 13/13. Browser actions/page2/mobile/desktop PASS. Earlier combined Web OOM produced 13 passed/9 failed and was stopped; do not claim a full Web pass. See S004 document for exact cases and reproduction.
- Migration `20260907021302_AddServicePaymentSettlement` generated only: eight nullable ServicePayment columns, new ServiceRefund table, five indexes; no DML/backfill. EF drift NONE. No SQL Server/Redis/VPL/production access, DbMigrator, migration application, deployment, push or S-005 implementation. Only disposable SQLite fixtures were used; browser proxy stopped. Both user-owned documents remain untracked/unstaged.
- Next agent: follow `docs/S004_SERVICE_PAYMENTS.md` and the active record above. S-006 still owns live SQL Server/Redis concurrency, legacy schema rehearsal/reconciliation and operator acceptance; local completion is not production approval.

### 2026-09-07 - Final S-003 calendar audit sealed

- Final source is `3413fc9a56f05e812bd4c19102b70ff69157836e` (`fix(service): preserve local replacement calendar dates`), following the main feature commit `18e9f02` and interim docs `49e8cde`. No history was amended, squashed or pushed.
- Corrected a final-audit boundary: 01:30 on September 7 in UTC+07 is September 6 18:30 UTC, but the replacement baseline/cycle must use September 7. Events/idempotency retain the UTC instant; only new Service event presentation converts to UTC+07. Existing historical local-wall-time rows are untouched.
- Final evidence supersedes earlier counts: EF 204/204, Web Service 25/25, Warranty Web 13/13; Domain 33/33, Application 6/6; Release build 0 errors; offline drift NONE. An intermediate build collided with running testhost DLL locks; it was rerun successfully after tests ended, with no code workaround.
- S-003 DONE again after the explicit correction/regression gate; S-004 READY and unclaimed. All external data/deployment restrictions remain unchanged.

### 2026-09-07 - S-003 atomic completion complete

- Implementation `18e9f02a279709ca01018f06933aec12de64cf12` (`feat(service): add atomic service completion`) follows `bd8be2a533bb69bc7973497c7cca07bf244314a0`. Source changes are in 44 explicitly staged files; no user-owned documents, Service payments, push or deployment were included.
- Only InProgress completes; exact command hashes enable safe replay and reject conflicting payloads. Actual quantities, FIFO allocations, nullable cost snapshots, machine/replacement events, old reminder closure, current-policy successors and terminal state share one transaction.
- Service order and CustomerAsset locks span transaction completion; current Inventory optimistic concurrency/FIFO remain authoritative. Failure-after-stock tests use real SQLite transactions, overriding the legacy test harness's always-disable-transaction manager.
- Current-policy/MissingBaseline rules passed; no implicit position mapping/reactivation. Service-enabled manual Warranty replacement is server-guarded; disabled-Service Warranty regression remains green. Unrelated positions/reminders retain their metadata.
- Final evidence: build 0 errors; Domain 33, Application 6, EF 203, Web Service 24, Warranty Web 13 passed. Desktop/mobile browser interaction and 204 completion passed. Offline model drift NONE; migration not applied; VPL/production untouched.
- S-003 DONE; S-004 READY/unclaimed; S-005/S-006 HOLD. Future agent: use `docs/S003_SERVICE_COMPLETION.md`, `docs/MODULE_MAP.md` and `docs/CUSTOMERCARE_OPERATIONS.md`. Live SQL Server/Redis/legacy-data rehearsal remains S-006, not an implicitly completed acceptance gate.

### 2026-09-07 - S-002 Service Order Workflow Complete

- Decision: S-002 COMPLETE. Implementation commit `2f27ed81618403d7375b2af237025e6e931bbe3f` (`feat(service): add service order workflow`) follows docs baseline `9a0967b`. It is local only; no push, deploy, runtime enablement or database action. S-003 is READY and unclaimed; S-004..S-006 remain HOLD.
- Delivered Draft -> Confirmed -> InProgress plus reason-required cancellation from Draft/Confirmed/InProgress. Completed remains a passive historical enum/state only: no Complete/CompleteLine API, domain command or UI action exists in S-002. Completed/Cancelled are terminal.
- Material and Labor are exclusive line types with positive integer quantity. Labor cost snapshot preserves null versus zero. Draft edits preserve line IDs and snapshots unless the operator explicitly replaces the selected item; inactive historical catalog items remain renderable and do not block header/note edits.
- Every update/transition carries an expected concurrency stamp. Feature and permission guards are enforced server-side. Service list, Asset/Material/Work/Technician lookups use database count/filter/whitelisted sort/Skip/Take; page 2 was executed for orders, assets and works. Material eligibility is a translated Component + inventory-enabled StockItem EXISTS query.
- UI consists only of Service list/Create/Edit/Details and a Cancel ABP modal. It uses server-side DataTables, remote Select2 continuation, dynamic Material/Labor rows, integer quantity, Service-scoped invariant/vi-VN money binding, antiforgery and encoded output. No browser prompt/alert/native confirm and no unfinished completion action. Stale Cancel displays a business error in the modal.
- Migration `20260906185742_AddServiceOrderWorkflow` adds nullable `AppServiceOrders.CancellationReason` and nullable `AppServiceOrderLines.StandardCostSnapshot`; no DML, backfill, duplicate table, core-table change or migration application. Offline EF reports no pending model changes.
- Test results: build 0 errors/4 existing warnings (two OpenIddict nullable, Scriban NU1903, Web test entrypoint CS7022); Domain Service 14/14; Application Service 6/6; EF Service 72/72, including S-002 7/7; EF cross-module 110/110; Service Web 18/18; Warranty Web 13/13; JS syntax and diff checks PASS. Tests found and fixed EF owned-line add tracking, a non-translatable material projection, and missing stale-error rendering in Cancel modal. No full Web suite or S-002 screenshot/browser claim.
- Confirm/Start create no InventoryTransaction, maintenance event, reminder or revenue. Payment, FIFO issue, completion, CustomerCare schedule changes and reporting remain explicitly deferred to S-003+.
- Excluded user-owned files remain untracked: `docs/VPureLux_Sales_Flow_Design_Review_for_Codex_5_6_Sol.docx`, `docs/html.txt`. No VPL/production connection, DbMigrator, migration application, deploy/restart or push occurred.
- Next agent: claim S-003 separately. Preserve S-002 snapshots and line identity; add completion atomically with batched FIFO and idempotent CustomerCare effects. Do not infer old snapshots from current Work/Component/BOM/policy settings, and do not start payments/reports.

### 2026-09-07 - S-001 Service Foundation Complete

- Decision: S-001 COMPLETE. Source `babc96fc5ecba242e3f23d0612c3a46df3916dc3` (`feat(service): restore foundation and work catalog`) follows baseline `83fb3f5`. W-008 accepted commit `d1e8b56` and audit `6ef1def` are unchanged. Implementation is local only; a separate documentation commit records completion. S-002 READY, unclaimed; S-003..S-006 HOLD.
- Restored exact migration pair `20260824113235_AddServiceModule` from `220d41c` (also identical at `bcc1b36`). Added `20260906174229_AddServiceWorkCatalogFields`, with only two nullable AppServiceWorks columns and no business DML/default backfill/core-table alteration. Current snapshot adds Service only; no historical full snapshot replacement. Original numeric enum meanings are preserved; ServiceIssue and posting are not restored.
- Implemented default-disabled flag, server permissions, manually coded Work Catalog, nullable cost, stale-edit protection, whitelisted database paging and an ABP modal. Service orders/lines/payments remain passive shells. Existing document snapshots stay independent from Work edits. The form uses a local invariant decimal binder to prevent vi-VN 123.50 -> 12350 corruption; no global/Sales binder change.
- Verification: build 0 errors/2 warnings, Domain 38/38, Application contracts 4/4, full EF 215/215, Service Web 5/5, Warranty Web 13/13, offline EF model drift NONE, diff check PASS, local publish/static-script/font assets and desktop/mobile browser checks. Service API feature/permission execution is covered in EF tests as well as HTTP tests. No full/combined Web suite pass is claimed.
- Preserved two original 172/173 EF results and isolated race rerun evidence. Shared SQLite connection reuse was corrected in the test module to separate connections against one uniquely named in-memory database; a focused active-reader test and full EF run pass without weakening Sales assertions. This is test infrastructure only, not a Sales business change.
- Documentation: this handoff, `docs/MODULE_MAP.md`, and `docs/S001_SERVICE_FOUNDATION.md`. The historical Service audit remains an unchanged dated audit; no new assumption required rewriting it. Raw evidence/publish/browser fixtures stay ignored in `artifacts/s001-foundation/`.
- Excluded user-owned files remain untracked: `docs/VPureLux_Sales_Flow_Design_Review_for_Codex_5_6_Sol.docx`, `docs/html.txt`. No VPL/production connection, migration application, DbMigrator, deploy/restart, push or operational Service workflow occurred. Test data existed only in disposable SQLite in-memory databases.
- Next agent: claim S-002 separately, read the skill, audit and S-001 evidence/source. Preserve original Service migration identity and the current model; do not enable external runtime or apply migrations based solely on this offline foundation verification. Empty/legacy SQL Server rehearsal remains S-006.

### 2026-09-07 - Accepted W-008 Implementation Baseline Sealed

- Decision: W-008 BASELINE SEALED. Dedicated implementation commit `d1e8b5684d21eca3ee5586fe75913b60e24de190`; parent audit docs commit `6ef1def` is unchanged. No amend, squash, tag, or push. A separate handoff-only commit records this SHA.
- Scope: 19 accepted files (4 application/domain/EF source, 7 test files, 8 harness/evidence files). No new implementation edits were necessary. Exact committed paths:
  - `src/VPureLux.Application/Warranty/CustomerCareSalesIntakeService.cs`
  - `src/VPureLux.Application/Warranty/WarrantyAppService.cs`
  - `src/VPureLux.Domain/Warranty/WarrantyRepositoryContracts.cs`
  - `src/VPureLux.EntityFrameworkCore/Warranty/EfCoreCustomerCareSalesIntakeRepository.cs`
  - `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Sales/SalesWorkflowTests.cs`
  - `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/VPureLuxEntityFrameworkCoreTestModule.cs`
  - `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Warranty/CustomerCareSchemaTests.cs`
  - `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Warranty/WarrantyPermissionTests.cs`
  - `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Warranty/CustomerCareUatSafetyTests.cs`
  - `test/VPureLux.Web.Tests/VPureLuxWebTestModule.cs`
  - `test/VPureLux.Web.Tests/Pages/WarrantyAntiforgeryTests.cs`
  - `docs/evidence/w008/Capture-VplBaseline.ps1`
  - `docs/evidence/w008/Run-VplFixtures.ps1`
  - `docs/evidence/w008/Run-VplLifecycle.ps1`
  - `docs/evidence/w008/Run-VplSales.ps1`
  - `docs/evidence/w008/Vpl-UatApi.ps1`
  - `docs/evidence/w008/isolated-r2-reconciliation.json`
  - `docs/evidence/w008/local-evidence-manifest.json`
  - `docs/evidence/w008/original-run-reconciliation.json`
- Excluded, still user-owned/untracked: `docs/VPureLux_Sales_Flow_Design_Review_for_Codex_5_6_Sol.docx` and `docs/html.txt`. Ignored raw artifacts remain local; source-controlled evidence preserves original D02 40/41 and official isolated R2 41/41 without rerunning or altering data.
- Verification for this seal: Release build 0 errors/4 warnings; Domain 6/6, EF 74/74, focused Warranty Web 13/13; no skips/failures, no EF model drift, diff check passed. No claim of full UAT or combined Web pass.
- No Service implementation, migration generation/application, VPL/production database access, production action, redeploy, restart, or push occurred. Accepted source is local, not a new production release.
- Next action: S-001 is safe to start as a separate task after reading the skill and Service audit; it remains READY and unclaimed. Historical handoff entries below describe their original point-in-time state, not the current sealed baseline.

### 2026-09-07 - Warranty Accepted And Service Inventory Audit Complete

- User explicitly accepted Warranty/CustomerCare; W-001..W-008 remain DONE; W-GATE changed PENDING -> DONE. Configuration/templates only initialize/suggest new facts; historical snapshots change only through explicit permitted, audited business actions. New cycles may use current policy without rewriting older cycles.
- SERVICE-INVENTORY-AUDIT progressed HOLD -> READY -> IN_PROGRESS -> DONE; S-001 is READY, S-002..S-006 stay HOLD with unchanged dependencies. Current active task is None. No Service implementation started.
- Inspected bcc1b36/220d41c actual code and intervening report-minifier fix; produced `docs/SERVICE_INVENTORY_AUDIT.md` with inventory, reuse/refactor/discard/missing matrix, concrete defects, migrations, FIFO/care/payment/report/test assessments and exact sequential plan.
- Critical migration finding: the old Service migration was historically applied and VPL evidence contains existing Service data, while current source lacks its migration/model. Recover applied migration identity and reconcile current model before future upgrades; do not replace the Sales V1 snapshot or blindly recreate tables.
- Scope/verification: documentation/control files only; read-only preflight/Git/source/evidence review; no new build/test claim, application edit, migration, database connection, production access, deploy, restart or push. Prior W-008 implementation/test changes remain local and uncommitted; user-owned files remain excluded.
- Next agent: read this audit and the skill before claiming S-001. Preserve the W-008 working-tree baseline; acceptance and this documentation commit do not imply those fixes are deployed.
- Documentation commit scope: `docs(service): audit existing service implementation` captures AGENT_TASKS, Service audit, operations/module map and the related W-008 acceptance/evidence summary only. W-008 application/tests, raw evidence/harness files and both user-owned files remain outside this commit. Resolve this commit by subject in Git; no push was performed.

### 2026-09-06 - W-008 Technical Gate Complete

- Decision: W-008 DONE; READY FOR USER ACCEPTANCE = YES. W-GATE remains PENDING and Service remains HOLD.
- D01: Existing reminders remain immutable. Complete starts a successor from the current enabled policy only while the Component and actual position are active and mapped; absent/disabled policy or inactive/unmapped state completes without a successor. Re-enabling does not backfill a missed successor.
- Intake race: The batch lock is retained, each candidate is revalidated under the shared Sales order lock, and stale candidates are skipped without recording a failure. Deterministic cancel/replacement barriers and unchanged/replay coverage pass.
- Security/UI: All seven Warranty permissions pass server-side deny/allow execution; four authenticated Razor mutation endpoints reject missing antiforgery tokens through the application's HTTP 400 path; server-side asset/reminder paging, filtering, sorting, and page 2 pass.
- Verification: Release build 0 errors; Domain 102/102, Application 30/30, EF 208/208, focused Warranty Web 13/13, and Sales/Inventory API Web 6/6 pass. EF has no pending model change. The known combined Web testhost resource leak remains outside this release claim.
- Data/release boundary: No new VPL fixture/write and no production access, migration, deployment, restart, commit, or push occurred in this close-out. D02 preserves the original 40/41 result as an accepted test-harness exception and isolated R2 41/41 as official data-safety evidence. User-owned untracked files remain untouched.
- Next action: The user performs/accepts the operator workflow at W-GATE. Only after that acceptance may W-GATE be marked DONE and the first Service prerequisite become READY.

### 2026-09-04 - W-008 Verification Executed, Acceptance Blocked

- Decision: W-008 BLOCKED for acceptance, not DONE; W-GATE PENDING; Service HOLD. Sales release remains frozen at `release-2026-09-03-sales-v1` / `a4717aa`.
- Findings: next-cycle rule differs between current code/tests and requested behavior (3/15 copied after policy changed to 6/21). Asked user to choose; do not silently rewrite history or business rules. First-run legacy group metadata touch is retained as an exception, not repaired or excluded retroactively. New isolated R2 fixtures preserved all 41 pre-R2 table fingerprints.
- Execution: VPL only, prefixes W008_20260904 and W008_20260904R2. Four R2 orders SO-202609-000001..000004 exercised intake, install, quantity/price revisions, cancellation, and actual concurrent HTTP requests using SQL/Redis. External machine mapping created no stock issue; reminder history/replay, notification filters, and reports were checked. Tests: Domain 54, Application 2, EF 123 distinct across final group + one added test, Web 15; all executed cases passed, but this is not full business acceptance.
- Runtime cleanup: stopped owned local Web processes (port 5198), closed temporary browser tab, reset mobile viewport. No production database connection/mutation, production deployment/migration/restart, Service merge, commit, or push. VPL fixture writes and the first-run exception are explicitly recorded above.
- Changed surface: documentation, test-only PowerShell harness/evidence, and focused EF tests. No application code or schema change. User-owned `docs/VPureLux_Sales_Flow_Design_Review_for_Codex_5_6_Sol.docx` and `docs/html.txt` remain untouched/untracked.
- Resume from `docs/W008_WARRANTY_UAT_20260904.md`; do not rerun fixture creation or overwrite original baseline. Raw artifacts remain in `artifacts/w008-verification/` and `artifacts/w008-isolated-r2/`, with SHA-256 manifest under `docs/evidence/w008/`. Complete remaining role/UI/intake-race evidence and policy decision before user acceptance; future Service work starts with SERVICE-INVENTORY-AUDIT only after W-GATE.

### 2026-09-04 - W-008 Verification Claimed

- Explicit user task authorizes Warranty regression/UAT on VPL, not production writes, Sales expansion, or Service implementation.
- Corrected the obsolete environment boundary: VPL = test/UAT; VPureLux = production. Historical production-UAT notes below are historical evidence, not current permission.
- W-008 moved through authorized READY to IN_PROGRESS; W-GATE remains PENDING until separate explicit user acceptance. Added a HOLD-only Service inventory audit prerequisite to avoid duplicate implementation.
- Evidence and remaining work are tracked in `docs/W008_WARRANTY_UAT_20260904.md`. No test or UAT pass is claimed at task start.

### 2026-09-04 - SALES-V1-RELEASE Source Milestone Accepted

- Decision: COMPLETE. Sales Post-Confirmation V1 is RELEASED / ACCEPTED with no active implementation task.
- Annotated tag: `release-2026-09-03-sales-v1`; message `Sales post-confirmation V1 production release`; tag object `1b3bc2e5d60318d2bf9183df6052a2769afe0ec0`; target `a4717aa361e931aaa2bb09fd55d20d0efd9599c2`. Pushed separately to origin without force and verified the remote peeled target.
- Production baseline retained from the accepted 2026-09-03 rollout: release `/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa`; rollback `/opt/vpurelux/releases/web-20260825-111401`; rehearsal/deployment/reconciliation PASS; 14/14 matching legacy fingerprints; no production business-data mutation from migration. No live recheck was performed in this Git/docs-only task.
- Files changed: `AGENT_TASKS.md` and `docs/SALES_PRE_INSTALLATION_V1_TECHNICAL_IMPLEMENTATION.md` only. Both user-owned untracked files remain excluded. No application code, business rule, or migration was changed; production/database/redeploy actions: NO.
- Release boundary: CancelAndClone, non-machine terminal lock, and Web testhost leak require separate tasks; none blocks or silently expands this accepted release. W-GATE and Service statuses are unchanged.
- Next action: Await a new explicit task; use the release tag, not a later docs-only HEAD, to identify the production code source.

### 2026-09-03 - SALES-V1-ROLLOUT Production Complete

- Decision: `PRODUCTION ROLLOUT COMPLETE`. Pushed and deployed commit `a4717aa361e931aaa2bb09fd55d20d0efd9599c2` from `origin/codex/warranty-release-review` without force.
- Safety and backup: runtime and SQL both confirmed `VPureLux`. Backup `/var/opt/mssql/data/VPureLux-pre-sales-v1-20260903-180246.bak` used `COPY_ONLY, CHECKSUM` and passed `RESTORE VERIFYONLY WITH CHECKSUM`.
- Migration and data: latest migration moved from `20260824113235_AddServiceModule` to exactly `20260826051356_AddSalesPreInstallationV1Foundation`. No custom business DML/backfill ran. All 14 legacy fingerprints, five sampled confirmed orders, and Revenue/Profit outputs matched before migration, immediately after migration, and after Web smoke. Revision/Cancellation/Refund tables remained empty and no smoke data was created.
- Release: active `/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa`; rollback `/opt/vpurelux/releases/web-20260825-111401`. Web artifact SHA-256 `5185335BA99BF7D16ACC00D44346A3EA989903F655AD726CACF074D8C60C0393`; DbMigrator SHA-256 `9ED876037E0B5D8501260D9835E2035F88316ED22EC324D9C6AE14F48AE8910E`.
- Smoke: service active; three sequential health probes Healthy; root/login/CSS/JS/font and authenticated Sales list/detail/Adjust/cancel modal/Returns/Refunds/Revenue/Profit all returned HTTP 200. Server-side Sales/return/refund DataTables returned valid paged JSON. A non-machine Confirmed order exposed Adjust/Cancel. No HTTP 500 or error-level journal entry occurred. Initial 502 responses were limited to startup warm-up and stopped before the three healthy probes.
- Coverage note: production contained zero Draft orders and zero Installed assets, so those two non-destructive live cases were unavailable. Accepted rehearsal/test evidence covers Draft compatibility, installed-machine `SALES_018` lock, and vi-VN decimal submission. No legacy row was changed to manufacture production smoke coverage.
- Next action: Obtain user acceptance or a new explicit task. Do not start Service while W-GATE remains open.

### 2026-09-03 - SALES-V1-REHEARSAL Complete And Ready For Authorized Rollout

- Decision: `READY FOR PRODUCTION ROLLOUT`. Phase 2 implementation is complete and the final legacy-data safety gate passed. This rehearsal did not push, migrate production, or deploy.
- Database: Used only `VPL_SALES_REHEARSAL_20260903`, restored from the 2026-08-25 VPL backup. Baseline ended at `20260824113235_AddServiceModule`; Sales V1 was pending. Applied exactly `20260826051356_AddSalesPreInstallationV1Foundation`. Production `VPureLux` was not accessed.
- Migration audit: `Up()` contains no business `UPDATE`, `DELETE`, `MERGE`, backfill, historical recalculation, or automatic Revision/Cancellation/Refund/Asset creation. Stored-procedure alteration succeeded against real definitions beginning with irregular `CREATE   PROCEDURE` whitespace.
- Legacy evidence: Before/after counts matched for Orders 2, lines 3, payments 1, inventory transactions 3, inventory lines 5, lots 2, allocations 3, customers 1, assets 1, BOM versions 2, and BOM items 3. All 14 deterministic fingerprints matched. Legacy payment, inventory references, totals/cost/profit, external asset and positions remained unchanged; 3/3 lines remained effective with no revision link. New process tables were empty before UAT.
- Legacy smoke/report: Current app returned HTTP 200 for Sales list/details and preserved 2 lines plus 3m paid / 3.5m remaining on `SO-202608-000001`. Revenue/profit procedures compiled and returned exactly the same legacy values before/after migration.
- UAT A-I: Price-only posted no inventory; increase issued delta FIFO; decrease required Warehouse confirmation and reversed original lot/cost; machine replacement reconciled pending assets; payment carry-forward and append-only refunds matched formulas; paid non-machine cancellation closed independent return/refund queues; non-machine orders remained eligible; installed machine blocked whole-order adjust/cancel with `SALES_018`. UAT reports excluded cancellation and superseded lines and matched 41.5m revenue / 850k cost / 40.65m profit.
- Automated verification: Solution build 0 errors. Application 2/2, Domain 35/35, EF 95/95. Combined Web filter was stopped after testhost grew to about 3.6 GB without completing; smaller operator UI, Vietnamese decimal validation, Warranty pages, and Sales API groups passed 15/15. No full combined Web pass is claimed. EF reports no pending model changes; diff check passes.
- Files changed by rehearsal: `docs/SALES_PRE_INSTALLATION_V1_TECHNICAL_IMPLEMENTATION.md` and `AGENT_TASKS.md` only. User-owned `docs/VPureLux_Sales_Flow_Design_Review_for_Codex_5_6_Sol.docx` and `docs/html.txt` remain untracked and excluded.
- Next action: Do nothing to production until the user gives a new explicit rollout instruction. Then follow the deployment runbook: capture a read-only production baseline, create and verify a backup, publish from the exact approved commit, apply the reviewed migration once, deploy, and reconcile the same legacy counts/reports plus health/UI/API logs.

### 2026-08-28 - SALES-V1-PHASE2 Completed Locally And On VPL UAT

- Eligibility correction: Commit `fea7423` removed all machine-only eligibility from confirmed Sales correction/cancellation. Every Confirmed order may be adjusted or cancelled when permission, conflict, and installation-lock rules pass. `IsMachine` is used only for CustomerCare side effects; any installed machine asset still locks the whole order.
- Sales behavior: Posted payments carry forward unchanged. Revised totals derive remaining amount/refund due. Every negative inventory delta requires Warehouse confirmation; reversal uses the original posted allocation, lot, quantity, and cost. Draft cancellation still uses `CancelAsync`; Confirmed cancellation routes to the post-confirm operation.
- CustomerCare: Applied machine-line increases create only missing PendingInstallation assets; decreases/removals/replacements cancel only surplus affected pending assets; unchanged and price-only lines do nothing; installed and legacy assets are never rewritten. Non-machine lines never trigger reconciliation.
- Operator UI: Added Manager adjustment page and Confirmed-cancel ABP modal, Warehouse return queue/modal, Accounting refund queue/modal, and payment-void modal. Return/refund lists use database Count/filter/sort/Skip/Take with server-side DataTables. Product lookup is remotely paged. No native prompt/alert/confirm or top-N pseudo-paging was introduced.
- UAT defect fixed: The revision quantity `RangeAttribute` parsed decimal limits with `vi-VN` culture and returned HTTP 500. It now uses invariant-culture limits, matching existing Sales DTOs, with a focused regression test.
- VPL evidence: New non-machine orders `SO-202608-000001` through `SO-202608-000006` covered price-only, paid quantity increase (10m paid 5m -> 12m, remaining 7m), paid decrease (10m paid 8m -> 6m, refund 2m), product replacement, line removal, and paid cancellation. Decrease was blocked before Warehouse confirmation. Original payments remain Posted; refunds are append-only; lot/cost preservation checks passed; completed return/refund queues returned zero.
- Schema boundary: No new migration was created. The pre-existing Phase 1 migration was corrected to convert any SQL Server procedure definition to `ALTER PROCEDURE` robustly, then applied only to `VPL`. No backfill, rebuild, DELETE, or historical-order edit ran. Production `VPureLux` was not accessed.
- Verification: Build 0 errors; Application 2/2; Domain 35/35; EF 95/95; Web 112/112 before the locale fix plus locale regression 1/1 after it; no pending EF model changes; JS/JSON/diff checks passed. A final combined Web rerun reproduced the documented testhost memory leak and was stopped at about 5.6 GB RAM.
- Commits: Foundation correction `fea7423`; Phase 2 operator workflow `1436feb`. Neither commit is pushed. No deployment was performed.
- Excluded user-owned files: `docs/VPureLux_Sales_Flow_Design_Review_for_Codex_5_6_Sol.docx` and `docs/html.txt`.
- Next action: Review the local Phase 2 commit. Push and production deployment require a new explicit instruction. Before any production rollout, verify the production migration history read-only and rehearse the existing Phase 1 migration against a production-schema clone.

### 2026-08-26 - SALES-V1-PHASE1 Backend Foundation Completed Locally

- Result: Added Manager-only post-confirm revision and cancellation foundations while retaining Confirm-time FIFO issue and the existing three Sales statuses.
- Inventory: Delta issue/reversal uses batched context loads and original allocation/lot/cost facts; unchanged and price-only lines do not post inventory; failures roll back atomically; apply/return/refund paths are idempotent.
- Payment/cancellation: Posted payments carry forward; refund due is derived; real payments stay immutable unless explicitly voided with permission/reason; cancellation is effective immediately with independent stock-return and refund obligations.
- Lock/audit/reporting: Revision, cancellation and sold-machine installation share the `SalesOrderId` lock; business events reuse `AppBusinessAuditLogs`; reports and CustomerCare intake read effective lines only.
- Persistence/API: Added schema-only migration `20260826051356_AddSalesPreInstallationV1Foundation` and explicit `SalesPostConfirmationController` endpoints. No production or remote database was used.
- Verification: Full build 0 errors; Application Sales 2/2, Domain Sales/Inventory 30/30, EF Sales/Inventory 84/84; no pending EF model changes; diff check clean.
- Deferred: Razor/DataTables/ABP modal UI, Cancel-and-clone, and pending CustomerCare asset reconciliation after an applied product/quantity revision.
- User-owned file excluded from implementation commit: `docs/VPureLux_Sales_Flow_Design_Review_for_Codex_5_6_Sol.docx`.
- Next action: Implement SALES-V1-PHASE2 on this foundation, validate only against test database `VPL`, then request explicit approval before any production migration or deployment.

### 2026-08-26 - V1 Post-Confirm Business Boundary Accepted With Refinements

- Input/review: The user supplied `docs/VPureLux_Sales_Flow_Design_Review_for_Codex_5_6_Sol.docx` as review material and then accepted the simplified direction with five refinements. The DOCX remains user-owned and unmodified.
- Final boundary: Keep `Confirm = FIFO Issue`; do not add reservation/delivery/fulfillment architecture or redesign the core Sales lifecycle. User-facing Sales remains `Draft`, `Confirmed - Waiting Installation`, and `Cancelled`, with first installation represented as a derived modification lock rather than a new Sales status.
- Revision: Use delta impact. Unchanged lines retain original FIFO allocations and cost snapshots. Price/information changes do not touch stock; quantity/product changes affect only the difference and enforce prerequisites by impact. Separate opening a revision from applying it; failed apply leaves the current order fully effective.
- Payment/customer: Carry posted payments forward across revisions. Recalculate remaining/overpaid from the revised total and create a refund obligation when required; do not force net paid to zero. CustomerId stays immutable after Confirm; provide a convenience `Cancel and create new order` flow that copies only operator-entered business fields.
- Cancellation: Manager approval makes cancellation effective immediately, blocks installation/revision, removes the order from active sales, and cancels/supersedes pending machines. Inventory return and payment refund/void remain truthful, independent follow-up tasks. UI shows outstanding obligations without adding Sales statuses; the internal process closes when both branches are resolved.
- Documentation: Rewrote `docs/SALES_PRE_INSTALLATION_CHANGE_CANCELLATION_BUSINESS_SPEC.md` as the approved V1 business target and `docs/SALES_PRE_INSTALLATION_CHANGE_CANCELLATION_FLOWS.md` as the simplified flow model. No application code, migration, database, server, or deployment was touched.
- Next action: Produce a minimal ABP-aligned technical design, schema-only migration review, task list, and VPL-only UAT matrix when explicitly requested. Treat reservation, delivery orders, Sales Completed status, line-level post-install lock, customer credit, and non-machine post-confirm revision as out of scope.

### 2026-08-26 - Pre-Installation Sales Adjustment And Cancellation Business Design

- Agent/scope: Codex reviewed the exact source snapshot tagged `release-2026-08-24-warranty-notifications` on branch `codex/warranty-release-review`. Newer Service work remains preserved on `main` at `220d41c`; no production deployment, database access, code change, migration, or data mutation occurred.
- Business finding: Sales confirmation already posts FIFO inventory, financial snapshots, receivables, reporting state, and asynchronous machine intake. Therefore a confirmed order cannot be edited in place without reversal and reconciliation. Customer cancellation before installation has the same inventory/payment/machine side effects and must be treated as a controlled workflow.
- Documents: Added `docs/SALES_PRE_INSTALLATION_CHANGE_CANCELLATION_BUSINESS_SPEC.md` and `docs/SALES_PRE_INSTALLATION_CHANGE_CANCELLATION_FLOWS.md`. They cover Manager-only revisions, cancellation, physical goods return, void versus real refund, pending-machine supersession, first-installation permanent lock, audit/concurrency, decision matrix, acceptance criteria, and Mermaid process/state/sequence diagrams.
- Recommended boundary: Apply the first version only to orders containing at least one machine; any first completed installation locks the entire order. Keep non-machine confirmed orders immutable. Do not apply a revision until net posted payment is zero, and do not finalize cancellation until both inventory and payment settlement are complete.
- Required next action: User reviews and approves decisions D01-D08. Only after approval should an agent produce a technical design, migration review, implementation task breakdown, and VPL-only test plan.

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
