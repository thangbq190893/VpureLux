# UAT Fix 02B — Sales Create Multi-Product UI

**Date:** 2026-06-27  
**Scope:** Web UI only — Sales Create page dynamic product lines.

---

## 1. Root cause

The Sales Create Razor page hard-coded a single line block (`Input.Lines[0]`). Although `CreateModel` binds `CreateSalesOrderDto` with a `Lines` collection and `CreateAsync` already loops all lines, operators could only submit one product per create form.

`SalesProductContext.js` used page-level `querySelector`, so even if multiple selectors existed, only the first row would load product context.

---

## 2. Why backend was not changed

UAT Fix 02A confirmed:

- `SalesOrder` aggregate already owns multiple `SalesOrderLine` entities
- `CreateSalesOrderDto.Lines` is already `List<CreateSalesOrderLineDto>` with `[MinLength(1)]`
- `SalesOrderAppService.CreateAsync` iterates `input.Lines`
- Confirm/FIFO/pricing already run per line
- DB schema (`AppSalesOrderLines`) already supports multiple rows

No Domain, Application, Contracts, HttpApi, or migration work was required.

---

## 3. Files changed

| File | Change |
|------|--------|
| `src/VPureLux.Web/Pages/Sales/Create.cshtml` | Repeatable `data-sales-line-row` blocks; add/remove controls; load shared scripts |
| `src/VPureLux.Web/Pages/Sales/SalesCreateLines.js` | **New** — template clone, re-index, Select2 init, product context bind |
| `src/VPureLux.Web/Pages/Sales/SalesProductContext.js` | Row-scoped context loading; `vplSalesProductContext` API |
| `test/VPureLux.Web.Tests/Pages/SalesPagesTests.cs` | Source-shape assertions for multi-line Create UI |
| `docs/UAT_FIX_02B_SALES_CREATE_MULTI_PRODUCT_UI.md` | This document |

**Unchanged:** `Create.cshtml.cs` (`OnPostAsync` still calls `_service.CreateAsync(Input)`), Edit, Details, backend layers.

---

## 4. Create page dynamic line design

1. `#sales-create-lines` container holds one or more `.sales-line-row` elements (`data-sales-line-row`).
2. Initial render uses `@for` over `Model.Input.Lines` (default: one line).
3. **Add line** clones a hidden `data-dynamic-row-template` row (UAT Fix 01 `DynamicRowSelects.js` pattern).
4. **Remove line** deletes a row when more than one live row remains.
5. Product `<select>` options copied from first live row; values cleared on new row.
6. Select2 initialized on the new row only (`initializeSelects(row)`).

---

## 5. Row-scoped product context design

- Each row contains `[data-sales-product-selector]` and `[data-sales-product-context]`.
- `bindRow(scope)` attaches `change` on the selector within that scope only.
- AJAX loads context into the same row's panel; suggested price fills `[data-sales-actual-price]` in that row only.
- **Edit page compatibility:** selectors outside `data-sales-line-row` (Add Line form) bind via `closest('form')` — no Edit markup changes.

Public API: `window.vplSalesProductContext.initializeRow(row)` called after dynamic row insert.

---

## 6. Model binding / re-indexing behavior

After add/remove, live rows (excluding hidden template) are re-indexed sequentially:

| Field | Posted name |
|-------|-------------|
| Product | `Input.Lines[n].ProductId` |
| Quantity | `Input.Lines[n].Quantity` |
| Actual price | `Input.Lines[n].ActualSellingPrice` |
| Override reason | `Input.Lines[n].OverrideReason` |

Matching `id` and label `for` attributes updated (`Input_Lines_n__Field`). Hidden template excluded from indexing.

---

## 7. Tests run

```text
dotnet build test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj -o .build-out-02b -m:1
  → Build succeeded

dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -o .build-out-02b --filter "FullyQualifiedName~Sales" -m:1
  → Passed: 16, Failed: 0

dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -o .build-out-02b -m:1
  → Passed: 129, Failed: 0
```

---

## 8. Manual smoke result

Not run in this session (requires running app with test catalog data). Recommended checklist:

1. `/Sales/Create` — one line on load
2. Add 3 rows — each product dropdown opens (Select2)
3. Select different products — each row shows its own context panel
4. Remove middle row — remaining rows re-index; dropdowns still work
5. Add row again — dropdown opens
6. Submit with 2+ valid lines — Details shows multiple lines

---

## 9. Remaining deferred items

- Browser/E2E automation for multi-line create submit
- Backend integration test: `CreateAsync` with 2+ products in one request (separate batch)
- Client-side validation message re-index after row remove (server validation unaffected)
- Optional Edit UX: inline multi-line add (Edit already supports sequential `AddLine`)

---

