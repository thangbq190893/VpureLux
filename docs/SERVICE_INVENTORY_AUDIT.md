# Service Inventory Audit

Audit date: 2026-09-07 (Asia/Saigon). Decision: **SERVICE AUDIT COMPLETE**.

The user explicitly accepted Warranty/CustomerCare on this date. W-001..W-008 and W-GATE are DONE. SERVICE-INVENTORY-AUDIT is DONE; S-001 is READY but has not started. S-002..S-006 retain their dependencies. This is a source/documentation audit, not implementation, deployment, a new UAT run, or authorization to write a database.

Baseline and provenance:

- Current starting HEAD: `5c5d1de66d44a2968dff9677f9e325aed9b88b41`, branch `codex/warranty-release-review`.
- Production source remains `a4717aa361e931aaa2bb09fd55d20d0efd9599c2`, tag `release-2026-09-03-sales-v1`. Accepted W-008 application/test fixes are still in the local worktree and are not part of that deployed commit. Acceptance does not imply deployment.
- Main implementation inspected: `bcc1b36cfd4edb5f2e2ac2df24400048bdacbfac`.
- Antiforgery fix and final historical tree inspected: `220d41c3817f9bd2b5bc1d5c79805face82c1132`.
- These are historical branch commits, not Service code in current HEAD. Their merge base with HEAD is `55aaf24b369ef49bf081c00e00bc50f05f8d42ac`.
- Between them, `be4a5ad` fixes report JavaScript minification; `457fceb` and `c3df0a1` record deployment/preparation. Do not attribute that minifier fix to `220d41c` or lose it when reusing UI.
- Evidence notation below: historical paths refer to the `220d41c` tree unless stated otherwise. Inspect with `git show 220d41c:<path>`; these are not links to files currently present in the checkout.
- Read both business and technical DOCX documents in `docs/service-care-design/`, current operations/module map, W-008 evidence, source and test files, migration/Designer, EF mappings, and Sales V1 migration/reconciliation records. The user's current decisions override obsolete proposal assumptions, including the old synchronous Sales/Warranty integration.

Authoritative configuration rule, accepted 2026-09-07:

Configuration/templates only suggest or initialize new records. Persisted business facts are independent snapshots. A later Work Catalog, default price, Component metadata, BOM, package, or policy edit must not rewrite, recalculate, resync, or backfill existing facts. An explicit permitted and audited business edit may change the intended fields. A new replacement cycle may use the policy effective when that cycle is created; it does not change the old cycle.

## 1. Existing Implementation Inventory

| Area | Existing | Commit/File | Status |
|---|---|---|---|
| Shared definitions | Draft=1, Confirmed=2, InProgress=3, Completed=4, Cancelled=5; Material=1/Labor=2; Work Active/Inactive; Payment Posted/Voided; unused receivable enum | `src/VPureLux.Domain.Shared/Service/{ServiceEnums,ServiceConsts}.cs` | Historical only; preserve numeric meanings |
| Domain | `ServiceWork`, `ServiceOrder`, owned `ServiceOrderLine`, independent `ServicePayment`; int planned/actual quantities; price/name/unit and completed totals snapshots | `src/VPureLux.Domain/Service/*.cs` | Useful foundation, incomplete invariants |
| Contracts/DTO | CRUD, confirm/start/complete/cancel, payment, lookup and paged list APIs; DTO validation | `src/VPureLux.Application.Contracts/Service/{IServiceAppService,ServiceDtos,ServiceInputs}.cs` | Missing line identity/version in edits, full command replay facts, labor cost and advance summaries |
| Application | `ServiceAppService` orchestrates order, stock, care and payment; `ServiceWorkAppService` manages independent work catalog | `src/VPureLux.Application/Service/*.cs` | No service runtime enablement guard; orchestration needs focused refactoring |
| Repositories/read models | Repositories for orders, work, payments and paged order projection with payment subquery; material/asset/work lookups | `src/VPureLux.Domain/Service/{ServiceRepositoryContracts,ServiceReadModels}.cs`; `src/VPureLux.EntityFrameworkCore/Service/EfCoreServiceRepositories.cs` | Real database paging exists; lookup continuation, sorting and concurrency gaps |
| EF | Three DbSets; owned lines; rowversion for orders/payments; Restrict external FKs; unique code, order number, completion/payment keys | `src/VPureLux.EntityFrameworkCore/Service/*Configuration.cs`; `EntityFrameworkCore/VPureLuxDbContext.cs`, `VPureLuxEntityFrameworkCoreModule.cs` | Missing CHECK guards; old model snapshot cannot replace current snapshot |
| Migration | Four new tables, no business DML or core-table alteration in Up | `src/VPureLux.EntityFrameworkCore/Migrations/20260824113235_AddServiceModule{,.Designer}.cs` | Historically applied; absent from current branch migration chain |
| Permissions | Service root + View/Create/Edit/Confirm/Complete/Cancel/ManagePayments/ManageWorks/ViewCost/ViewProfit; separate Service and consolidated report permissions; admin grants | `Application.Contracts/Permissions/VPureLuxPermissions.cs`, `VPureLuxPermissionDefinitionProvider.cs`; `Application/Permissions/VPureLuxPermissionDataSeedContributor.cs` under `src/VPureLux.*` | Definition and attributes exist; no real deny/allow test matrix |
| UI | One primary Service menu, Works from Service context; Create/Edit/Details/list; work/payment/completion ABP modals; machine-detail link | `src/VPureLux.Web/Pages/Service/*`, `Pages/Warranty/Assets/Details.cshtml`, `Menus/*` | Good operator structure; preserve simplicity, repair details below |
| Inventory | `ServiceIssue=6`; batch lot query; one issue transaction with ServiceOrder reference; actual allocation cost per material line | Historical `InventoryManager`, `InventoryRepositoryContracts`, `EfCoreInventoryLotRepository`; `ServiceAppService.CreateInventoryIssueAsync` | Reuse current allocator; old batch-query overload is missing in current HEAD |
| Maintenance | Replacement event per performed material position; closes pending reminders, builds successor from enabled current policy | `ServiceAppService.ApplyMaintenanceScheduleAsync` | Automatic remap/reactivation conflicts with accepted rules |
| Payment | Separate table, Add/Void, posted-only sums, sequential overpayment guard | `ServicePayment`, `ServiceAppService.AddPaymentAsync/VoidPaymentAsync` | Race, replay, audit and customer-advance handling incomplete |
| Reporting | Completed-Service revenue/cost/profit; Sales + Service `Concat` with source dimension and summary; permissions | `Application.Contracts/Reports/BusinessRevenueDtos.cs`; `Application/Reports/BusinessRevenueAppService.cs`; `EntityFrameworkCore/Reports/EfCoreBusinessRevenueReadRepository.cs`; `Web/Pages/Reports/*BusinessRevenue*`, `ServiceRevenue*` | Revenue basis reusable; Sales V1 refund/payment parity and missing cost need work |
| Tests | 5 Domain facts, 1 EF workflow + 2 EF model/permission facts, 5 Web source facts in final old tree | `test/VPureLux.Domain.Tests/Service/ServiceDomainTests.cs`; `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Service/*Tests.cs`; `test/VPureLux.Web.Tests/Pages/ServicePagesTests.cs` | 13 source test methods identified; none executed in this audit |

