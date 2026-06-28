# UAT Fix 02A — Sales Multi-Product Audit

**Date:** 2026-06-27  
**Scope:** Read-only audit of Sales order line support (Domain → Web). No implementation in this batch.

---

## Executive conclusion

| Question | Answer |
|----------|--------|
| Backend multi-line capable | **yes** |
| DTO/API contract change needed | **no** |
| DB schema change needed | **no** |
| UI is main blocker | **partial** |
| Inventory/FIFO impact | **none** (already per-line) |
| Pricing/profit impact | **none** (already per-line) |
| Recommended implementation batch | **UI-only** |

The stack already models, persists, confirms, and prices **multiple order lines**. The primary gap is the **Create** Razor page, which renders and posts only `Input.Lines[0]`. **Edit** already lists all lines and exposes `AddLine` / per-line update / remove via existing app-service methods.

---

## Question-by-question findings

### 1. Does SalesOrder domain have multiple Lines?

**Yes.**

`SalesOrder` owns a private `List<SalesOrderLine> _lines` exposed as `IReadOnlyCollection<SalesOrderLine> Lines`. Methods:

- `AddLine(...)` — appends with sequential `LineNo`
- `UpdateLine(lineId, ...)`
- `RemoveLine(lineId)` — renumbers remaining lines
- `Confirm(...)` — sums `_lines` for totals; requires all lines confirmed

Evidence: `src/VPureLux.Domain/Sales/SalesOrder.cs` lines 12–32, 46–78, 125–131.

Domain unit test adds two lines and renumbers after remove: `test/VPureLux.Domain.Tests/Sales/SalesDomainTests.cs` — `Should_Create_Draft_Edit_And_Renumber_Lines`.

---

### 2. Do Create/Update input DTOs have a Lines collection?

**Yes (create); update is per-line.**

| DTO | Lines support |
|-----|----------------|
| `CreateSalesOrderDto` | `List<CreateSalesOrderLineDto> Lines` with `[Required, MinLength(1)]` |
| `CreateSalesOrderLineDto` | Single line fields (ProductId, Quantity, ActualSellingPrice, OverrideReason) |
| `UpdateSalesOrderLineDto` | Per-line update (no collection — by design) |
| `AddLine` | Uses `CreateSalesOrderLineDto` on existing order |

Evidence: `src/VPureLux.Application.Contracts/Sales/SalesInputs.cs`.

Output DTOs `SalesOrderDto` / `SalesOrderLineDto` also expose `Lines` collection: `SalesDtos.cs`.

---

### 3. Does SalesOrderAppService create multiple lines?

**Yes.**

`CreateAsync` loops `input.Lines` and calls `AddInputLineAsync` for each before insert:

```csharp
foreach (var inputLine in input.Lines)
{
    await AddInputLineAsync(order, inputLine);
}
```

Evidence: `src/VPureLux.Application/Sales/SalesOrderAppService.cs` lines 91–102.

---

### 4. Does SalesOrderAppService update multiple lines?

**Yes (one line per call).**

- `UpdateLineAsync(id, lineId, input)` — updates a single line by ID
- `AddLineAsync(id, input)` — adds another line to draft order
- `RemoveLineAsync(id, lineId)` — removes a line

There is no bulk-update DTO; multi-line editing is **line-at-a-time**, which matches current Edit UI.

Evidence: `ISalesOrderAppService.cs`, `SalesOrderAppService.cs` lines 105–133.

HTTP API mirrors this: `POST /lines`, `PUT /lines/{lineId}`, `DELETE /lines/{lineId}` — `SalesOrderController.cs`.

---

### 5. Does inventory issue/FIFO posting process multiple order lines?

**Yes — one inventory transaction per order line.**

`ConfirmAsync` iterates `order.Lines.OrderBy(x => x.LineNo)` and calls `ConfirmLineAsync` for each. Each line:

