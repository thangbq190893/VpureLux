# S-004 Service Payments And Settlement

Date: 2026-09-07 (Asia/Saigon). Branch: `codex/warranty-release-review`.
Starting HEAD: `38cb1451b9e4acf2ee805eb66351df95e4b0098a`.
Status: COMPLETE, local source sealed at `2a93dfb847201fe87749d6dc7d4b267db16fad4c` (`feat(service): add payments and settlement`, 37 application/test/migration files).
No push, deployment, database migration execution, VPL or production access is authorized by this task.

## Ownership And Recognition

Service owns `ServicePayment` and `ServiceRefund`. No Sales payment/refund table, command, report or stored procedure is changed. Historical table names, payment method vocabulary and enum numeric values remain unchanged. `ServicePaymentStatus.Posted=1`, `Voided=2`.

- Payment records actual customer money received. Before completion it is an advance, not revenue.
- Revenue is zero for Draft, Confirmed, InProgress and Cancelled. Completed revenue is the persisted S-003 `TotalRevenueAmount`, never payment timing or current catalog prices.
- Void corrects an incorrectly recorded receipt. It requires a reason, actor, timestamp, expected payment concurrency stamp and idempotency key. The original row and amount remain; only explicit void facts/status are appended to that entity.
- Refund is an actual customer cash return, recorded as a separate immutable Service-owned row. It has no edit, void, delete or soft-delete API. Every row is factual/posted; no unnecessary refund state machine was introduced.
- Recording a payment/refund documents an external cash/bank event; this application does not initiate a bank transfer.
- No automatic refund, cancellation fee, credit transfer, customer wallet or accounting general ledger is implemented.

## Canonical Projection

`ServiceMoneySummary.Projection` is the single money formula used by the domain calculation, write guards and EF queries. AppService/UI map its values without recalculating balances. Money uses decimal, scale two; commands reject missing/nonpositive amounts, excessive precision or values outside decimal(18,2), rather than silently rounding a cash fact.

Definitions:

```text
GrossPosted   = SUM(ServicePayment.Amount WHERE Status = Posted)
GrossRefunded = SUM(ServiceRefund.Amount)
NetPaid       = GrossPosted - GrossRefunded

Before completion:
  PlannedTotal     = SUM(persisted line UnitPrice * PlannedQuantity)
  AdvancePaid      = NetPaid
  PlannedRemaining = max(PlannedTotal - NetPaid, 0)
  AdvanceExcess    = max(NetPaid - PlannedTotal, 0)
  Receivable      = 0
  Revenue         = 0

Completed:
  ActualTotal     = persisted S-003 TotalRevenueAmount
  Receivable      = max(ActualTotal - NetPaid, 0)
  CustomerCredit  = max(NetPaid - ActualTotal, 0)
  RefundDue       = CustomerCredit

Cancelled:
  Charge obligation = 0
  Revenue           = 0
  Receivable        = 0
  CustomerCredit / RefundDue = NetPaid
```

Before completion, an existing advance excess is also exposed as order-specific CustomerCredit/RefundDue and may be settled, for example after an explicit Draft price/quantity reduction. No historical amount is repaired. `ActualTotal` is null before completion/cancellation, not a fabricated recognized total. Completed/Cancelled `AdvancePaid`, `PlannedRemaining` and `AdvanceExcess` are zero.

Normal commands preserve nonnegative NetPaid. If an inconsistent legacy/imported ledger would make net cash negative, reads flag `HasInconsistentLedger` and display zero rather than a negative customer-credit convention; writes fail closed with SERVICE_032. Gross facts remain visible for reconciliation and are never repaired automatically. Customer summary includes the number of inconsistent orders.

Customer Service summary sums each order's receivable, advance and credit in SQL. It never offsets one order's receivable against another's credit and never mixes Sales. These are separate operational totals, not components to add into a single balance.

## Acceptance Rules

- Draft/Confirmed/InProgress: new payment may use only current PlannedRemaining.
- Completed: new payment may use only current Receivable.
- Cancelled: new payment rejects, even when an old exact replay remains valid.
- Existing credit does not permit collecting more. A refund cannot reopen collection room beyond the actual obligation.
- Refund amount may not exceed current RefundDue; partial settlement is supported.
- Void may not leave GrossPosted below GrossRefunded. This additional guard is necessary because a receipt can be voided after refunds have already occurred.
- Financial records do not change the order header, quantities, stock, completion snapshots or replacement schedules.

Examples:

| State | Planned | Actual | Posted | Refunded | Advance | Receivable | RefundDue | Revenue |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| InProgress | 10 | - | 5 | 0 | 5 | 0 | 0 | 0 |
| Completed | 10 | 6 | 8 | 0 | 0 | 0 | 2 | 6 |
| Completed | 10 | 6 | 8 | 1 | 0 | 0 | 1 | 6 |
| Completed | 10 | 6 | 8 | 2 | 0 | 0 | 0 | 6 |
| Completed | 10 | 12 | 5 | 0 | 0 | 7 | 0 | 12 |
| Cancelled | 10 | - | 5 | 0 | 0 | 0 | 5 | 0 |
| Cancelled | 10 | - | 5 | 5 | 0 | 0 | 0 | 0 |

The Actual 12 / Planned 10 example is a projection/legacy boundary test, not permission to expand S-003. Current S-003 caps actual quantity at planned quantity and uses unchanged prices, so its normal completion command cannot produce that example. S-004 deliberately does not change those rules.

The old proposed technical DOCX required settlement before cancellation. The current explicit task supersedes that proposal: cancellation commits immediately, preserves posted advances and exposes RefundDue. Accounting settles separately. There is no new RefundPending order status.

## Coordination And Transactions

All AddPayment, VoidPayment and Refund mutations reuse `ServiceOrderOperationCoordinator` and its actual existing lock name `VPureLux:ServiceOrder:{id:N}`. It is the same boundary used by Update, Confirm, Start, Cancel and Complete. The illustrative `service-order:{id}` spelling in the task is not a second lock.

Sequence: acquire distributed lock, begin a new transactional UoW, reload authoritative order and monetary facts, validate replay/cap/state, persist money and business audit, commit/dispose, release lock. Payment rowversion alone is not used as an aggregate-wide cap. Completion additionally retains S-003 asset coordination; money does not introduce another asset or Sales lock.

Money reads the header with repository tracking disabled. ABP otherwise marks the tracked FK principal changed when a payment is inserted, unnecessarily changing the ServiceOrder concurrency stamp and making a pending legitimate Complete/Cancel stale. No-tracking avoids that side effect without weakening S-003 optimistic version checks. Money read projections also use AsNoTracking.

Cancel/Complete do not modify payment records. The same ledger is interpreted against zero/actual obligation after the order transaction commits. Cancel versus Complete remains the S-003 single-terminal-transition behavior.

Unique indexes remain the database backstop for payment/refund/void keys across different orders. Predictable duplicate/concurrency failures are translated while preserving inner exceptions and transactional rollback. External SQL Server/Redis multi-process behavior is an S-006 rehearsal gate, not proven by an in-memory lock.

## Replay And Legacy

New payment/refund hashes are SHA-256 over versioned JSON string arrays: operation kind, OrderId, fixed two-decimal invariant amount, UTC timestamp ticks, method, trimmed reference and trimmed note/reason. JSON avoids delimiter collisions. Equivalent offsets represent the same instant. Keys, expected concurrency stamp and server-assigned audit timestamp are not mutable business payload.

- Same key and same canonical facts return the existing record without another row or business audit.
- Same key and different facts conflict, including changed date/method/reference/note, not just amount.
- Exact AddPayment replay after Void returns the original Voided state. It never reposts or resurrects a receipt.
- Void identity includes payment ID, key and canonical reason. Exact replay returns the original actor/time/reason; a changed reason or key cannot rewrite it. A stale expected version rejects a first void, while exact completed replay does not require the old version still to match.
- Legacy payments without RequestHash compare only their complete persisted raw fields for an exact Add replay. No hash is filled, no date offset inferred, and no amount/status is changed. Mismatches reject. This also works for an old Voided receipt without resurrecting it.
- Old voids lacking reason/time/key remain unknown/legacy in history. No guessed reason or refund row is created. A new conflicting void request cannot fill those gaps.
- New UTC dates display in UTC+07; legacy dates are displayed as their existing raw wall-time values, not retrospectively shifted.

## ABP Layers And UI

- Domain: money commands, payment/refund invariants and canonical summary expression.
- Contracts: independent `IServicePaymentAppService`, nullable required amount inputs, paged history DTOs, per-order/customer summaries.
- Application: permission/feature checks, existing coordinator, authoritative reads, domain guards, audit and mapping. No DbContext in PageModels.
- EF: decimal grouped sums and LEFT JOINs for planned/posted/refunded facts; customer totals aggregate in SQL. No loading all orders or per-row dependency query.
- UI: one financial section in existing Service Details, two DataTables histories and ABP Money/Void modals. No extra menu or accounting workflow screen.
- Before completion show planned amount, advance and planned remaining; after completion show actual, posted, refunded and receivable/credit; cancellation shows retained money and remaining refund.
- `Service.ManagePayments` is the recovered permission name for all three money commands. Read summaries/histories use existing Service.View. Feature-disabled mutation API requests fail even if permission is granted. No new refund role model was invented.
- Razor forms carry antiforgery. Amount text parsing is local to financial forms and accepts strict vi-VN grouping (`1.500.000,50`) plus invariant (`1500000.50`) and ungrouped decimal commas. Malformed grouping, negative/zero or extra precision reject. Existing Sales/global binders are untouched.
- Dates use exact datetime-local syntax with explicit UTC+07. Money display preserves nonzero cents; it does not round recorded amounts to whole VND. Untrusted history text is encoded.
- Payment/refund histories execute Count/filter/whitelisted sorting/Skip/Take server-side. PaymentDate/RefundDate, CreationTime and Id provide stable ordering. Unsupported amount/reference sorts are not advertised. Payment status sorting is supported. Page 2 is covered for both histories.