Current HEAD has no Service domain/application/UI/test directories, Service DbSets, Service report implementation, or AddServiceModule migration. It does already have CustomerAsset/positions/events/reminders and `InventoryManager.AllocateFifo(..., IReadOnlyCollection<InventoryLot>)`. Working-tree W-008 adds current-policy successor semantics and the authoritative Sales intake race fix. Preserve both sets of current behavior when recovering Service.

## 2. Reuse Matrix

An X in REUSE means the identified behavior/snippet needs no business redesign, not that the entire historical commit is approved for cherry-pick. Every recovered artifact still needs current-baseline compilation and regression checks.

| Area | REUSE | REFACTOR | DISCARD | MISSING | Reason |
|---|---|---|---|---|---|
| Independent Service context and one CustomerAsset per order | X | | | | Matches the accepted workflow; no Catalog Product for labor |
| Explicit enum numbers, int quantities, money rounding | X | | | | Preserve old persisted values; add domain/DB guards around them |
| Historical name/code/unit/price fields and completed financial totals | X | | | | Reads use stored facts, not current template joins |
| Work entity CRUD shell | | X | | X | Add unit, optional standard labor cost and unavailable-cost semantics; preserve default-price copying |
| Order/line state methods | | X | X | | Remove direct Confirmed -> Completed shortcut; guard CompleteLine against modifying terminal records |
| Draft UpdateAsync line replacement | | | X | X | Replace wholesale remove/recreate with identity-preserving intentional edits and expected version |
| ABP DTO/AppService/PageModel structure, one main list | X | X | | | Keep architecture and simple navigation; improve the contracts and validation |
| Current InventoryManager FIFO algorithm | X | | | | Already present; do not replace with historical file |
| Historical batch FIFO lot query | X | | | | Add overload to current repository without losing Sales changes; additional balance batching is needed |
| Service posting orchestration | | X | | X | Transaction boundary, command replay, order/asset concurrency, early validation and contextual errors |
| Automatic position remap/reactivation | | | X | | A stale Service line must not reactivate an inactive/unmapped position |
| Current-policy successor/date calculation | X | X | | | Reuse W-008 semantics through a CustomerCare-owned command in the Service transaction |
| Legacy original migration identity/Up/Down/Designer | X | | | | Preserve as applied history; recover only after compatibility design, never mutate its meaning |
| Old entire DbContext/model snapshot | | | X | | Would remove current Sales V1 fields, mappings and filtered indexes from model metadata |
| Payment ledger separation and posted-only sum | X | X | | X | Add lock, full replay validation, void audit/reason and advance/outstanding handling |
| Completed-only revenue + source dimension | X | X | | | Reconcile effective Sales and refunds; add missing labor-cost flags |
| Labor cost hardcoded zero and definitive profit | | | X | X | Unknown cost is not a known zero cost |
| `220d41c` antiforgery headers and `be4a5ad` minifier correction | X | | | | Preserve both; add actual HTTP rejection and publish tests |
| Lookup top-30 responses without continuation | | | X | X | Server filtering exists but results beyond first page are unreachable for broad searches |
| Existing tests | X | X | | X | Retain valid assertions, replace obsolete state/cost expectations and add missing matrix |
| Feature guard and single replacement authority | | | | X | Permissions alone do not disable APIs or prevent Warranty manual completion bypass |

