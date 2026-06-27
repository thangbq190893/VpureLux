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
