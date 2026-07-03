# UAT Fix 03J.1 - Inventory Ledger and Adjustment Audit

## 1. Executive summary

This is a docs-only audit of the current Inventory Ledger (`Sổ kho`) and Inventory Adjustment (`Ghi nhận điều chỉnh kho`) behavior before any business logic changes. No production code, Domain/Application/DB/schema/migration files, posting logic, FIFO logic, costing logic, or UI were changed.

Current finding:

- Ledger is currently a posted inventory transaction list, not a full trace ledger.
- Ledger has useful backend data available through existing DTOs, but the page shows only a small transaction-level subset.
- Adjustment currently works as manual increase/decrease posting, reusing receipt-like and issue-like mechanics.
- Adjustment does require a reason and protects against negative stock through FIFO allocation and balance checks.
- Adjustment does not yet model a physical count workflow where the user enters counted quantity and the system calculates the difference.

Recommended direction:

- 03J.2 should make Ledger a trace screen using UI/read-model changes first.
- 03J.3 should redesign Adjustment around physical count to delta, with manual increase/decrease kept only if the business explicitly needs an advanced mode.
- 03J.4 should formalize reason/note policy and validation.
- 03J.5 should improve source document/reference display if the business wants stronger audit provenance.

## 2. Current Inventory Ledger behavior

Files inspected:

- `src/VPureLux.Web/Pages/Inventory/Ledger.cshtml`
- `src/VPureLux.Web/Pages/Inventory/Ledger.cshtml.cs`
- `src/VPureLux.Application/Inventory/InventoryQueryAppService.cs`
- `src/VPureLux.EntityFrameworkCore/Inventory/EfCoreInventoryTransactionRepository.cs`
- `src/VPureLux.Application.Contracts/Inventory/InventoryDtos.cs`
- `test/VPureLux.Web.Tests/Pages/InventoryPagesTests.cs`

Current page behavior:

- The page title is `Inventory:Ledger`.
- The page loads posted transactions through `IInventoryQueryAppService.GetLedgerAsync(WarehouseId, StockItemId)`.
- The query returns posted `InventoryTransactionDto` records ordered newest first.
- The table is transaction-level, not line-level.

Current filters:

- Warehouse.
- Stock item.
- Apply filter.
- Clear filter.

Current visible columns:

- Posted date/time, formatted through `InventoryPostingUi.FormatDate`.
- Warehouse label.
- Transaction type, localized from `Inventory:TransactionType:{x.Type}`.
- Reason.
- Total issue cost, shown as `Inventory:IssueCost`.

Ledger field coverage:

| Expected field | Current UI? | Backend data available? | Notes |
|---|---:|---:|---|
| Warehouse | Yes | Yes | UI resolves labels from warehouse list. |
| Stock item | Filter only | Yes | Transaction lines include `StockItemId`, but rows do not show item labels. |
| Lot | No | Partial | Increase lines have `LotNo`; decrease lines have FIFO allocations to lot ids, but DTO allocations do not include lot labels. |
| Transaction type | Yes | Yes | Localized enum display exists. |
| Source document | No | Partial | `ReferenceType`, `ReferenceId`, and `BomVersionId` exist, but UI does not render them or resolve friendly labels. |
| In quantity | No | Yes | Line direction and quantity exist. UI does not split in/out. |
| Out quantity | No | Yes | Line direction and quantity exist. UI does not split in/out. |
| Running balance | No | Not directly | Can be reconstructed for a filtered item from transaction lines, but no persisted balance-after snapshot exists. |
| Unit cost | No | Partial | Increase lines have `UnitCost`; decrease allocations have FIFO unit cost. |
| Amount | Issue cost only | Partial | Decrease amount is allocation total; increase amount can be quantity times unit cost. UI shows only transaction total issue cost. |
| User | No | Partial | `InventoryTransaction` is a full-audited aggregate, so audit fields should exist through ABP, but the current DTO/page do not expose creator/user labels. |
| Time | Date only | Yes | `PostedAt` exists; current formatter appears date-focused in tests and docs. |
| Reason | Yes | Yes | Required for adjustments at domain level. Optional/blank for ordinary receipts/issues. |
| Note | No | No | There is `Reason`, but no separate note/comment field. |

Ledger assessment:

- It is not yet a business trace/audit ledger.
- It is a useful transaction list for high-level review.
- It cannot quickly answer "why did this stock increase/decrease?" for ordinary receipts/issues because source document and line details are hidden.
- It can partly answer that question for adjustments because reason is visible.
- It cannot show the complete FIFO trail for decreases because the UI hides line allocations and allocation DTOs lack lot display data.

## 3. Current Inventory Adjustment behavior