## Audit

New payment: explicit RecordedBy/RecordedAt plus original business date/method/reference and amount. Void: explicit VoidedBy/VoidedAt/VoidReason and replay facts. Refund: explicit actor/time, amount, business date/method/reference/reason. ABP creation/concurrency audit remains available.

`BusinessAuditManager` writes a Service PaymentPosted/PaymentVoided/RefundPosted event in the same transaction as the monetary fact, with order identity, fact ID, command key, user and relevant business fields. Replay does not append another event. Injected failure after audit persistence proves money and audit both roll back.

## Migration

Generated only: `20260907021302_AddServicePaymentSettlement`.

- Eight nullable columns on AppServicePayments: RequestHash, RecordedAt, RecordedBy, VoidedAt, VoidedBy, VoidReason, VoidIdempotencyKey, VoidRequestHash.
- New AppServiceRefunds with decimal(18,2) positive Amount check, immutable business/audit facts, Restrict FKs and unique idempotency index.
- Filtered unique VoidIdempotencyKey and ordered history indexes. Original Service tables/index names and enums are retained.
- Up contains only eight AddColumn, one CreateTable and five CreateIndex operations. No SqlOperation, business INSERT/UPDATE/DELETE/MERGE, defaults fabricating legacy facts, backfill or historical table recreation. All non-payment existing entity metadata is unchanged.
- Generated migration and drift tooling use `127.0.0.1,1 / S004_OFFLINE_ONLY`, a deliberately unusable connection. No database connection, DbMigrator or database update is executed. Down is only for disposable databases, never an operational rollback of cash history.

## Verification And Follow-up

Final Release solution build: PASS, 0 errors, 1 existing Scriban NU1903 warning in HttpApi.Client.ConsoleTestApp. No dependency upgrade was made. Earlier rebuilt projects also emitted pre-existing OpenIddict nullable/CS7022 warnings. EF `has-pending-model-changes`: NONE. `git diff --check` and `node --check Pages/Service/Payments.js`: PASS.

| Isolated test group | Passed/total | Local TRX |
|---|---:|---|
| Domain Service + Inventory | 49/49 | s004-domain-final.trx |
| Application Service | 11/11 | s004-application-final.trx |
| EF Service + Sales + Inventory + Warranty/CustomerCare | 226/226 | s004-ef-final.trx |
| Web money HTTP workflows + standalone amount parser | 11/11 | s004-web-money-final.trx |
| Web existing order workflow, excluding Money/Completion | 9/9 | s004-web-order-final.trx |
| Web completion | 6/6 | s004-web-completion-final.trx |
| Web work catalog + order UI source guard | 6/6 | s004-web-work-source-final.trx |
| Web Warranty | 13/13 | s004-web-warranty-final.trx |

The EF group includes 11 deterministic money race cases: duplicate/different-key payments, both Payment/Cancel orderings, both Payment/Complete orderings, duplicate/different-key refunds, both Void/Refund orderings, and Refund/new-Payment. All pass. The cap-5 concurrent 4+4 case posts only one 4; Cancel-first rejects payment, Payment-first preserves it as refund due; Complete-first validates actual receivable, Payment-first retains the receipt and derives excess credit. Refund races cannot exceed credit. Legacy exact replay, invalid ledger fail-closed, rollback after audit insertion, database aggregation translation and Service-only migration operations are covered.

HTTP tests cover ManagePayments deny/allow, feature-disabled denial, missing/valid antiforgery, required void/refund reason, stale version, encoded hostile input, vi-VN fractional money, and payment/refund history page 2. Pure parser cases run without a Web host. The injection-only audit repository has DisableConventionalRegistration and is explicitly registered only by its EF test fixture.

Browser verification: real headless Chrome at 1440x960 and 390x844 against the existing Web testhost's isolated SQLite fixture through a temporary loopback proxy. Payment `1.500.000,50`, reasoned Void and partial Refund all returned 204; both histories returned their two expected page-2 rows. After refund, Actual=1m, GrossPosted=3m, GrossRefunded=620k, RefundDue=1.38m. No page JavaScript errors or horizontal document overflow. Payment/refund/void screenshots were inspected; money labels were corrected and the date column wraps rather than overlapping the next header in an empty history. Existing duplicated menu contributors belong to the test fixture, not a new production menu. The temporary proxy has been stopped; no port 5099 listener remains.