## 3. Architecture Conflicts

These are defects/gaps in the historical implementation, not claims of current production incidents. No application fix was made during this audit.

| ID | Evidence and concrete risk | Required disposition |
|---|---|---|
| A01 | `ServiceAppService.UpdateAsync` calls ResolveLinesAsync on every input, removes all lines and assigns new IDs. ServiceOrderLineInput has no existing line ID; even a note/address-only save can refresh unchanged item name/unit from current catalog or fail because a retained item was deactivated. There is no background rewrite on policy/work edit; the defect is unintended refresh during an unrelated permitted draft edit. | S-002: keep unchanged line IDs/snapshots. Refresh selected fields only for an explicit line/item edit; retain historical labels for inactive selections. |
| A02 | `ServiceOrder.Complete` accepts Confirmed directly, and `CompleteLine` lacks an order-state guard. CompleteAsync performs stock/care work before final state validation. | S-002/S-003: enforce Draft -> Confirmed -> InProgress -> Completed; validate before side effects; prohibit terminal-line edits. Keep straightforward action buttons, without adding workflow screens. |
| A03 | Order/payment RowVersion exists, but DTOs carry no expected version. AppService reloads the latest entity on each request, so an old browser form can overwrite newer draft edits without detecting its staleness. | S-002: optimistic expected version/ConcurrencyStamp on edit and transition commands, plus transaction coordination for posting. |
| A04 | Complete replay compares only order status and key. Changed completion date, labor quantity or other payload under the same key is silently treated as the old command. Duplicate LineIds use Single() and can surface an unexpected exception. | S-003: validate unique line IDs; persist/compare a canonical command hash including date, all actual lines and source facts; return the original result only for exact replay. |
| A05 | AddPayment reads existing posted sum then inserts without touching/locking the order. Two different keys can both pass the cap. Rowversion on the two different payment rows does not protect the order-wide invariant. Cancel/complete/payment also lack shared coordination. | S-004: serialize monetary decisions per Service order and coordinate with state transitions. Test two payment requests, payment/cancel and payment/complete. |
| A06 | ApplyMaintenanceScheduleAsync unconditionally maps line.ComponentId onto a changed/unmapped position and calls SetReplacementBaseline, which makes a mapped inactive position Active. It never checks current Component activity for scheduling. | S-003: preserve authoritative current position state; actual replacement event remains valid, but no successor for inactive/unmapped/invalid policy. Any intentional remap is a distinct explicit audited choice, not inference from stale line data. |
| A07 | Old Service is always available when permission is granted; no Service runtime flag and no server guard on Warranty CompleteReminder when Service is enabled. | S-001/S-003: default-disabled Service configuration, guarded API/UI; switch actual replacement recording to Service completion when enabled. Keep Warranty reschedule/skip/contact behavior separate. |
| A08 | Service directly owns repositories for Warranty rows. Labor-only or material-without-position orders return before any maintenance event is appended. | S-003: CustomerCare-owned completion contract within the same transaction; machine-level completed-service fact, and per-position replacement events only for actual replacements. Never fabricate replacement events for labor or unperformed lines. |
| A09 | ServiceWork has no unit/standard cost; ResolveLines hardcodes labor unit, CompleteLine rejects nonzero labor cost, CompleteAsync always passes zero for Labor. | S-001..S-005: snapshot optional standard labor cost and its known/missing status; display provisional profit when missing, preserving known zero as a separate fact. |
| A10 | Stock/position/policy loading is batched, but balance.ApplyMovementAsync still performs FindAsync inside a per-stock loop. GetMaxOrderNoSequence loads all matching strings to calculate max in memory. | S-001/S-003: use existing BusinessCodeGenerator with database-side seed-max lookup; batch balances while reusing Inventory ownership. Do not claim zero N+1 for the old path. |
| A11 | Order lists sort only OrderDate; work lists ignore Sorting. UI advertises other sortable columns. Select2 sends search only, fixed MaxResultCount=30, no page/hasMore. Work/report text columns have no explicit text renderer. | S-001/S-002/S-005: real sort whitelist or disable unsupported sorts; remote paged lookups; explicit safe text rendering; preserve current selected values. This is not top-100 order-list rendering: the order list does have real Skip/Take. |
| A12 | Cancel has no reason and ignores advances. Void changes status with generic ABP modification audit but has no business reason/void timestamp fields or refund semantics. RemainingAmount is zero revenue minus payment before completion, so an advance appears as negative receivable. | S-004: separate advance, completed receivable and customer credit; cancellation must keep traceable unsettled money. Void of an erroneous receipt must not masquerade as an actual refund. |
| A13 | Repository CHECK constraints for Material/Labor exclusivity, quantity bounds and completion facts are absent. ServiceOrderRepository catches selected EF exceptions only, not all duplicate/conflict cases across payment/lot/care writes. | S-002..S-004: validate in Domain and DB where compatible; normalize predictable conflicts, preserve full rollback and give actionable identifiers. |