1. Expands published BOM components × line quantity
2. Posts a separate `SalesIssue` transaction via `PostInventoryIssueAsync`
3. Runs FIFO allocation per component requirement
4. Stores `InventoryTransactionId` and cost snapshot on the line

Per-line idempotency key: `sales-confirm:{orderId}:line:{lineId}`.

Evidence: `SalesOrderAppService.cs` lines 155–158, 203–225, 227–281.

No cross-line consolidation — each product line gets its own issue transaction. Adding more lines increases issue count linearly (expected).

---

### 6. Does pricing/profit calculation process multiple order lines?

**Yes — per line, aggregated at confirm.**

- **Draft:** each line resolves suggested price at order date; override permission checked per line in `AddInputLineAsync` / `UpdateLineAsync`
- **Confirm:** `ApplyLineConfirmationSnapshot` computes revenue, cost, profit, margin per line
- **Order totals:** `TotalRevenueAmount`, `TotalCostAmount`, `TotalProfitAmount` = sum of line amounts at confirm

Evidence: `SalesOrderLine.cs` lines 86–111; `SalesOrder.cs` lines 129–131; `SalesOrderAppService.cs` `AddInputLineAsync`.

---

### 7. Does Details page display multiple lines?

**Yes.**

`Details.cshtml` iterates `@foreach (var line in Model.Order.Lines)` in a table with line number, product, quantity, prices, cost/profit (permission-gated), and BOM snapshot items.

Evidence: `src/VPureLux.Web/Pages/Sales/Details.cshtml` lines 116–147.

---

### 8. Is Create UI rendering only one line?

**Yes — this is the main Create blocker.**

`Create.cshtml` binds only `Input.Lines[0].ProductId`, `Quantity`, `ActualSellingPrice`, `OverrideReason`. No add/remove row UI. No loop over lines.

`CreateModel` initializes `Lines = [new CreateSalesOrderLineDto()]` and posts full `Input` to `CreateAsync` — **PageModel is ready for multiple lines; the view is not.**

`SalesProductContext.js` uses `querySelector` (single selector/context panel) — supports one product context only.

Evidence: `Create.cshtml` lines 22–35; `Create.cshtml.cs` line 28; `SalesProductContext.js` lines 7–9.

---

### 9. Is Edit UI rendering only one line?

**Partial — existing lines yes; add-one-at-a-time.**

- **Existing lines:** table with `@foreach (var line in Model.Order.Lines)` — update/remove per line ✓
- **New lines:** separate “Add line” card with single `NewLine` form → `OnPostAddAsync` → `AddLineAsync` ✓
- **Not supported:** adding multiple new lines in one submit on Edit (must add sequentially)

Evidence: `Edit.cshtml` lines 21–54 (list), 58–83 (add one line); `Edit.cshtml.cs` `OnPostAddAsync`.

---

### 10. Are there tests for multiple products in one order?

**Partial coverage — no end-to-end multi-product confirm test.**

| Test area | Multi-line coverage |
|-----------|---------------------|
| Domain | **Yes** — two lines, remove, renumber (`SalesDomainTests`) |
| EF workflow | **No** — E2E confirm uses `.Lines.Single()` throughout |
| API | **Partial** — create with 1 line, then `AddLineAsync` for 2nd; no create-with-2-lines-in-body |
| Web pages | **No** — all `CreateAsync` calls pass single-element `Lines` array |

Evidence:

- Multi-line domain: `SalesDomainTests.Should_Create_Draft_Edit_And_Renumber_Lines`
- API add second line: `SalesApiTests.Should_Expose_Order_Line_Confirmation...` lines 32–38
- Single-line create everywhere else: grep `Lines = [` in `test/`

**Gap:** No integration test confirming an order with **two different products** in one `CreateAsync` call or confirming FIFO/cost for both lines.

---

### 11. Is DB schema already capable of multiple lines?

**Yes.**

