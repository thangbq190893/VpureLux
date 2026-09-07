# Sales Adjustment Forensic 001

Date: 2026-09-07 (Asia/Saigon)
Scope: source-level forensic investigation only. No production, VPL, VPS, SQL, migration, deployment, or business-data operation was performed.

## Decision

**DATA INTEGRITY RISK: AT RISK, NOT A CONFIRMED SPLIT-BRAIN POSSIBILITY.**

The released source has one explicit transactional coordination boundary for an adjustment. Source evidence shows that Sales effective-line changes, Inventory/FIFO writes, CustomerCare reconciliation, revision status, and the local business audit event are all produced before `SalesOrderOperationCoordinator.ExecuteAsync` calls `CompleteAsync`. A failure before `CompleteAsync` should roll back the shared `VPureLuxDbContext` transaction.

This is not a production-data proof: no read-only production inspection was authorized, and the regression suite has no injected-failure test after a revision Inventory write or inside CustomerCare reconciliation. Until those failure tests exist and an operator demonstrates the exact UI sequence, the risk cannot be marked SAFE.

## Likely Cause Of The Reported "Success"

1. **Most likely: draft-save versus apply confusion.** `AdjustModel.OnPostSaveAsync` calls `UpdateRevisionAsync`, then displays `Sales:AdjustmentSaved`: "Da luu noi dung dieu chinh. Hay kiem tra phan anh huong truoc khi ap dung." That intentionally leaves the effective order unchanged. Only `OnPostApplyAsync` calls `ApplyRevisionAsync` and redirects to Sales Details with `Sales:AdjustmentApplied`.
2. **Likely UI/interaction gap:** the form has two submit actions, but `OnPostApplyAsync` intentionally ignores posted `UpdateInput`. JavaScript disables Apply after any edit, requiring Save and then Apply; without JavaScript, or if an operator assumes one click applies the edited fields, Apply uses the last persisted draft rather than the current unsaved form values.
3. **Less likely, unproved from source:** a deployed artifact/configuration mismatch or a browser path not represented by the Razor form. The deployed Service release source is recorded as `b0bf197e8525acb2f254995af70b3da8a397a9f8`; no production inspection was authorized to compare its active behavior with this source.

No source evidence supports an Apply request returning business success while leaving its persisted revision in Draft. `ApplyRevisionAsync` returns its DTO only after `revision.Apply(...)` sets `Status = Applied` and after it changes the effective order.

## Exact Call Chain

The adjustment Razor page does not use ABP AJAX for mutation. It submits antiforgery-protected Razor Page forms.

1. `Pages/Sales/Details.cshtml` opens `Pages/Sales/Adjust.cshtml`.
2. `AdjustModel.OnPostStartAsync` -> `ISalesPostConfirmationAppService.OpenRevisionAsync` -> `SalesPostConfirmationAppService.OpenRevisionAsync` -> `SalesOrderOperationCoordinator.ExecuteAsync` -> create/insert `SalesOrderRevision` as Draft.
3. `AdjustModel.OnPostSaveAsync` -> `UpdateRevisionAsync` -> validates active products/BOM/price permission -> mutates Draft revision lines -> `_revisions.UpdateAsync(..., autoSave: true)` -> returns to Adjust page with the draft-save message.
4. `AdjustModel.OnPostApplyAsync` -> `ApplyRevisionAsync` with a fresh key -> `SalesPostConfirmationAppService.ApplyRevisionAsync` -> coordinator lock and transaction.
5. `ApplyRevisionAsync` loads the Draft revision and confirmed order, checks modification/install locks, active revision, idempotency, and warehouse-return prerequisites.
6. `ApplyLineAsync` performs only delta work, mutates `SalesOrder.EffectiveLines`, and records revision-line facts.
7. `SalesRevisionCustomerCareReconciler.ReconcileAsync` updates only affected pending machine assets or creates new pending assets/components.
8. `SalesOrder.RecalculateEffectiveTotals`, payment summary read, `SalesOrderRevision.Apply`, order/revision persistence, coordinator `CompleteAsync`.
9. Only then does `OnPostApplyAsync` set `DetailsModel.SuccessMessage = Sales:AdjustmentApplied` and redirect to `Pages/Sales/Details.cshtml`.

The HTTP API controller maps the same AppService methods at `SalesPostConfirmationController`: `POST orders/{id}/revisions`, `PUT revisions/{id}`, `POST revisions/{id}/returned-goods`, and `POST revisions/{id}/apply`. It has no independent business logic.

## Revision And Effective-Order Model

- State machine: `Draft -> Applied` or `Draft -> Cancelled`; `SalesOrderRevision.Apply` rejects a different retry key once applied.
- `SalesOrder.EffectiveLines` is the in-memory projection of `SalesOrderLine.IsEffective = true`.
- Price/quantity edits update the existing effective line via `ApplyEffectiveRevisionLine`; removals call `RemoveEffectiveRevisionLine`; product replacement supersedes the old line and adds a new effective line; additions add a new effective line.
- `SalesOrderConfiguration` enforces a filtered unique index on effective `(SalesOrderId, LineNo)` values. Repository Detail reads include all lines; `SalesApplicationMapper` projects the aggregate effective lines, so Details/List read the changed effective facts after a new request.
- The page redirect after Apply is a new Details request, not a stale in-memory page reload.

## Write Boundaries And Side Effects

`SalesOrderOperationCoordinator.ExecuteAsync` obtains `VPureLux:SalesOrder:{id}` and starts `Begin(requiresNew: true, isTransactional: true)`. The post-confirmation AppService methods disable the interceptor UoW specifically so this coordinator is the authoritative boundary.