Configuration audit result: normal historical detail/report reads already use stored Service snapshots. No Work/BOM/policy edit handler was found that automatically rewrites Service history. Current-policy reads at completion are appropriate for a NEW cycle. A01 and A06 are the concrete snapshot/state defects; do not label all configuration references as defects or redesign accepted Warranty.

No unresolved product choice blocks the audit or S-001 foundation. The state sequence, independent payments, factual completion, optional position link and no-history-rewrite rules are already specified. Detailed settlement UX must express the existing advance/void/refund distinction in S-004; this audit does not authorize a new accounting ledger or Sales expansion.

## 4. Data/Migration Assessment

Migration inspected: `20260824113235_AddServiceModule`. Up only creates the following tables and their indexes/FKs:

| Table | Key columns/facts | Index/FK assessment |
|---|---|---|
| AppServiceWorks | Id, Code, Name, DefaultPrice decimal(18,2), Status, Note, full ABP audit/soft-delete | Unique Code; Status+Name; no unit or labor-cost fields |
| AppServiceOrders | Id/OrderNo; Customer/CustomerAsset/Warehouse; date, schedule, technician, status; customer/asset snapshots; completed totals, completion key, inventory reference, rowversion, audit | Unique OrderNo; filtered unique non-null completion key on non-deleted rows; Status+OrderDate and AssetId+OrderDate; Restrict FKs to Customers, CustomerAssets, Warehouses, InventoryTransactions and AbpUsers |
| AppServiceOrderLines | Owned OrderId, Id/LineNo; type; nullable Component/position/Work; code/name/unit; int planned/actual quantities; price/revenue/cost, note | Unique OrderId+LineNo; type+component and reference indexes; Restrict Component/position/Work FKs; ownership cascade from ServiceOrder only; no CHECK exclusivity/quantity constraints |
| AppServicePayments | OrderId, CustomerId, Amount, PaymentDate, byte method/status, reference, note, key, rowversion, audit | Unique idempotency key; OrderId and CustomerId+PaymentDate; Restrict order/customer FKs; no request hash or void reason |

Up has no ALTER of Sales/Product/Component/BOM/Customer/Inventory tables, no UPDATE/DELETE/INSERT/MERGE business-data backfill, and no enum-renumbering DDL. Down drops all four Service tables and must not be used on persisted business history. Ownership cascade is compatible with draft owned lines but must never be used to delete completed history.

**This is not a greenfield database.** Current local evidence, without a new DB connection:

- `docs/SALES_PRE_INSTALLATION_V1_TECHNICAL_IMPLEMENTATION.md` sections 12 and 17 record AddServiceModule already in the restored rehearsal backup and production history before Sales V1 rollout.
- `220d41c:AGENT_TASKS.md`, 2026-08-25 production deployment entry, records the original Service migration as applied.
- `docs/evidence/w008/isolated-r2-reconciliation.json` preserves VPL ServiceOrders 2, ServiceOrderLines 3, ServicePayments 2 and ServiceWorks 1 before/after R2, zero changed/missing legacy rows. These are historical evidence counts, not a live inventory taken today.

Recommended migration strategy for S-001 and subsequent schema tasks:

1. Recover the original migration and its Designer under the ORIGINAL ID with unchanged historical Up/Down/target-model semantics. Existing databases with that ID must skip it; empty databases must still be able to create the original four tables. Verify file identity against the historical commit.
2. Reconcile historical Service baseline mappings with the CURRENT Sales V1 model. Restore compatibility mappings for the four historical tables as needed before enabling any Service workflow. This may bring passive order/payment schema definitions into S-001, but does not implement their actions early.
3. Never copy `220d41c`'s whole `VPureLuxDbContextModelSnapshot` or DbContext. Merge Service declarations into the current model and regenerate the current snapshot through a reviewed forward schema change when implementation is authorized. The original Designer is historical metadata, not the current model snapshot.
4. Add new unit/cost/request-audit fields and constraints in later, additive forward migrations. New fields for legacy facts should allow unknown/null where needed; do not fill old labor cost from today's catalog or call old missing costs known zero. Review every constraint against old rows before adding it; a failing legacy row requires a separate decision, not silent repair.
5. Before ANY database application, in its separately authorized task, inspect history and schema: original ID + matching tables; no ID + empty Service schema; or partial/mismatched state. The third state blocks application for explicit reconciliation. Never hide a mismatch with CREATE IF NOT EXISTS or manual history insertion.
6. Rehearse both empty database creation and upgrade of a legacy-Service + Sales V1 copy; fingerprint all historical business rows and preserve enum values. Do not generate a second CREATE-table migration over the old tables.