Raw local evidence is under `artifacts/s004/` and `artifacts/s004-browser/`, ignored by Git; source-controlled tests are the reproducible evidence. All database use was disposable SQLite fixtures. No VPL, production, SQL Server connection, Redis connection or external service was used.

### Bounded Web Failure, Not A Full-Suite Pass

An earlier combined Service Web run (`s004-web-final.trx`) aborted at the bounded 1.5 GiB testhost limit with 13 passed and 9 failed results. HTTP 500/missing-token fallout preceded OutOfMemory during host construction; the exact testhost PID was stopped. Do not report the combined/full Web suite as passing. Moving eight pure parser cases out of the Web-host fixture and running bounded groups produced the successful results above; this does not fix the known combined testhost lifetime leak.

Exact results delivered by that aborted run (all names are in VPureLux.Pages except AuditApiTests):

- PASS: ServiceWorkWebTests.Service_catalog_should_render_and_serve_real_page2_filter_sort; Service_disabled_should_hide_menu_and_block_pages_and_api; Service_permission_should_deny_page_and_api_then_allow; Service_modal_should_reject_missing_token_and_accept_valid_create_and_edit.
- PASS: ServiceWorkUiSourceTests.Work_ui_should_use_abp_modals_server_paging_and_honest_nullable_cost; ServiceOrderUiSourceTests.Order_ui_should_use_server_paging_abp_dialogs_and_safe_rendering; WarrantyPagesTests.External_asset_workflow_should_preserve_positions_and_use_application_services; VPureLux.Api.AuditApiTests.Controller_Should_Delegate_To_Authorized_App_Service_And_Expose_No_Mutations.
- PASS: ServiceOrderWebTests.Completion_modal_validates_integer_actual_date_and_antiforgery for quantity/date pairs (1, 2026-09-07T12:45), (1, 2026-09-07T01:30), (0, 2026-09-07T12:45), (1,5, 2026-09-07T12:45), (1, 07/09/2026). These include expected validation rejects, not accepted fractional quantities.
- FAIL: that Completion test for (2, 2026-09-07T12:45); ServiceOrderWebTests.Confirm_should_reject_missing_token_and_accept_current_token; ServiceOrderWebTests.Money_payment_and_refund_modal_bind_vi_money_and_enforce_csrf_permissions.
- FAIL: the former host-based Service_money_parser_is_strict_and_culture_independent cases for 1.500.000, 1.500.000,50, 1500000,50, -1, 0, invalid. It now belongs to standalone ServiceMoneyParsingTests; all eight cases pass there.

### Reproduce Without External Data

Build first: `dotnet build VPureLux.slnx -c Release --no-restore -m:2`. Run each test project separately with `-c Release --no-build -m:1`; never rebuild its output while testhost is running. Filters:

- Domain: `FullyQualifiedName~Service|FullyQualifiedName~Inventory`.
- Application: `FullyQualifiedName~Service`.
- EF: `FullyQualifiedName~Service|FullyQualifiedName~Sales|FullyQualifiedName~Inventory|FullyQualifiedName~Warranty|FullyQualifiedName~CustomerCare`.
- Web, separate processes: `FullyQualifiedName~Money`; `FullyQualifiedName~ServiceOrderWebTests&FullyQualifiedName!~Money_&FullyQualifiedName!~Completion_`; `FullyQualifiedName~ServiceOrderWebTests.Completion_`; `FullyQualifiedName~ServiceWork|FullyQualifiedName~ServiceOrderUiSourceTests`; `FullyQualifiedName~Warranty`. Use DOTNET_GCHeapHardLimit=0x60000000 and `--blame-hang-timeout 90s`, and bound/stop the exact testhost on OOM rather than retrying a combined suite.
- EF drift: from src/VPureLux.EntityFrameworkCore, override ConnectionStrings__Default to the unusable offline target above, then `dotnet ef migrations has-pending-model-changes --no-build --configuration Release`. Do not run database update or DbMigrator.

Findings fixed during verification: tracked FK principal stamp mutation; obsolete S-001 assertion that no Void method existed; SQL Server translation of nested monetary aggregation; nullable LEFT JOIN projection materialization. Keep the failed TRX runs alongside successful reruns rather than hiding them.

S-004 is DONE; S-005 is READY and unclaimed. S-005 remains outside this task: no Service/consolidated report UI, accounting redesign, generic wallet, cross-order transfer or Sales reporting change. S-006 remains HOLD and must rehearse real SQL Server/Redis races, applied legacy schemas, query plans/index efficiency, reconciliation and operator acceptance on separately authorized targets. No external rollout is implied by local completion. No push occurred and the two user-owned documents remain untracked/unstaged.