## 10. UAT Fix 02B.1 — Rendering bug fix (2026-06-27)

### Rendering bug root cause

Dynamic rows cloned the **first live row** after global Select2 initialization. That copied `.select2-container` DOM into the hidden template and new rows, producing stacked inline product controls/context blocks instead of one dropdown overlay per row.

Copying `innerHTML` from an initialized live product select also risked carrying stale option/state markup into new rows.

### DOM/template fix

- Added `<template id="sales-line-row-template">` **outside** `#sales-create-lines` — inert markup never enhanced by Select2.
- `SalesCreateLines.js` clones from the template only (`cloneTemplateRow()`); removed `ensureTemplate()` live-row cloning.
- New rows initialize Select2 on the raw `[data-sales-product-select]` element only.
- Remove-row strips Select2 from the deleted row before DOM removal.

### Row-scoped product context fix

- Default context HTML captured from the `<template>` panel (placeholder text), not from a live row after product selection.
- `getProductSelector(scope)` resolves selector within row scope only.
- Select2 `select2:select` / `select2:clear` events wired per row after init.

### Manual smoke result (02B.1)

Not run in this session — verify `/Sales/Create` add/remove/add flow in browser after deploy.

### Tests run (02B.1)

```text
dotnet build test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj -o .build-out-02b1-fix -m:1
  → Build succeeded

dotnet test ... --filter "FullyQualifiedName~Sales" -m:1
  → Passed: 16, Failed: 0

dotnet test ... -m:1
  → Passed: 129, Failed: 0
```

---

## 11. UAT Fix 02B.2 — Clean row UI (2026-06-27)

### Duplicate rendering root cause

Three issues combined to produce messy rows in the browser:

1. **Select2 + ABP auto-init** — Line product selects used `form-select`, so ABP/LeptonX enhanced them with Select2 on load; dynamic rows then received a second manual Select2 pass or orphaned `.select2-container` DOM, producing stacked/duplicate dropdown UI on row 2+.
2. **Product name in context panel** — `renderContext` wrote `ProductLabel` as a bold heading below the dropdown, visually duplicating the selected product name.
3. **Quantity culture formatting** — `asp-for` on `decimal Quantity` (default `0`) rendered as `0,00` under vi-VN culture, looking like money formatting.

### Final DOM structure (each live row)

```text
[data-sales-line-row]
  label → Product SKU
  select.form-select.w-100[data-sales-product-select]   ← single native full-width dropdown
  div.alert[data-sales-product-context]                 ← BOM badge, image status, suggested price only
  input[type=number].sales-line-quantity                ← default 1, invariant numeric
  input[type=number].sales-line-actual-price
  input.sales-line-override
  button.remove-sales-line
```

Hidden `<template id="sales-line-row-template">` sits **outside** `#sales-create-lines` — never rendered, never Select2-enhanced.

### Product dropdown rendering

- Native Bootstrap `form-select w-100` only — **no Select2** on Sales Create line products.
- `ensureNativeProductSelect()` strips any accidental Select2 artifacts after `abp.dom.ready`.
- Add-row clones raw template markup; never clones a live row.

### Product context (row-scoped)

- `bindRow(scope)` resolves `[data-sales-product-select]` and `[data-sales-product-context]` within the row only.
- Context shows **supporting info only**: BOM badge, image status, suggested price.
- Product name/code appears **only** in the dropdown selection.
- Placeholder when no product selected: localized `Sales:SelectProductForContext`.

### Quantity formatting

- `type="number"` with `step="any"` and `CultureInfo.InvariantCulture` value formatting.
- Initial display: `1` when model quantity is `0`; template default `value="1"`.
- New rows reset quantity to `1` in JS — not `0,00`.

### Price formatting

- Unchanged: raw numeric `type="number"` input; suggested price still pre-fills from context AJAX when empty.

### Manual smoke result (02B.2)

Not run in this session — verify `/Sales/Create` in browser after deploy using checklist in section 8.

### Tests run (02B.2)

```text
dotnet build test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj -o .build-out-02b2 -m:1
  → Build succeeded

dotnet test ... --filter "FullyQualifiedName~Sales" -m:1
  → Passed: 16, Failed: 0

dotnet test ... -m:1
  → Passed: 129, Failed: 0
```

### Remaining known issues

- Browser/E2E automation for multi-line create submit
- Optional Select2/searchable product picker if catalog grows (native select is intentional for stability)
- Client-side validation message re-index after row remove (server validation unaffected)

---

## 12. UAT Fix 02B.3 — Eligibility validation and context init (2026-06-27)

### Why backend exception happened

`SalesOrderAppService.CreateAsync` calls `EnsurePublishedBomAsync` for each line. Products without a published BOM throw `BusinessException` (`SALES_010`). The Create page previously relied on AJAX per-row context (inconsistent on row 1) and only a summary-level catch, so operators could submit ineligible products and sometimes see raw error UI.