No migration was generated or executed in this audit. No live schema/history query was performed. The old migration is reusable as history; the old entire snapshot and a blind full-commit cherry-pick are not.

## 5. Inventory/FIFO Assessment

Old Service issues stock only from CompleteAsync for Material lines with actual quantity > 0. Confirm and Start do not reserve/issue; Labor never creates inventory. One transaction records ServiceOrder source and per-line actual FIFO allocation cost. The historical EF test covers stock 10 -> 8, then a failed request for 9 leaving stock 8 and no failed-order ledger entry.

`ServiceIssue=6` is appended after existing values 1..5, so those meanings are preserved. Current HEAD still uses 1..5; no conflict at value 6 was found in source. Persisted old Service transactions may already contain 6. Restore the enum deliberately, plus ledger labels/source links and tests; do not renumber. Old Service UI/localization did not add a ServiceIssue-specific ledger label/link.

Current `InventoryManager.AllocateFifo` already filters matching warehouse/stock, orders ReceivedAt -> CreationTime -> Id and skips depleted lots. Use it as-is. The historical multi-stock repository overload is reusable; retain current overloads and Sales behavior. Stock identities and actual costs remain Inventory-owned; never recompute old cost from current BOM or current lot prices.

Atomicity: old conventional ABP mutating AppService UoW and a shared DbContext give a transactional route, with a sequential shortage rollback test. That is useful evidence, not proof of every concurrent/failure interleaving. Add explicit coordination consistent with current `SalesOrderOperationCoordinator` style but independent Service keys. Order transitions/payments share an order lock; operations touching the same asset/position require a consistent asset locking/version strategy across Service and CustomerCare. Keep lock acquisition order fixed and hold locks through UoW completion. Cross-order competition for stock still needs Inventory concurrency checks.

Required validation: same/different-key completion replay, conflicting payload, two completions, complete/cancel, two orders on one position, Service vs Sales/Inventory stock competition, multi-lot/depleted-first/repeated-Component allocation, shortage after a prior line allocated, and failure after inventory writes before care commit. Assert orders, lots, balances, allocations, events and reminders all roll back together. Enrich shortages with order/line/material/warehouse and requested/available quantities.

## 6. Warranty/Reminder Integration Assessment

Reusable behavior: old completion uses current enabled policy, snapshots cycle/warning in a new reminder, uses CompletedAt.Date.AddMonths, closes pending old reminders, and appends ServiceOrder-sourced events. It leaves old policy snapshot fields intact. Disabled/missing policy already yields an event with no successor for a performed linked material.

Conflicts: A06 automatically overwrites mapping/reactivates position; no active-Component scheduling check; no common concurrency boundary with manual Warranty actions; no service-level event when no positioned material was performed. A completed Service is a valid historical fact even if the policy was disabled or mapping removed after planning.

S-003 integration contract:

- Service validates performed work and posts stock/cost atomically with completion.
- CustomerCare appends a machine-level Service completion fact; performed positioned materials append replacement facts with immutable Service source/line identity and actual item snapshots. Unperformed materials and other positions (for example Core4..9 when replacing Core1..3) are untouched.
- Close/supersede only the relevant pending cycle with an audited reason and source link. This is an explicit business transition, not rewriting old due/policy/event facts. Historical closed cycles/events stay unchanged.
- Create a successor only for an eligible position mapped to the relevant active Component with an enabled current policy. Preserve the inactive/unmapped exclusion before changing any position state. A mapped `MissingBaseline` external-machine position is different from an inactive/unmapped position: the explicitly performed replacement is a valid new baseline event, so it can establish its first known cycle. Do not copy Warranty's existing-reminder Active-only predicate so literally that this required external-machine case becomes impossible. Missing/disabled policy or inactive/unmapped position does not invalidate actual Service completion and does not create a successor. No automatic remapping or re-enabling is inferred.
- A later explicit mapping/baseline action may establish a new valid cycle; enabling configuration alone never backfills.
- Use the accepted W-008 rule as the reference. Guard manual Warranty replacement completion on the server while Service is enabled, and provide a simple create-Service action from machine/reminder context. Keep the normal Warranty-only flow when Service is disabled.

Keep integration under CustomerCare ownership, with a bounded batch input and no repository queries inside per-position projections. Do not revive synchronous Sales confirmation coupling or redesign the accepted Warranty workflow in this audit.

## 7. Payment Assessment

The old ledger is independent from SalesOrderPayment. Reusing the SalesPaymentMethod enum is vocabulary coupling, not reuse of the Sales payment table; an unnecessary rename would add migration risk. Posted-only sums and unique payment key are reusable foundations.