- `AppSalesOrderLines` table via EF `OwnsMany(x => x.Lines)`
- Unique index `UX_SalesOrderLines_OrderId_LineNo` on `(SalesOrderId, LineNo)`
- Child `AppSalesOrderBomSnapshotItems` per line
- FK to inventory transaction per line

Evidence: `SalesOrderConfiguration.cs` lines 39–75; repository tests confirm table/index in `SalesRepositoryAndPermissionTests`.

No migration required for multi-line support.

---

### 12. What is the smallest safe implementation batch?

**Batch: UI-only (Create page + JS)**

| Layer | Change needed? | Notes |
|-------|----------------|-------|
| Domain | No | Already multi-line |
| Application.Contracts | No | `CreateSalesOrderDto.Lines` exists |
| Application | No | `CreateAsync` already loops lines |
| EntityFrameworkCore | No | Schema ready |
| HttpApi | No | API ready |
| **Web Create** | **Yes** | Dynamic line rows (reuse `DynamicRowSelects.js` pattern from UAT Fix 01) |
| **Web JS** | **Yes** | Row-scoped product context in `SalesProductContext.js` |
| Web Edit | Optional polish | Already functional via sequential AddLine |
| Tests | Recommended follow-up | Add 1 integration test: `CreateAsync` with 2 products + confirm; 1 web source-shape test for Create multi-line markup |

**Suggested Create UX (smallest delta):**

1. Replace single `Lines[0]` block with repeatable rows (`for` loop + add/remove JS).
2. Re-index `Input.Lines[n].*` names on add/remove (same as BOM).
3. Extend `SalesProductContext.js` to bind per-row `[data-sales-product-selector]` (class-based, not single `querySelector`).
4. Keep `CreateModel.OnPostAsync` unchanged — it already passes `Input` to `_service.CreateAsync(Input)`.

**Edit page:** No change required for basic multi-product workflow (create draft with one product → Edit → Add line). Optional UX improvement: allow multiple lines on Create in one step to reduce round-trips.

**Out of scope for UI-only batch:**

- Bulk line update API
- Same-product duplicate line validation (if business wants to forbid/restrict — not found in domain today)
- Customer history aggregation changes (already groups by product across orders)

---

## UI vs backend summary

```
┌─────────────────────────────────────────────────────────────┐
│  CreateSalesOrderDto.Lines[]  ──►  CreateAsync (loops)     │
│         ▲                              │                    │
│         │                              ▼                    │
│  Create.cshtml (Lines[0] only) ✗   SalesOrder + Lines      │
│                                         │                    │
│  Edit.cshtml (list + AddLine) ✓        ▼                    │
│                                    ConfirmAsync (per line)  │
│                                         │                    │
│  Details.cshtml (foreach lines) ✓      ▼                    │
│                                    FIFO + profit per line   │
└─────────────────────────────────────────────────────────────┘
```

---

## Files inspected

**Domain:** `SalesOrder.cs`, `SalesOrderLine.cs`, `SalesManager.cs`  
**Contracts:** `SalesInputs.cs`, `SalesDtos.cs`, `ISalesOrderAppService.cs`  
**Application:** `SalesOrderAppService.cs`  
**EF:** `SalesOrderConfiguration.cs`, `EfCoreSalesOrderRepository.cs`  
**Web:** `Create.cshtml(.cs)`, `Edit.cshtml(.cs)`, `Details.cshtml(.cs)`, `SalesProductContext.js`  
**Tests:** `SalesDomainTests.cs`, `SalesWorkflowTests.cs`, `SalesApiTests.cs`, `SalesPagesTests.cs`, `SalesRepositoryAndPermissionTests.cs`

---

## Recommended follow-up batches (after UI-only Create)

1. **Tests batch** — `CreateAsync` with 2+ lines + confirm; web regression for Create line collection markup (no production code).
2. **Optional Edit UX** — inline add multiple lines before save (nice-to-have; Edit AddLine already works).
