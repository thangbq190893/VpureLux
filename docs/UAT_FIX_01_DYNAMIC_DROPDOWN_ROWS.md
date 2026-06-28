# UAT Fix 01 — Dynamic Dropdown Rows

## 1. Root cause

Create/edit forms with add/remove line rows cloned the first row with `cloneNode(true)` after ABP/LeptonX had already enhanced `.form-select` elements with Select2.

Cloned rows copied Select2 wrapper DOM (`.select2-container` spans, hidden-accessible state, duplicate `aria-*` attributes) without re-initializing the underlying `<select>`. The first row kept working; later rows appeared focusable but the dropdown would not open.

Re-indexing `name`/`id` attributes was already implemented, but Select2 cleanup and re-init were missing.

## 2. Pages affected

| Page | Route pattern | Dynamic rows |
|------|---------------|--------------|
| BOM create | `/Bom/Create/{productId}` | Component + quantity lines |
| BOM edit | `/Bom/Edit/{id}` | Component + quantity lines |
| Inventory receipt | `/Inventory/Receipt` | Stock item lines |
| Inventory issue | `/Inventory/Issue` | Stock item lines |
| Inventory adjustment | `/Inventory/Adjustment` | Increase/decrease lines |

**Not changed:** Sales create/edit — single product selector per form (no dynamic line cloning). Customer and inquiry filter pages — static selectors only.

## 3. Files changed

| File | Change |
|------|--------|
| `src/VPureLux.Web/Pages/Shared/DynamicRowSelects.js` | **New** shared helper: strip Select2 artifacts, hidden row template, re-init |
| `src/VPureLux.Web/Pages/Bom/BomItems.js` | Clone from hidden template; init Select2 on new rows; skip template when re-indexing |
| `src/VPureLux.Web/Pages/Inventory/Posting.js` | Same pattern for inventory line containers |
| `src/VPureLux.Web/Pages/Bom/Create.cshtml` | Load shared script before `BomItems.js` |
| `src/VPureLux.Web/Pages/Bom/Edit.cshtml` | Load shared script before `BomItems.js` |
| `src/VPureLux.Web/Pages/Inventory/Receipt.cshtml` | Load shared script before `Posting.js` |
| `src/VPureLux.Web/Pages/Inventory/Issue.cshtml` | Load shared script before `Posting.js` |
| `src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml` | Load shared script before `Posting.js` |
| `test/VPureLux.Web.Tests/Pages/DynamicRowDropdownRowsTests.cs` | **New** source-shape regression tests |
| `test/VPureLux.Web.Tests/Pages/BomPagesTests.cs` | Assert new init hooks |
| `test/VPureLux.Web.Tests/Pages/InventoryPagesTests.cs` | Assert new init hooks |

## 4. Fix design

1. **Hidden row template** — On first load, clone the first live row into a `d-none` template marked `data-dynamic-row-template`. The template is never Select2-enhanced in the DOM.
2. **Clean clone on add** — New rows clone the template, not an initialized live row. `createCleanClone()` removes any `.select2-container` nodes and resets select attributes.
3. **Re-index live rows only** — Re-indexing skips `[data-dynamic-row-template]` so model binder names stay sequential (`Items[0]…`, `Input.Lines[0]…`, etc.).
4. **Class-based selectors** — Row logic uses `.bom-item`, `.component-id`, `[data-inventory-line-row]`; no first-row ID selectors.
5. **Select2 re-init** — After append + re-index, `initializeSelects(row)` runs Select2 on raw `<select class="form-select">` in the new row only (Bootstrap 5 theme, modal-aware `dropdownParent` when applicable).

## 5. Manual validation result

Not run in this session (IIS Express held `VPureLux.Web` output DLLs, blocking a full solution rebuild). Re-test in browser after stopping IIS Express / Visual Studio debug:

1. BOM create/version: add 3+ component rows, open each "Linh kiện" dropdown, select values, remove row 2, add another row, open/select again.
2. Inventory receipt/issue/adjustment: same add/remove/add pattern on stock-item line dropdowns.

## 6. Tests run

```text
dotnet build test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj -o .build-out -m:1
  → Build succeeded

dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -o .build-out \
  --filter "FullyQualifiedName~Bom|FullyQualifiedName~Sales|FullyQualifiedName~Inventory|FullyQualifiedName~DynamicRow" -m:1
  → Passed: 61, Failed: 0

dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -o .build-out -m:1
  → Passed: 129, Failed: 0

dotnet build VPureLux.slnx --no-restore -m:2
  → Blocked: IIS Express file lock on VPureLux.Web bin output (stop IIS Express and retry)
```

## 7. Deferred items

- Sales multi-line create UI (if added later) should reuse `DynamicRowSelects.js`.
- Optional: mark dynamic-row selects with `data-no-autoinit` if ABP adds global auto-init that touches hidden templates before our script runs (not observed in current LeptonX setup).
- End-to-end browser automation for Select2 open/close (no Playwright suite in repo today).