Required corrections:

- Order-wide lock/transaction for add, void, completion/cancel interactions. Existing payment rowversions cannot prevent two new payments exceeding a cap.
- Replay compares full command facts (order, amount, date, method, reference and other accepted payload), not only order+amount. Return the same prior result for identical replay, including a subsequently voided record without reposting it.
- Void needs explicit reason and actor/time evidence. A cash return is a refund, not a silent Void of a valid payment. The API must not erase a real customer-credit obligation by cancelling the order.
- Before completion, show planned charge and customer advance separately from recognized revenue. At completion use actual performed amount; if an advance exceeds it, expose customer credit and settlement rather than negative receivable or lost money. Cancellation must retain/resolve that obligation according to the documented settlement workflow.
- Add server-paged payment history and customer Service receivable/advance projection; the old API loads an entire per-order payment list. Do not insert/update SalesOrderPayments or mix advances into revenue.

The existing documents already require controlled settlement and a distinct ledger. S-004 must provide a reviewable settlement workflow before acceptance; a full accounting-ledger redesign and generic transfers are out of scope.

## 8. Reporting Assessment

Positive: `EfCoreBusinessRevenueReadRepository` recognizes only Completed Service at CompletedAt, uses stored totals and snapshots, returns source+document identity, and has SQL Count/Skip/Take, Concat and grouped summaries. A posted advance on a draft order contributes no Service revenue. Existing Sales stored procedures were untouched by the historical Service commit.

Corrections before reuse:

- Cost/profit: old labor cost is always zero; add known/missing cost snapshot and provisional-profit indication. Preserve legacy unknown facts; do not recalculate completed services after catalog edits.
- Sales V1: the current Sales aggregate recalculates effective totals; header totals are not inherently obsolete. However the historical report's Paid/Remaining uses gross Posted payments only and knows nothing of Sales V1 refunds. Align meanings with current Sales payment/refund/read contracts and test adjusted, cancelled, returned and refunded orders. Do not modify frozen Sales stored procedures to fit Service.
- Separate document-recognition date filtering from cash/payment-date reporting. Old TotalPaid is payments linked to recognized documents, not a cash ledger for the selected period. Cancelled documents and advances cannot disappear from settlement reporting merely because they contribute zero revenue.
- Consolidated source filter exists in DTO/query but is absent from the rendered filter form/JavaScript; add it. Sorting must match advertised columns with stable tie breakers. Keep date-range parity with Sales and display unknown/restricted costs honestly rather than as definitive zeros.
- Retain `be4a5ad`'s lexical `const summary = () => ...` correction from the final historical tree and verify actual published/minified assets. Explicitly encode textual DataTable values and test report cost/profit permission combinations, including cross-source data exposure.

Acceptance equation: for equivalent recognition scope, consolidated revenue = accepted Sales revenue + Completed Service revenue, with no duplication from payments/refunds or Service source keys. Separate monetary settlement totals must reconcile to the appropriate ledgers.

## 9. Test Coverage Assessment

Source inventory at `220d41c`: **13 Fact methods**, not 13 newly passed tests. No build/test or application runtime was needed to establish these source findings; none was run in this audit. Prior historical UAT/build claims in old AGENT_TASKS are inherited evidence only.

| Layer | Existing exact test(s) | Reuse/disposition | Missing acceptance evidence |
|---|---|---|---|
| Domain (5) | `Should_Complete_Performed_Lines_And_Calculate_Totals`, `Should_Not_Count_Unperformed_Line`, `Should_Enforce_State_And_Completion_Idempotency`, `Material_Quantity_Must_Be_Integer_And_Positive`, `Payment_void_should_be_idempotent` | Retain intent; direct Confirmed completion and zero-labor-cost definitive profit expectations are obsolete. Extend void evidence. | Full transition graph; terminal line protection; immutable snapshot after unrelated edit; missing vs known-zero labor cost; valid actual quantity/zero action |
| Application | No dedicated Service Application test class | AppService workflow exercised through EF host | Enabled/disabled server gate, authenticated permission allow/deny matrix, payload replay/conflict, stale expected version, command validation |
| EF workflow (1) | `Completing_service_should_issue_fifo_and_restart_position_schedule` | Reuse isolated group/customer/component/warehouse/asset fixture; asserts FIFO, schedule/event, advance excluded, void, completed report, shortage rollback | More than one lot/position, Core1..3 vs Core4..9, labor-only, unperformed line, exact retry, race barriers, policy disabled/deleted/current edit, unmapped/inactive state, partial-failure rollback |
| EF schema/permission (2) | `Should_Define_Service_Permissions`, `Should_Map_Service_Tables_Indexes_And_Integer_Quantities` | Useful definition/model assertions; not authorization execution tests | New CHECKs, historical migration identity/upgrade, SQL Server rowversion, uniqueness collisions, non-admin server denials |
| Web source (5) | `Service_should_have_one_primary_menu_and_server_paged_lists`, `Service_create_should_reuse_customer_asset_and_remote_lookups`, `Completion_and_payment_should_use_abp_modals`, `Service_ajax_post_pages_should_send_antiforgery_tokens`, `Reports_should_use_server_paging_and_keep_sales_report_separate` | Reuse useful guards from final tree including both hotfixes; string assertions alone do not prove runtime behavior | Authenticated missing-token POST rejection; valid token success; negative permissions; page2/filter/sort; Select2 continuation; selected inactive items; safe encoding; real date/int/money binding; desktop/mobile/minified static asset smoke |
| Payments/reports/concurrency | Payment and report assertions are embedded in the single EF test; no dedicated concurrency suite | Reuse correct advance exclusion and source assertions | Parallel payments; complete/cancel/refund obligations; changed payload replay; ledger reconciliation; actual mixed Sales+Service totals with revisions/refunds; missing cost flags |