Files inspected:

- `src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml`
- `src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml.cs`
- `src/VPureLux.Web/Pages/Inventory/Posting.js`
- `src/VPureLux.Application.Contracts/Inventory/InventoryInputs.cs`
- `src/VPureLux.Application/Inventory/InventoryTransactionAppService.cs`
- `src/VPureLux.Domain/Inventory/InventoryTransaction.cs`
- `src/VPureLux.Domain/Inventory/InventoryManager.cs`
- `src/VPureLux.Domain/Inventory/InventoryBalance.cs`
- `src/VPureLux.EntityFrameworkCore/Inventory/InventoryTransactionConfiguration.cs`
- `test/VPureLux.Domain.Tests/Inventory/InventoryDomainTests.cs`
- `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Inventory/InventoryWorkflowTests.cs`

Current page behavior:

- User selects warehouse.
- User selects adjustment type: `AdjustmentDecrease` or `AdjustmentIncrease`.
- User enters reason.
- For decrease, user enters stock item and quantity lines.
- For increase, user enters stock item, quantity, lot number, received date, and unit cost lines.
- `Posting.js` toggles the active increase/decrease section and disables inactive controls.
- Page posts `PostAdjustmentDto` to `IInventoryTransactionAppService.PostAdjustmentAsync`.

Current adjustment mechanics:

- Increase adjustment creates an `InventoryTransaction` of type `AdjustmentIncrease`.
- Increase lines are receipt-like: they create transaction receipt lines, create inventory lots, and increase inventory balance/value.
- Decrease adjustment creates an `InventoryTransaction` of type `AdjustmentDecrease`.
- Decrease lines are issue-like: duplicate stock item lines are consolidated, FIFO allocations are created, lots are reduced, and inventory balance/value is reduced.
- Reason is required by `InventoryTransaction.NormalizeReason` for both adjustment types.
- Negative stock is prevented by FIFO allocation failure and balance non-negative checks.

Adjustment field coverage:

| Expected behavior | Current support? | Notes |
|---|---:|---|
| Manual increase/decrease lines | Yes | This is the primary current design. |
| Physical count entry | No | No counted quantity field exists. |
| Current system balance display | No | Page does not query balances before posting. |
| Difference calculation | No | User must choose increase/decrease and enter the delta manually. |
| Positive difference posts increase | No | Could be implemented, but not current behavior. |
| Negative difference posts decrease | No | Could be implemented, but not current behavior. |
| Reason required | Yes | Contract and domain require reason for adjustments. |
| Separate note/comment | No | Only reason exists. |
| Negative stock prevention | Yes | FIFO and balance checks reject overdraw. |
| Lot/FIFO/cost interaction | Yes, but manual | Increase requires lot and unit cost; decrease consumes FIFO lots automatically. |

Adjustment assessment:

- It is logically safe for manual stock corrections because it uses the same posting/FIFO/cost rules as receipts and issues.
- It is not optimized for physical inventory counting.
- It asks the user to decide the transaction direction and delta, which creates operational risk during count adjustments.
- It can be used as a generic in/out replacement unless business policy or UI wording constrains it.

## 4. Gaps against expected business flow

Ledger gaps:

- Ledger does not show line-level item movement.
- Ledger does not show lot movement or FIFO allocation details.
- Ledger does not show in/out quantities.
- Ledger does not show balance after each movement.
- Ledger does not show source document in a user-friendly way.
- Ledger does not show actor/user.
- Ledger does not show separate note.
- Ledger only shows total issue cost, so receipt/increase values are not obvious.
- Ledger filters are too narrow for audit use: no lot, transaction type, date range, or source document filter.

Adjustment gaps:

- Adjustment is direction-first and delta-first, not count-first.
- User does not see current system quantity on the page.
- User cannot enter physical counted quantity and let the system calculate the adjustment.
- Reason exists, but there is no clear reason category versus free-form note policy.
- Increase adjustment requires lot and unit cost, which is correct for valuation but may be too much friction for count users unless the UX explains/defaults it.
- Decrease adjustment consumes FIFO automatically, which is correct for cost consistency but not visible to the user before posting.
- Current UI can feel like another receipt/issue screen rather than a controlled stock count correction process.

## 5. Data already available

Available transaction data:

- Transaction id.
- Warehouse id.
- Transaction type.
- Transaction status.
- Idempotency key and request hash.
- Reference type.
- Reference id.
- BOM version id.
- Reason.
- Posted at.
- Total issue cost.
- Transaction lines.

Available line data:

- Stock item id.
- Direction: increase or decrease.
- Quantity.
- Lot number for receipt-like/increase lines.
- Received date for receipt-like/increase lines.
- Unit cost for receipt-like/increase lines.
- FIFO allocations for issue-like/decrease lines.

