# Batch 16B — Sales/Pricing History & Detail Page Chrome — Audit & Implementation Plan

**Document type:** Audit / implementation plan (no code changes in 16B.1)  
**Branch:** `feature/ui-history-detail-chrome-batch-16`  
**Base branch:** `main` (@ `7810f53` — Merge PR #13 Batch 15 list chrome)  
**Audit date:** 2026-06-24  
**Owner:** Cursor Agent (Composer 2.5)

---

## 1. Executive summary

Batches 13–15 established VPURELUX branding, Batch 14 dense ERP tokens/helpers, Batch 14 inquiry/catalog polish, and Batch 15 remaining **list/index** chrome. Several **Sales history/detail** and **Pricing history/detail** pages still use legacy patterns: bare `<h2>`, default tables, monolithic rows, Bootstrap summary cards without `.vpl-page` wrappers, and mixed action button styles.

Batch 16B applies **existing Batch 14/15 opt-in helpers** (class-only Razor changes) to:

- **Sales:** `History`, `CustomerHistory`, `Details`
- **Pricing:** `Components/History`, `Products/History`

**Batch 16A** (Home launcher density, global empty-state wiring on non–Sales/Pricing pages) is **explicitly deferred** and not part of 16B.

No backend, permission, route, handler, or business-rule changes are in scope. **Formatting must not change** in Batch 16B unless separately approved (Sales Details already uses `₫` formatters; Pricing history uses `N2` + currency code — preserve both).

---

## 2. Exact branch name and base branch

| Item | Value |
|------|--------|
| **Working branch** | `feature/ui-history-detail-chrome-batch-16` |
| **Base branch** | `main` @ `7810f53` (Merge PR #13 — Batch 15 list chrome) |
| **Prerequisite** | Batch 14 helpers present in `global-styles.css` on `main` (confirmed) |

---

## 3. Pages inspected

All paths under `src/VPureLux.Web/Pages/Sales/`, `Pages/Pricing/`, plus `wwwroot/global-styles.css` and Batch 14/15 docs.

| Page | Route area | Inspected |
|------|------------|-----------|
| `Sales/Index.cshtml` | Sales inquiry list | Yes — **already Batch 14** (reference only) |
| `Sales/History.cshtml` | Sales order history list | Yes — **16B in scope** |
| `Sales/CustomerHistory.cshtml` | Customer purchase report | Yes — **16B in scope** |
| `Sales/Details.cshtml` | Order detail | Yes — **16B in scope** (partial 14.4 toolbar only) |
| `Sales/Create.cshtml` | Order create form | Yes — out of scope |
| `Sales/Edit.cshtml` | Order edit form | Yes — out of scope |
| `Pricing/Index.cshtml` | Pricing tabbed list | Yes — **already Batch 15** (reference only) |
| `Pricing/Components/History.cshtml` | Component price history | Yes — **16B in scope** |
| `Pricing/Products/History.cshtml` | Product price history | Yes — **16B in scope** |
| `Pricing/Components/Create.cshtml` | Component price create | Yes — out of scope |
| `Pricing/Products/Create.cshtml` | Product price create | Yes — out of scope |

---

## 4. Sales history/detail pages found

### In scope — Batch 16B.2

| # | File | Menu / entry | Legacy pattern |
|---|------|--------------|----------------|
| 1 | `Pages/Sales/History.cshtml` | Sales Index toolbar link | Bare `<h2>`; single-line markup; default `abp-table`; `btn-outline-primary` Details; profit column gated by `canViewProfit` |
| 2 | `Pages/Sales/CustomerHistory.cshtml` | Sales Index toolbar link (profit permission) | Bare `<h2>`; GET customer filter; 4× Bootstrap summary stat cards; conditional product table; `alert alert-light border` empty state |
| 3 | `Pages/Sales/Details.cshtml` | History list / Sales Index | Bare `<h2>` header row; `.vpl-btn-toolbar` on actions (**Batch 14.4 only**); `dl.row` summary; default line table; `btn-secondary` Back; cost/profit columns permission-gated |

### Inspected — out of scope (16B)

| File | Reason |
|------|--------|
| `Pages/Sales/Index.cshtml` | Already uses `.vpl-page`, `.vpl-page-header`, `.vpl-card-dense`, `.vpl-table-dense`, `.vpl-btn-toolbar` |
| `Pages/Sales/Create.cshtml` | Create form — not history/detail |
| `Pages/Sales/Edit.cshtml` | Edit form — not history/detail |

---

## 5. Pricing history/detail pages found

### In scope — Batch 16B.3

| # | File | Entry | Legacy pattern |
|---|------|-------|----------------|
| 1 | `Pages/Pricing/Components/History.cshtml` | Pricing Index → Open History | Header `abp-row` + `h2` + create btn; context `<p>`; current version + lookup forms in columns; vertical timeline (not table); `text-muted` empty; `btn-secondary` Back |
| 2 | `Pages/Pricing/Products/History.cshtml` | Pricing Index → Open History | Same layout as component history (product context label) |

### Inspected — out of scope (16B)

| File | Reason |
|------|--------|
| `Pages/Pricing/Index.cshtml` | Batch 15 list chrome complete |
| `Pages/Pricing/Components/Create.cshtml` | Create form — not history/detail |
| `Pages/Pricing/Products/Create.cshtml` | Create form — not history/detail |

---

## 6. Current visual pattern per page

| Page | Header | Container | Filter / context | Table / content | Toolbar / actions |
|------|--------|-----------|------------------|-----------------|-------------------|
| **Sales History** | Bare `h2` | None | N/A | Default striped `abp-table` | `btn-sm btn-outline-primary` Details per row |
| **Sales CustomerHistory** | Bare `h2` | None | GET `CustomerId` select + Search | 4 default Bootstrap stat cards; default `abp-table` when results | Search `btn-primary`; no row actions |
| **Sales Details** | `h2` order no. + `.vpl-btn-toolbar` | None | `dl.row` order summary | Default striped line table | Edit / Confirm / Cancel forms; Back link |
| **Pricing Component History** | `h2` in `abp-row` + create column | None | Context line; current version `dl`; GET lookup form | Vertical timeline (`border-start` blocks) | Create `btn-primary`; lookup Search `btn-outline-primary`; Back |
| **Pricing Product History** | Same as component | Same | Same | Same | Same |

**CSS on `main` today:** Batch 14 helpers in `global-styles.css` — `.vpl-page`, `.vpl-page-header`, `.vpl-page-title`, `.vpl-page-subtitle`, `.vpl-card-dense`, `.vpl-table-dense`, `.vpl-btn-toolbar`, `.vpl-empty-state`.

---

## 7. Proposed target pattern per page

Apply **class-only** changes using existing helpers. Do **not** change columns, handlers, permission blocks, or formatters.

| Page | `.vpl-page` | `.vpl-page-header` + `.vpl-page-title` | `.vpl-page-subtitle` | `.vpl-btn-toolbar` | `.vpl-card-dense` | `.vpl-table-dense` | `.vpl-empty-state` | Notes |
|------|-------------|----------------------------------------|----------------------|--------------------|-------------------|--------------------|--------------------|-------|
| **Sales History** | Yes | Yes — `@L["Sales:History"]` | No | No | Yes — wrap table card | Yes | No | Details btn → `btn-sm btn-outline-secondary`; preserve `canViewProfit` column |
| **Sales CustomerHistory** | Yes | Yes — `@L["Sales:CustomerHistory"]` | Optional — selected customer label as subtitle when present | No | Yes — filter card + summary row wrapper + results card | Yes — product table | Yes — replace `alert alert-light border` empty with `.vpl-empty-state` when no purchases | Keep 4 summary stat cards structurally; optional `vpl-card-dense` on each stat card or single wrapper — **no metric redesign** |
| **Sales Details** | Yes | Yes — order no. as title | Optional — status/customer as subtitle block below title | Yes — existing toolbar (already present) | Yes — summary `dl` in dense card; dense card around line table | Yes — line table | No | Preserve `#SalesDetailsPage`, `data-sales-*` hooks, Confirm/Cancel forms; Back stays secondary |
| **Pricing Component History** | Yes | Yes — history title | Yes — `@L["Pricing:ComponentContext", …]` as `.vpl-page-subtitle` | Yes — Create button | Yes — current version card; lookup card; optional timeline wrapper card | N/A (timeline, not table) | Yes — `text-muted` no-version → `.vpl-empty-state` where safe | Keep timeline markup; lookup GET form unchanged |
| **Pricing Product History** | Yes | Same as component | Yes — product context subtitle | Yes | Same | N/A | Same | Mirror component history pattern |

**No new CSS** expected unless an edge case cannot use existing helpers (unlikely). Do **not** convert Pricing timeline to a table.

**Explicitly excluded from Batch 16B formatting changes:** Pricing `N2` + currency display; Sales CustomerHistory raw `@Model.TotalRevenue` display strings (separate approval if alignment needed).

---

## 8. Exact files proposed for implementation

| Batch | Files |
|-------|-------|
| **16B.2 Sales** | `src/VPureLux.Web/Pages/Sales/History.cshtml`, `Pages/Sales/CustomerHistory.cshtml`, `Pages/Sales/Details.cshtml` |
| **16B.3 Pricing** | `src/VPureLux.Web/Pages/Pricing/Components/History.cshtml`, `Pages/Pricing/Products/History.cshtml` |
| **16B.4 Wrap-up** | `docs/UI_HISTORY_DETAIL_CHROME_BATCH_16_PR_SUMMARY.md`; optional status line in this plan |
| **Tests (only if assertions break)** | `test/VPureLux.Web.Tests/Pages/SalesPagesTests.cs`, `Pages/PricingPagesTests.cs` |

---

## 9. Explicit files that must not be touched

| Category | Paths / examples |
|----------|------------------|
| **Backend** | `src/VPureLux.Domain/**`, `Application/**`, `EntityFrameworkCore/**`, `HttpApi/**` |
| **PageModel logic** | All `*.cshtml.cs` under Sales and Pricing |
| **JavaScript** | `Sales/Details.js`, `Sales/SalesProductContext.js`, `SalesProductContext.js` |
| **Sales create/edit** | `Pages/Sales/Create.cshtml`, `Edit.cshtml` |
| **Pricing create** | `Pages/Pricing/Components/Create.cshtml`, `Products/Create.cshtml` |
| **Already polished** | `Sales/Index.cshtml`, `Pricing/Index.cshtml`, Catalog, Inventory inquiry trio, Audit, Batch 15 list pages |
| **Batch 16A deferrals** | `Pages/Index.cshtml` (Home), `Index.css`; Inventory inquiry empty alerts (Ledger/Balances/Lots) |
| **Themes / libs** | `Themes/**`, `wwwroot/libs/**` |
| **Global CSS** | `global-styles.css` — avoid unless helper gap proven |
| **Formatting helpers** | `SalesUiFormatter.cs`, `PricingDateUi.cs`, `@functions` blocks in Razor |

---

## 10. Permission / business-rule safety notes

| Concern | Batch 16B approach |
|---------|-------------------|
| Sales profit column on History | **No change** — `canViewProfit` / `VPureLuxPermissions.Sales.ViewProfit` preserved |
| Sales cost/profit on Details | **No change** — `canViewCost` / `canViewProfit` column gates preserved |
| Sales CustomerHistory profit summary/table | **No change** — report data and columns preserved (page is profit-gated at Index link) |
| Sales Confirm/Cancel/Edit actions | **No change** — handlers, idempotency key, `data-sales-action-form` preserved |
| Pricing Create permission | **No change** — `Model.CanCreate` on history pages preserved |
| Pricing lookup GET form | **No change** — `LookupDateText`, validation, historical lookup behavior preserved |
| Pricing timeline content | **No change** — version list order, badges, dates, reasons unchanged |
| Column visibility | **No** add/remove columns |
| Data calculations | **No change** — display-only class additions |

---

## 11. Proposed implementation batches

### Batch 16B.2 — Sales history/detail chrome

**Goal:** Align Sales History, CustomerHistory, and Details with shared page chrome and dense tables/cards.

**Touch:** `Pages/Sales/History.cshtml`, `CustomerHistory.cshtml`, `Details.cshtml`.

**Changes:** `.vpl-page` wrapper; `.vpl-page-header` + `.vpl-page-title`; `.vpl-card-dense` on list/summary containers; `.vpl-table-dense` on tables; `.vpl-btn-toolbar` on Details (extend existing); optional `.vpl-page-subtitle` / `.vpl-empty-state` on CustomerHistory; History Details button style aligned with Audit/Index (`outline-secondary`).

**Validation:** `dotnet test ... --filter FullyQualifiedName~Sales`

---

### Batch 16B.3 — Pricing history/detail chrome

**Goal:** Dense page chrome on component and product price history pages without altering timeline or lookup behavior.

**Touch:** `Pages/Pricing/Components/History.cshtml`, `Pages/Pricing/Products/History.cshtml`.

**Changes:** `.vpl-page`; external header + title; `.vpl-page-subtitle` for context; `.vpl-btn-toolbar` for Create; `.vpl-card-dense` on current-version and lookup sections; `.vpl-empty-state` for zero-version messaging; preserve timeline DOM and Back link.

**Validation:** `dotnet test ... --filter FullyQualifiedName~Pricing`

---

### Batch 16B.4 — Final validation and PR summary

**Goal:** Full Web.Tests, `docs/UI_HISTORY_DETAIL_CHROME_BATCH_16_PR_SUMMARY.md`.

**Checklist:** All five Razor pages; confirm Sales Index, Pricing Index, and other batches unchanged; document deferred 16A items.

---

## 12. Explicitly defer Batch 16A

Not in Batch 16B (separate future batch):

| Item | Rationale |
|------|-----------|
| **Home launcher density** | `Pages/Index.cshtml` / `Index.css` — module grid redesign |
| **Empty-state wiring outside Sales/Pricing history/detail** | Inventory inquiry `alert alert-light border` on Ledger/Balances/Lots; other modules — use `.vpl-empty-state` globally in 16A |

Batch 16B **may** apply `.vpl-empty-state` on Sales CustomerHistory and Pricing history pages only (in scope above).

---

## 13. Non-goals

- No backend, Domain, Application, EF Core, migration, or DB schema changes
- No API contract or PageModel query changes
- No business-rule changes (Sales orders, CustomerHistory aggregations, Pricing version lookup)
- No permission gate changes or data exposure changes
- No route, handler, or form field name / binding changes
- No create/edit/modal behavior or JS changes
- No hiding columns or removing actions
- No formatting changes unless separately approved
- No Home launcher changes in Batch 16B
- No Batch 16A scope in 16B
- No global CSS overrides of all `.card` / `.table` / `.btn`
- No LeptonX rewrite or new UI framework
- No Pricing timeline → table conversion

---

## 14. Recommended first implementation batch

**Start with Batch 16B.2 — Sales history/detail chrome, beginning with `Sales/History.cshtml`.**

**Why:**

1. **Lowest risk entry:** `History.cshtml` is a single compact list (similar to pre–Batch 15 Pricing Index) with no filter forms or summary cards.
2. **High visibility:** Linked from polished Sales Index toolbar — legacy History page contrast is obvious to users.
3. **Validates profit permission column** pattern before CustomerHistory report complexity.
4. **Details and CustomerHistory** follow the same helper set with incrementally more layout (summary cards, action toolbar, empty state).
5. Pricing history pages share header/toolbar pattern but use timeline layout — better after Sales table/card patterns are proven in 16B.2.

**Exit criteria for 16B.2:**

- Sales History, CustomerHistory, Details use `.vpl-page`, `.vpl-page-header`, `.vpl-page-title`, and dense cards/tables where applicable
- `dotnet build VPureLux.slnx` succeeds
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --filter FullyQualifiedName~Sales` — all pass

---

## References

- `docs/UI_VISUAL_REFINEMENT_BATCH_14_AUDIT.md` — Sales Details deferral (§7 item 1); Sales History/CustomerHistory implicit in remaining lists
- `docs/UI_VISUAL_REFINEMENT_BATCH_14_PR_SUMMARY.md` — helper definitions and deferred Sales History
- `docs/UI_LIST_CHROME_BATCH_15_PLAN.md` — Batch 15 deferrals for Sales History, CustomerHistory, Pricing history
- `docs/UI_LIST_CHROME_BATCH_15_PR_SUMMARY.md` — confirmed deferred pages list
- `src/VPureLux.Web/wwwroot/global-styles.css` — Batch 14 helper definitions

---

**Status:** Batch 16B.1 audit/plan **complete**. Awaiting Batch 16B.2 Sales history/detail chrome.