`autoSave: true` calls in the revision/order repositories flush `SaveChangesAsync` inside that already-open transaction; they do not invoke `CompleteAsync` and do not create an independent transaction. The repositories and Inventory balance/reconciliation repositories use `IDbContextProvider<VPureLuxDbContext>`, therefore participate in the coordinator's ambient unit of work.

| Change | Inventory timing | Sales effective facts | CustomerCare |
|---|---|---|---|
| Price only | No issue/reversal branch | Existing effective line is updated | Only if the product is a machine; normally no asset count change |
| Quantity increase | `PostIssueAsync` issues positive delta by FIFO | Same line updated with new quantity/cost | Pending unit assets may be added |
| Quantity decrease/remove | Apply blocked until Warehouse marks required revision line returned; `PostReversalAsync` restores original allocation facts | Line updated or superseded | Pending excess/removed assets cancelled |
| Product replacement | Reversal old allocation then issue new BOM | Old line superseded; replacement effective line added | Pending old asset cancelled; new pending asset created |
| Add product | FIFO issue only for new line | New effective line added | Pending assets only for added machine products |

Reversal derives its ledger from factual original and prior revision issue allocations, subtracts previously reversed quantities, and uses deterministic revision-line idempotency keys. Issue and reversal are inserted without a separate completion boundary. The same deterministic keys prevent a second inventory transaction on an exact retry.

Business audit is not manually written by the AppService. `SalesOrderRevision.Apply` raises `SalesOrderRevisionAppliedEvent`; `BusinessAuditEventHandler` handles that local event. It is part of the unit-of-work completion path, so an audit failure should also prevent the transaction from completing.

## Failure And Retry Assessment

| Failure point | Expected persisted result from source | Proof strength |
|---|---|---|
| Validation/return prerequisite before delta work | No Sales, Inventory, or CustomerCare write | Direct control flow |
| FIFO/Inventory failure during issue/reversal | Coordinator does not reach `CompleteAsync`; all prior writes roll back | Shared UoW source evidence; shortage rollback regression exists |
| Order/revision persistence failure after issue/reversal | Same transaction rolls back issue/reversal, lots, balances, and effective facts | Shared UoW source evidence; no injected late-persist test |
| CustomerCare reconciliation failure after Inventory work | Same transaction should roll back Sales/Inventory/CustomerCare writes | Shared UoW source evidence; no injected reconciler-failure test |
| Audit local-event failure at completion | Completion should fail and roll back | Framework/UoW design and source event path; no targeted test |
| Exact Apply replay | Applied revision validates same key; deterministic inventory keys are reused | Quantity-increase replay regression exists |
| Browser retry after lost response with a new page-generated key | Already-Applied revision rejects the differing key, so it cannot repost Inventory | Direct `SalesOrderRevision.Apply` behavior |

## Explicit Answers

| Question | Answer |
|---|---|
| A. Can price-only adjustment alter Inventory? | **NO** in the current Apply path. `quantityDelta == 0` and same product bypass both Inventory branches. |
| B. Can failed quantity increase leave Inventory issued? | **NO by source design**: it remains in the coordinator transaction. Not independently fault-injection-tested after the issue write. |
| C. Can failed decrease/replacement leave stock returned? | **NO by source design**: reversal and later work share the coordinator transaction. Not independently fault-injection-tested after reversal. |
| D. Can CustomerCare change while effective Sales stays unchanged? | **NO by source design**: reconciliation runs before the same transaction completes. Not injected-failure-tested. |
| E. Can retry duplicate Inventory side effects? | **NO for the implemented retry paths**: revision status/key plus deterministic Inventory idempotency keys protect it. |
| F. Can API return success before revision is effective? | **YES for Open/Update draft endpoints; NO for Apply.** A 200/redirect after Save means the revision draft was stored, not the effective order changed. |

Payments are read to calculate applied remaining/refund facts and are not mutated by Apply. The pre-existing Sales `GrossPosted`/`NetPaid` projection defect remains separate and untouched.

## Existing Tests And Gaps

`SalesWorkflowTests` covers price-only total/payment with no Inventory transaction, quantity increase delta plus exact replay, decrease requiring Warehouse confirmation plus effective quantity/cost, product replacement, removal/addition delta isolation, pending-machine reconciliation, and insufficient-delta rollback. These tests do assert final effective Sales state together with Inventory in the primary normal/shortage cases.

Gaps:

- no browser/Razor integration test proves Save cannot be confused with Apply or that an unsaved Apply cannot silently use older persisted data;
- no test injects a failure after an issue/reversal but before final Sales persistence;
- no test injects a `SalesRevisionCustomerCareReconciler` failure after Inventory writes;
- no test injects a business-audit local-event failure at unit-of-work completion;
- no source-level test proves the deployed production artifact/UI sequence reported by the operator.

One focused SQLite test command was started for the six adjustment scenarios but its host ended after the tool's bounded output window without a final summary being captured. It is therefore **not counted as passing evidence** in this report. No test data left the local test host.

## Recommended Next Task

Do not alter production data. Create a separate fix task only after reviewing this forensic report. That task should first add failing regression tests for the operator sequence and injected post-Inventory/CustomerCare failure rollback, then make the smallest UX/API correction necessary: either make Apply persist and apply the submitted draft atomically, or make the two-step state unmistakable and reject Apply when unsaved changes are posted. It must preserve current accepted Sales delta semantics.