Available allocation data:

- Inventory lot id.
- Quantity.
- Unit cost.
- Total cost.

Available lot/balance data:

- Lot number.
- Warehouse id.
- Stock item id.
- Received quantity.
- Available quantity.
- Received date.
- Unit cost.
- Currency.
- Lot status.
- Balance quantity on hand.
- Inventory value.
- Last movement time.

Existing tests already guard:

- Inventory pages render.
- Ledger filters pass selected warehouse and stock item to the query service.
- Ledger transaction type labels are localized.
- Inquiry pages preserve selected filters and clear links.
- Inquiry pages format dates, quantities, and money for Vietnamese display.
- Receipt, Issue, and Adjustment compact multi-line markup and dynamic row hooks.
- Reason is required for adjustment domain transactions.
- FIFO allocation order and insufficient inventory behavior.
- Idempotency conflict behavior.

## 6. Data missing or unclear

Missing or unclear for a true trace ledger:

- Persisted balance after each transaction line.
- Persisted inventory value after each transaction line.
- User-friendly source document number/code.
- Source document display resolver across Sales, BOM, receipt, issue, and adjustment origins.
- Creator/user display in ledger DTOs.
- Separate reason code/category.
- Separate free-form note/comment field.
- Physical count document/session model.
- Counted quantity field.
- Count snapshot time.
- Approval/review state for adjustments.
- Optional attachment/evidence support for count adjustments.
- Whether adjustment increases should reuse an existing lot or always create a new lot.
- Whether count decreases should preview FIFO lots and cost before posting.

## 7. Recommended UX for Ledger

Ledger should become a trace screen rather than a compact transaction list.

Recommended filters:

- Warehouse.
- Stock item.
- Lot.
- Transaction type.
- Date range.
- Source document/reference.

Recommended columns:

- Date/time.
- Warehouse.
- Stock item.
- Lot or allocated lot.
- Transaction type.
- Source.
- In quantity.
- Out quantity.
- Balance after.
- Unit cost.
- Amount.
- Reason/note.
- User.

Recommended row model:

- Prefer line-level rows for audit clarity.
- For increase lines, one ledger row can show item, lot, quantity in, unit cost, and amount.
- For decrease lines with multiple FIFO allocations, either:
  - show one line row with expandable allocation details, or
  - show allocation-level rows so lot/cost trace is explicit.
- Keep transaction-level grouping available visually or through a detail link.

Classification:

- UI/read-model only: add filters/columns based on existing transaction line fields, reason, reference ids, and computed in/out/amount.
- Application behavior change: add a dedicated ledger read DTO/query that returns line-level or allocation-level rows instead of transaction aggregate DTOs.
- DB/schema change: only needed if business requires persisted balance-after/value-after snapshots, document numbers, note fields, or count documents.
- Deferred decision: exact balance-after semantics when multiple same-time transactions or backdated received dates exist.

## 8. Recommended UX for Adjustment

Adjustment should preferably become count-based.

Recommended primary flow:

1. User selects warehouse.
2. User selects stock item or scans/chooses Vật tư.
3. System shows current system quantity and current value context.
4. User enters physical counted quantity.
5. System calculates difference: counted quantity minus system quantity.
6. Positive difference becomes adjustment increase.
7. Negative difference becomes adjustment decrease.
8. Zero difference is either skipped or shown as no posting needed.
9. User enters required reason and optional note, once policy is approved.
10. System prevents posting if the resulting stock would be negative.

Recommended increase behavior:

- Positive delta should collect or derive lot number, received date, and unit cost because the current costing model requires valued lots.
- If a default unit cost is proposed, it must be explicitly designed. Possible choices include last cost, weighted average, manual required cost, or zero-cost disallowed.

Recommended decrease behavior:

- Negative delta should preview that FIFO will consume existing lots.
- User should not manually select FIFO lots unless business explicitly requires lot-directed adjustment.
- Posting should continue to use FIFO and existing negative-stock protections.

Manual mode:

- Keep manual increase/decrease only if the business confirms it is needed for non-count corrections.
- If kept, move it to an advanced mode with clear labeling so ordinary stock counts use the safer count-first flow.

Classification:

- UI/read-model only: show current balance and computed delta preview if no posting contract changes are made yet.
- Web/PageModel validation only: require reason before submit, block zero-delta submissions, and improve field-level errors.
- Application behavior change: add count-based posting input that converts counted quantity to increase/decrease delta.
- Domain/business rule change: formalize count adjustment invariants, cost policy, and optional approval state.
- DB/schema change: needed only if physical count documents, counted quantity snapshots, reason categories, notes, approvals, or balance-after snapshots must be persisted.
- Deferred decision: lot/cost policy for positive count deltas.