Future S-006 must test SQL Server/Redis interleavings on explicitly authorized VPL/isolated test copies and preserve existing fingerprints. SQLite source/model tests are not proof of live distributed locking or SQL Server migration compatibility. Known combined Web testhost leak stays a separate issue; use bounded focused groups and report exactly what ran.

## 10. Recommended S-001..S-006 Plan

The plan is sequential and deliberately keeps one Service list, one order detail/edit flow, and work configuration reachable from the Service context. No extra screen for a flag, policy entity, sync engine, or accounting implementation detail. Machine selection supplies customer context; Material and Labor rows remain understandable to an operator.

### S-001 - Foundation, Historical Schema Compatibility And Work Catalog

- Status/dependency: READY after this completed audit and W-GATE DONE. Not started in this turn.
- Exact scope: default-disabled Service guard; preserve number sequence conventions using current BusinessCodeGenerator; recover historical schema identity and passive compatibility mappings; work code/name/unit/default price/optional standard cost/status; minimal permissions/menu, one paged work list and ABP edit modal. No executable order posting, payment or report workflow yet.
- Likely files: `src/VPureLux.Domain.Shared/Service/*`, `Domain/Service/*` and `Application.Contracts/Service/*` for foundation/schema definitions; `Application/Service/ServiceWorkAppService.cs`; current BusinessCodes contracts; `EntityFrameworkCore/Service/*`, current DbContext/registration/snapshot and original Service migration pair; Web `Pages/Service/Works*`, `WorkModal*`; permissions/localization/enablement configuration. Merge into current files, never replace old full shared files.
- Migration expectation: original ID recovery is not a new CREATE-table migration on existing installations. Add reviewed forward nullable/compatible work fields only after baseline model alignment. Preserve old records and current Sales V1 metadata. Schema application is a separate authorized action.
- Tests: work validation, default-price/cost snapshot independence, true sorting/page2, permission deny/allow and flag, number uniqueness/seed query, model compatibility and empty/legacy schema scripts; no historical business DML. Preserve W-008 uncommitted fixes until separately captured in Git.

### S-002 - Order, Material/Labor Lines And Operator Workflow

- Status/dependency: HOLD, depends on S-001; promote only when predecessor is DONE.
- Exact scope: one asset per order, immutable customer/item facts, identity-preserving draft edits, actual-state graph, expected-version contracts, Material/Labor exclusivity, required reason for cancellation, simple Create/Edit/Details and server-paged list. Confirm/Start record planned workflow; no stock or recognized revenue yet. Completion action stays disabled until S-003 is complete.
- Likely files: `Domain/Service/ServiceOrder*`, `Application.Contracts/Service/ServiceInputs.cs`, DTO/interfaces; `Application/Service/ServiceAppService.cs`; Service repositories/configurations; `Web/Pages/Service/{Index,Create,Edit,Details}*`, `EditForm.js` and role definitions.
- Migration expectation: additive Service-only fields/guards as needed, constrained by existing legacy rows. No Sales/Catalog table changes; do not replay original CreateTable operations.
- Tests: allowed/forbidden transitions, terminal edit protection, note-only save leaves line snapshots and IDs unchanged after catalog edits, per-field intentional edits/audit, stale browser saves, canceled asset selection, int binding, retained inactive selections, remote lookup continuation, modal/API authorization and negative antiforgery.

### S-003 - Atomic Completion, FIFO And CustomerCare

- Status/dependency: HOLD, depends on S-002.
- Exact scope: actual quantities (zero = unperformed), shared order/asset concurrency plan, canonical idempotency/hash, current Inventory allocator + bounded batch lots/balances, ServiceIssue=6, actual-cost snapshots, CustomerCare completion command/events and eligible successors, server guard against manual replacement bypass while Service enabled. Stage validation before any side effect.
- Likely files: Service order/application/command contracts; current Inventory enums, repository contract/batch query/balance integration and ledger source formatter; new focused CustomerCare integration service under Warranty ownership; `WarrantyAppService` guard only where necessary; completion UI; tests across Service/Inventory/Warranty.
- Migration expectation: Service-only completion audit/hash/source-link facts if required. Reuse existing asset/event/reminder schema and source enums where possible; additive companion fields only if necessary and reviewed. No historical schedule rewrite/backfill.
- Tests: full current-policy/disabled/missing/inactive/unmapped matrix, mapped external position without an old baseline receives its first known cycle from actual replacement, Core1..3 isolation, labor-only/no-position events, exact retry and conflicting replay, deterministic concurrent completion/cancel/Sales-stock/asset-care actions, all-or-nothing shortage and post-stock care failure. Maintain Sales/Inventory/BOM regression coverage.