### Create page handling (no Application change)

- **Preloaded map:** `GetProductContextsJson()` embeds all product BOM/image/suggested-price flags in `#sales-product-context-data` — no per-row AJAX on Create.
- **Row context init:** `loadProductContextFromMap()` runs synchronously for every row on `abp.dom.ready`, including postback lines with selected products.
- **Row eligibility:** Products with `hasPublishedBom: false` show localized warning (`Sales:ProductNotSaleEligible`), invalid select styling, and client submit is blocked.
- **Server validation:** `ValidateLineEligibility()` adds `ModelState` errors per line before `CreateAsync`; `SALES_010` catch adds row errors as safety net. Page re-renders with preserved input.

### Manual smoke result (02B.3)

Not run in this session — verify `/Sales/Create` scenarios in task checklist before UAT sign-off.

### Tests run (02B.3)

```text
dotnet build test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj -o .build-out-02b3 -m:1
  → Build succeeded

dotnet test ... --filter "FullyQualifiedName~Sales" -m:1
  → Passed: 19, Failed: 0

dotnet test ... -m:1
  → Passed: 132, Failed: 0
```

### Remaining known issues (02B.3)

- Browser/E2E automation for multi-line create submit
- Client-side validation message re-index after row remove (server validation unaffected)

---

## 13. UAT Fix 02B.4 — Initial row parity and final multi-product UI (2026-06-28)

### Root cause of initial-row vs dynamic-row mismatch

Existing server-rendered rows and dynamically added rows already had similar row-scoped markup, but they did not reliably share the same initialization timing. Existing rows were initialized only through an `abp.dom.ready` subscription, which could be missed if ABP had already fired by the time the Sales Create script subscribed. Dynamically added rows called `prepareLineRow(row)` directly, so row 2 could render product context while row 1 stayed on the placeholder.

There was also a smaller dynamic-row price issue: the product select had no blank option, so a newly added row could default to the first real product and auto-fill that product's suggested price before the operator made a selection.

### Shared init path

- `SalesCreateLines.js` now calls `bootExistingRows(container)` immediately on `DOMContentLoaded`.
- It still calls `bootExistingRows(container)` again after `abp.dom.ready`, so accidental Select2 markup can still be stripped if ABP enhances the row later.
- Both existing rows and added rows flow through `prepareLineRow(row)`.
- `prepareLineRow(row)` calls `vplSalesProductContext.initializeRow(row)`.
- `initializeRow(row)` remains idempotent: rebinding removes the previous row change handler before adding a new one, and rendering replaces the existing context panel content instead of appending duplicate markup.
- `CreateModel` now prepends an empty `Select` product option, so newly added rows start with no ProductId, placeholder context, blank actual price, and quantity `1`.

### Actual selling price auto-fill behavior

- When a selected product has a suggested price and the row actual price is blank, the row actual price is filled from the preloaded product context map.
- Existing postback/user-entered actual prices are not overwritten because the script fills only when `[data-sales-actual-price]` has no value.
- New rows now remain blank until a ProductId is selected; selecting different products fills the correct row-scoped suggested price.

### No-BOM validation behavior

- Products without a published BOM render the localized row warning: `Sales:ProductNotSaleEligible`.
- The row select receives invalid styling.
- Client submit is blocked by `validateAllRows`.
- `CreateModel.ValidateLineEligibility()` and the `SALES_010` catch remain the server-side safety net if an invalid product is submitted anyway.

### Manual smoke result (02B.4)

Passed against the local app at `https://localhost:44325` using the seeded admin login.

- `/Sales/Create` loaded and authenticated successfully.
- Initial row starts blank with placeholder context, blank actual price, and quantity `1`.
- Selecting a valid product in row 1 rendered published BOM status, image status, suggested price, eligibility state, and default actual selling price immediately.
- Adding row 2 and selecting the same ProductId as row 1 rendered identical BOM status, image status, suggested price, eligibility state, and default actual price.
- Adding row 3 with a different valid product rendered that product's own suggested price and context; no stale previous-row price remained.
- Removing row 2 and adding another row left the new row blank with placeholder context and quantity `1`.
- No selected ProductId showed the stale placeholder.
- Selecting a product without a published BOM showed the row-level warning and blocked client submit.
- Submitting two valid product rows succeeded and redirected to Details.
- Details rendered two order lines for the created draft order.

### Tests run (02B.4)

```text
dotnet build VPureLux.slnx --no-restore -m:2
  -> Build succeeded, 0 warnings, 0 errors

dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1
  -> Passed: 20, Failed: 0

dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1
  -> Passed: 133, Failed: 0
```