## 9. Proposed implementation batches

### 03J.2 Ledger UI/read-model polish

Classification: UI/read-model first, possible Application query contract change.

Scope:

- Add date range, transaction type, source document, and optional lot filters.
- Replace transaction-level table with line-level trace rows.
- Show in/out quantities, item labels, source reference, unit cost, amount, and reason.
- Use existing transaction line/allocation data where possible.
- Defer persisted balance-after unless approved.

### 03J.3 Adjustment UX redesign: physical count to delta

Classification: UI/PageModel plus Application behavior if posting input changes.

Scope:

- Add count-first UI.
- Load current balance for selected warehouse/item.
- Compute delta before posting.
- Route positive delta to increase and negative delta to decrease.
- Keep current manual mode only if approved.
- Preserve existing FIFO/cost behavior.

### 03J.4 Adjustment reason/note policy and validation

Classification: Web/PageModel validation first; Domain/schema only if note/category becomes persisted separately.

Scope:

- Confirm whether one free-form reason is enough.
- Decide if reason categories are required.
- Decide if optional note is required.
- Add clearer validation and display rules.
- Keep `Reason` as the existing field unless business approves new fields.

### 03J.5 Optional source document/reference improvements

Classification: Application/read-model first; DB/schema only if document numbers or new references are missing.

Scope:

- Resolve `ReferenceType`, `ReferenceId`, and `BomVersionId` into user-facing source labels.
- Link Ledger rows to source documents when routes exist.
- Add source reference filters.
- Decide whether manual receipt/issue/adjustment needs a document number/reference field.

## 10. Risk/impact

Low risk:

- Adding Ledger filters and columns based on existing DTO data.
- Improving display labels and formatting.
- Adding Web tests for Ledger table shape and filters.

Medium risk:

- Introducing a dedicated line-level ledger read model.
- Computing running balance from historical transaction lines at query time.
- Adding count-based UI that maps to existing adjustment posting inputs.

High risk:

- Changing FIFO allocation behavior.
- Changing costing policy for adjustment increases.
- Persisting balance-after snapshots.
- Adding count documents, approval states, or new source document identifiers.
- Changing adjustment semantics from manual delta to count-based without preserving compatibility for existing operational needs.

Guardrails:

- Do not change posting/FIFO/cost behavior in 03J.2.
- Do not change Domain/Application/DB in a Ledger UI-only batch.
- Do not add schema until the business confirms count document, note, source reference, and balance-after requirements.
- Keep backend identifiers unchanged.
- Keep Vietnamese display terminology aligned with `Vật tư`.

## 11. Deferred business decisions

- Should Ledger show transaction-level rows, line-level rows, allocation-level rows, or expandable grouped rows?
- Should running balance be reconstructed at query time or persisted at posting time?
- Should balance-after be per item, per item/warehouse, or per item/warehouse/lot?
- Should manual receipts/issues have a user-facing document number?
- Should adjustment have reason categories, note, or both?
- Should adjustment require approval before posting?
- For positive count deltas, what unit cost should be used?
- For positive count deltas, should users create a new lot, select an existing lot, or use a system count lot?
- For negative count deltas, should FIFO be automatic or should lot-directed decrease be allowed?
- Should zero-difference count rows be stored for count audit or skipped?

## 12. Suggested tests for later batches

Ledger tests:

- Web test for new filters: warehouse, stock item, lot, transaction type, date range, and source document.
- Web test that Ledger renders item labels, in/out quantities, amount, reason, and source labels.
- App/EF test for line-level ledger read model across receipt, issue, adjustment increase, and adjustment decrease.
- App/EF test for allocation-level decrease rows showing FIFO lot/cost detail.
- Test for source reference rendering for Sales/BOM-origin transactions.
- Test for stable ordering when multiple transactions are posted close together.

Adjustment tests:

- Web test for count-based row shape: current quantity, counted quantity, delta, reason.
- PageModel test that positive delta maps to adjustment increase input.
- PageModel test that negative delta maps to adjustment decrease input.
- PageModel or App test that zero-delta rows do not post unless explicitly approved.
- App/EF test that count decrease still consumes FIFO and rejects insufficient inventory.
- App/EF test that count increase creates a valued lot using the approved cost policy.
- Web test that reason/note policy displays friendly validation messages.
- Regression test that current manual increase/decrease mode remains available only if approved.

Validation for this docs-only audit:

- Do not run build or tests.
- Run `git diff --check`.
- Run repository grep for the legacy Vietnamese component wording.