### S-004 - Payments, Advances And Receivables

- Status/dependency: HOLD, depends on S-003.
- Exact scope: independent Service ledger; order-level payment/state serialization; Posted/Void with explicit audit; full payload replay; per-order and customer advance/receivable/credit summaries; controlled cancellation/excess-advance settlement that distinguishes erroneous receipt void from actual cash refund. No Sales payment mutation or general accounting redesign.
- Likely files: `Domain/Service/ServicePayment*` and monetary summary rules; payment contracts/AppService/repository/configuration; Service payment/history modal and paged list; any narrowly required Service-only refund/settlement record and permissions.
- Migration expectation: additive Service payment audit/hash fields and, if required for factual refunds, a separate Service-owned ledger record. Never repurpose SalesOrderPayment or erase posted historical receipts. Present exact settlement schema/workflow before its implementation if it extends existing approved contracts.
- Tests: pre-completion advance excluded from revenue, partial/paid/credit summaries, repeat/void/conflicting key, concurrent overpayment, cancel/payment and completion/payment races, lower actual completion amount after advance, refund vs void audit, customer aggregate and unchanged Sales money records.

### S-005 - Service And Consolidated Reports

- Status/dependency: HOLD, depends on S-003 and S-004.
- Exact scope: Completed-only Service revenue, actual FIFO cost and labor-cost completeness, provisional profit, separate settlement meaning, source-filtered consolidated read model; read current effective Sales totals/ledgers correctly. Keep existing Sales stored procedures behavior unchanged.
- Likely files: Reports contracts/query services/read repositories; Service cost completeness snapshots/read DTO; `Web/Pages/Reports/ServiceRevenue*`, `BusinessRevenue*`, shared report view; permissions and focused test fixtures.
- Migration expectation: preferably no schema beyond completed snapshots from earlier tasks; Service read indexes only if measured need. Any new view/procedure is additive and reviewed, not replacement of Sales report procedures.
- Tests: true mixed-source reconciliation, completed/cancelled/advance boundaries, Sales adjustment/cancel/return/refund cases, edited templates cannot change historical report facts, missing/zero labor cost, independent cost/profit permissions, date boundaries, source filter, sort/page2, publish/minifier behavior.

### S-006 - UAT, Reconciliation And Authorized Rollout

- Status/dependency: HOLD, depends on S-001..S-005 DONE.
- Exact scope: operator acceptance, empty/legacy upgrade rehearsal, isolated test fixtures, fingerprint/revenue/inventory/payment/care reconciliation, publish asset/UI checks, release/rollback instructions. Deployment requires its own explicit task/authorization.
- Likely files: Service UAT/evidence and operations docs, `AGENT_TASKS.md`, isolated test harnesses, publish/release scripts only where needed.
- Migration expectation: rehearse reviewed chain on test targets with/without original Service migration and Sales V1. No automatic prod migration, no legacy fixes/backfill and no destructive Down. Validate existing Service records before enabling runtime.
- Tests: full operator external-machine 1..9 example with only 1..3 replaced plus Labor, shortage/retry/concurrency, money and reports, source links, history/notifications, authenticated roles/antiforgery, desktop/mobile and all unaffected Sales/Inventory/BOM/report regressions. Use focused Web groups with resource limits.

### Audit Verification And Handoff

- Read-only preflight completed; local Git/file inspection only. `git diff --check` passed; document section/status checks passed; static inventory counted 5 Domain + 3 EF + 5 Web Fact methods, and the original migration pair is unchanged between the two inspected commits. No new build/full suite pass is claimed by this audit.
- Documentation commit scope is AGENT_TASKS, this audit, CustomerCare operations, module map and the related W-008 acceptance/evidence summary. Application/tests and raw local W-008 evidence/harness files remain outside that commit; the document commit does not make the accepted W-008 implementation reproducible from HEAD alone.
- Original W-008 fixes/evidence remain local changes, including the two unrelated user-owned files `docs/VPureLux_Sales_Flow_Design_Review_for_Codex_5_6_Sol.docx` and `docs/html.txt`; neither may be staged by this task.
- S-001 READY is permission to claim the next planned task, not a claim that Service is complete or authorization to run migrations/deploy in this audit. Stop after recording the audit.
- Database touched: NO. Production touched: NO. Migration generated/applied: NO. Service implementation started: NO. Push performed: NO.
