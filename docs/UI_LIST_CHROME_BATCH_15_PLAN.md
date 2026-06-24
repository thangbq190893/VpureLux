# Batch 15 — Remaining List Page Chrome — Audit & Implementation Plan

**Document type:** Audit / implementation plan (no code changes in 15.1)  
**Branch:** `feature/ui-list-chrome-batch-15`  
**Base branch:** `main` (@ `99adf48`)  
**Audit date:** 2026-06-24  
**Owner:** Cursor Agent (Composer 2.5)

---

## 1. Executive summary

Batch 13 established VPURELUX branding and inquiry scaffolding. Batch 14 defined and implemented (on `feature/ui-visual-refinement-batch-14`) a **token-driven dense ERP visual system** for Catalog lists and key inquiry pages. Several **menu-visible list/index pages** still use legacy patterns: bare `<h2>`, monolithic `abp-card` headers, non-dense tables, full-size toolbar buttons, or Bootstrap `list-group` hubs.

Batch 15 applies **existing Batch 14 opt-in helpers** (class-only Razor changes) to the remaining list/index surfaces:

- **Pricing** Index (tabbed tables)
- **Customers** and **CustomerGroups** Index
- **BOM** Index (product launcher) and **BOM Product** history list
- **Inventory** hub and **Warehouses** admin list

**Prerequisite:** `main` currently includes only `docs/UI_VISUAL_REFINEMENT_BATCH_14_AUDIT.md` from PR #11. Batch 14 **implementation** commits (`43e0638` … `481f4e3` — tokens, dense tables, toolbars, Catalog/inquiry chrome) are **not yet on `main`**. Before Batch 15.2 Razor work, merge or cherry-pick Batch 14 implementation into this branch so `global-styles.css` contains `.vpl-page`, `.vpl-card-dense`, `.vpl-table-dense`, `.vpl-btn-toolbar`, and `.vpl-empty-state`.

No backend, permission, route, handler, or business-rule changes are in scope.

---

## 2. Exact branch name and base branch

| Item | Value |
|------|--------|
| **Working branch** | `feature/ui-list-chrome-batch-15` |
| **Base branch** | `main` @ `99adf48` (Merge PR #11 — Batch 14 audit doc) |
| **Upstream Batch 14 implementation** | `feature/ui-visual-refinement-batch-14` @ `a613436` (recommended merge/cherry-pick before 15.2) |

---

## 3. Pages inspected

All paths under `src/VPureLux.Web/Pages/` plus `wwwroot/global-styles.css` and Batch 14 docs.

| Page | Route area | Inspected |
|------|------------|-----------|
| `Pricing/Index.cshtml` | Pricing | Yes |
| `Customers/Index.cshtml` | Customers | Yes |
| `CustomerGroups/Index.cshtml` | Customer groups | Yes |
| `Bom/Index.cshtml` | BOM launcher | Yes |
| `Bom/Product.cshtml` | BOM product history list | Yes |
| `Inventory/Index.cshtml` | Inventory hub | Yes |
| `Inventory/Warehouses.cshtml` | Warehouse admin | Yes |
| `Sales/History.cshtml` | Sales secondary list | Yes (deferred) |
| `Sales/CustomerHistory.cshtml` | Sales report list | Yes (deferred) |
| `Pricing/Components/History.cshtml` | Pricing detail | Yes (out of scope — not index) |
| `Pricing/Products/History.cshtml` | Pricing detail | Yes (out of scope — not index) |
| `Catalog/*/Index.cshtml` | Catalog | Yes — Batch 14 target (already on feature branch) |
| `Inventory/Ledger|Balances|Lots.cshtml` | Inventory inquiry | Yes — Batch 14 target |
| `Sales/Index.cshtml`, `Audit/Index.cshtml` | Inquiry lists | Yes — Batch 14 target |
| `Index.cshtml` (Home) | Home launcher | Yes — explicitly out of Batch 15 scope |

---

## 4. Remaining list/index pages found

### In scope — Batch 15 primary

| # | File | Menu-visible | Legacy pattern |
|---|------|--------------|----------------|
| 1 | `Pages/Pricing/Index.cshtml` | Yes | Bare `<h2 class="mb-4">`; `abp-tabs`; two default `abp-table`; `btn-outline-primary` row actions |
| 2 | `Pages/Customers/Index.cshtml` | Yes | Monolithic `abp-card` + `h2` in header; GET filter; default table; `btn-primary` create |
| 3 | `Pages/CustomerGroups/Index.cshtml` | Yes | Same as Customers |
| 4 | `Pages/Bom/Index.cshtml` | Yes | Single `abp-card` + header `h2`; POST product picker (no table) |
| 5 | `Pages/Bom/Product.cshtml` | Yes (via BOM flow) | Monolithic card + pricing context block + default table + mixed row buttons |
| 6 | `Pages/Inventory/Warehouses.cshtml` | Yes | Two default cards (create form + list table); no page header pattern |
| 7 | `Pages/Inventory/Index.cshtml` | Yes | Bare `<h2>` + Bootstrap `list-group` hub (no card/table) |

### Deferred — secondary / non-index (document only)

| File | Reason deferred |
|------|-----------------|
| `Pages/Sales/History.cshtml` | Secondary list; bare `h2` + table; can follow 15.x pattern in a later batch |
| `Pages/Sales/CustomerHistory.cshtml` | Report layout with summary stat cards + conditional table; higher layout risk |
| `Pages/Pricing/Components/History.cshtml` | Detail/history page, not module index |
| `Pages/Pricing/Products/History.cshtml` | Detail/history page, not module index |
| `Pages/Index.cshtml` (Home) | Module launcher grid — separate Home density batch |
| Already Batch 14 (on feature branch) | Catalog Products/Components, Inventory inquiry trio, Sales Index, Audit Index |

---

## 5. Current visual pattern per page

| Page | Header | Container | Filter / input | Table | Toolbar / actions |
|------|--------|-----------|----------------|-------|-------------------|
| **Pricing Index** | Bare `h2 mb-4` | `abp-tabs` (no card) | N/A (tabs only) | 2× default `abp-table` | Permission-gated `btn-sm btn-outline-primary` Open History |
| **Customers Index** | `h2` inside `abp-card-header` | Monolithic card | GET search + status in card body | Default striped table | `btn-primary` create; dropdown row actions (`btn-sm outline-secondary`) |
| **CustomerGroups Index** | Same as Customers | Same | Same | Same | Same pattern |
| **BOM Index** | `h2` in card header | Single card | POST product select + submit | None | `abp-button Primary` Open History |
| **BOM Product** | `h2` in card header row + create btn | Monolithic card | Product context block | Default table | Mixed `outline-secondary` / `outline-primary` / `abp-button` row actions |
| **Inventory hub** | Bare `h2 mb-4` | None | N/A | N/A | `list-group-item` links with permission `@if` |
| **Warehouses** | `h2` in first card header | Two stacked cards | POST create form in card 1 | Default table in card 2 | `btn-primary` create; `btn-sm outline-secondary` activate/deactivate |

**CSS on `main` today:** Batch 13 `.vpl-inquiry-*` helpers only. Batch 14 token helpers (`.vpl-page`, `.vpl-card-dense`, etc.) documented in audit but **not present in `global-styles.css` on `main`** until Batch 14 implementation is merged.

---

## 6. Proposed target pattern per page

Apply **class-only** changes using Batch 14 helpers. Do **not** change columns, handlers, or permission blocks.

| Page | `.vpl-page` | `.vpl-page-header` + `.vpl-page-title` | `.vpl-btn-toolbar` | `.vpl-card-dense` | `.vpl-table-dense` | `.vpl-empty-state` | Notes |
|------|-------------|----------------------------------------|--------------------|-------------------|--------------------|--------------------|-------|
| **Pricing Index** | Yes | Yes — title only (no create on index) | No | Optional wrapper around each tab content or single card per tab | Both tables | No (rows always present) | Keep `abp-tabs`; row actions → `btn-sm btn-outline-secondary` |
| **Customers Index** | Yes | Yes — title + create in toolbar | Yes on create | Yes on list card | Yes | No | Mirror Catalog 14.5a: external header, dense card body |
| **CustomerGroups Index** | Yes | Same as Customers | Yes | Yes | Yes | No | Same as Customers |
| **BOM Index** | Yes | Yes — title only | No | Yes on picker card | N/A | No | Keep POST form unchanged |
| **BOM Product** | Yes | Yes — title + create in toolbar | Yes | Yes on history card | Yes on version table | Optional `text-muted` empty → `.vpl-empty-state` only if zero versions | Keep pricing context block inside card body |
| **Inventory hub** | Yes | Yes — title only | No | Yes — wrap `list-group` in one dense card | N/A | No | Replace bare list-group page chrome only; keep links/permissions |
| **Warehouses** | Yes | Yes — page title (warehouses) | No | Yes on both cards (create + list) | Yes on warehouse table | No | Keep two-card split (create vs list) |

**No new CSS** expected unless Batch 14 helpers are missing after merge (then land Batch 14.2 block first, not new Batch 15 tokens).

**Explicitly excluded from Batch 15 formatting changes:** Pricing `N0` + currency display (separate approval per Batch 14 deferral).

---

## 7. Exact files proposed for implementation

| Batch | Files |
|-------|-------|
| **Prerequisite** | Merge/cherry-pick from `feature/ui-visual-refinement-batch-14`: `global-styles.css`, Batch 14-touched Razor (optional if merging whole branch) |
| **15.2 Pricing** | `src/VPureLux.Web/Pages/Pricing/Index.cshtml` |
| **15.3 Customers** | `src/VPureLux.Web/Pages/Customers/Index.cshtml`, `src/VPureLux.Web/Pages/CustomerGroups/Index.cshtml` |
| **15.4 BOM / Inventory** | `src/VPureLux.Web/Pages/Bom/Index.cshtml`, `src/VPureLux.Web/Pages/Bom/Product.cshtml`, `src/VPureLux.Web/Pages/Inventory/Index.cshtml`, `src/VPureLux.Web/Pages/Inventory/Warehouses.cshtml` |
| **15.5 Wrap-up** | `docs/UI_LIST_CHROME_BATCH_15_PR_SUMMARY.md` (future); optional status line in this plan |
| **Tests (only if assertions break)** | `test/VPureLux.Web.Tests/Pages/PricingPagesTests.cs`, `CustomerPagesTests.cs`, `BomPagesTests.cs`, `InventoryPagesTests.cs` |

---

## 8. Explicit files that must not be touched

| Category | Paths / examples |
|----------|------------------|
| **Backend** | `src/VPureLux.Domain/**`, `Application/**`, `EntityFrameworkCore/**`, `HttpApi/**` |
| **PageModel logic** | All `*.cshtml.cs` under target modules |
| **JavaScript** | `Customers/Index.js`, `CustomerGroups/Index.js`, `Inventory/Warehouses.js`, `Bom/BomProduct.js`, `Catalog/CatalogIndex.js` |
| **Create / edit / details / modals** | `Customers/Create*.cshtml`, `Edit*.cshtml`, `Details*.cshtml`; same for CustomerGroups, BOM, Pricing Create/History detail flows, Inventory Receipt/Issue/Adjustment |
| **Already polished (Batch 14 branch)** | `Catalog/Products|Components/Index.cshtml`, `Inventory/Ledger|Balances|Lots.cshtml`, `Sales/Index.cshtml`, `Sales/Details.cshtml`, `Audit/Index.cshtml` — avoid drive-by edits in Batch 15 |
| **Home / login** | `Pages/Index.cshtml`, `Index.css`, Account pages |
| **Themes / libs** | `Themes/**`, `wwwroot/libs/**` |
| **Formatting (unless approved)** | Money/date/quantity formatters in Razor `@functions` or shared formatters |

---

## 9. Permission / business-rule safety notes

| Concern | Batch 15 approach |
|---------|-------------------|
| Permission gates removed | **No** — preserve all `@if (Model.Can*)` and `AuthorizeAsync` blocks |
| Pricing history links | **No change** — `canViewComponentHistory` / `canViewProductHistory` unchanged |
| Customer status activate/deactivate | **No change** — dropdown forms and handlers preserved |
| BOM publish/archive/create | **No change** — row action forms and `CanCreate`/`CanPublish`/`CanArchive` preserved |
| Warehouse activate/deactivate | **No change** — POST handlers and confirm attributes preserved |
| Inventory hub links | **No change** — `CanManageWarehouses`, `CanReceive`, `CanIssue`, `CanAdjust`, `CanViewLedger` gates preserved |
| Column visibility (Sales profit, etc.) | **N/A** on Batch 15 pages; do not add/remove columns |
| Data calculations | **No change** — display-only class additions |

---

## 10. Proposed implementation batches

### Batch 15.2 — Pricing list chrome

**Goal:** Align `Pricing/Index.cshtml` with shared page header and dense tables.

**Touch:** `Pages/Pricing/Index.cshtml` only.

**Changes:** `.vpl-page` wrapper; `.vpl-page-header` + `.vpl-page-title`; `.vpl-table-dense` on both tab tables; standardize Open History buttons to compact `btn-outline-secondary` (permission gates unchanged). Optionally wrap each tab panel content in `.vpl-card-dense` if markup allows without breaking tabs.

**Validation:** `dotnet test ... --filter FullyQualifiedName~Pricing`

---

### Batch 15.3 — Customer / CustomerGroup list chrome

**Goal:** Match Catalog 14.5a pattern on Customers and CustomerGroups index pages.

**Touch:** `Pages/Customers/Index.cshtml`, `Pages/CustomerGroups/Index.cshtml`.

**Changes:** `.vpl-page`; external `.vpl-page-header` + `.vpl-page-title`; `.vpl-btn-toolbar` on create; `.vpl-card-dense` on list card; `.vpl-table-dense`; preserve `data-customer-*` / `data-customer-group-*` attributes and GET filter form.

**Validation:** `dotnet test ... --filter FullyQualifiedName~Customer`

---

### Batch 15.4 — BOM / Warehouse / Inventory hub chrome

**Goal:** Dense chrome on BOM launcher, BOM product history list, inventory hub, and warehouses admin.

**Touch:** `Pages/Bom/Index.cshtml`, `Pages/Bom/Product.cshtml`, `Pages/Inventory/Index.cshtml`, `Pages/Inventory/Warehouses.cshtml`.

**Changes:** `.vpl-page` + header/title on each; `.vpl-card-dense` on cards; `.vpl-table-dense` on BOM Product and Warehouses tables; `.vpl-btn-toolbar` on BOM Product create; inventory hub — dense card wrapping existing `list-group` (links unchanged).

**Validation:** `dotnet test ... --filter "FullyQualifiedName~Bom|FullyQualifiedName~Inventory"`

---

### Batch 15.5 — Final validation and PR summary

**Goal:** Full Web.Tests, build, `docs/UI_LIST_CHROME_BATCH_15_PR_SUMMARY.md`.

**Checklist:** Pricing Index, Customers, CustomerGroups, BOM Index/Product, Inventory hub, Warehouses; confirm Batch 14 pages unchanged if merged on same branch.

---

## 11. Non-goals

- No backend, Domain, Application, EF Core, migration, or DB schema changes
- No API contract or PageModel query changes
- No business-rule changes (Pricing, Customers, BOM, Inventory warehouse rules)
- No permission gate changes or data exposure changes
- No route, handler, or form field name / binding changes
- No create/edit/details/modal behavior or JS changes
- No hiding columns or removing actions
- No Vietnamese/`₫` formatting changes on Pricing unless separately approved
- No Home launcher redesign
- No new UI framework or LeptonX rewrite
- No global CSS overrides of all `.card` / `.table` / `.btn`
- No Sales History / CustomerHistory in Batch 15 (documented deferral)

---

## 12. Recommended first implementation batch

**Start with prerequisite + Batch 15.2 — Pricing list chrome.**

**Why:**

1. **Prerequisite:** Ensure Batch 14 CSS helpers exist on the branch before any Razor class work.
2. Pricing Index is a **high-visibility menu page** called out in Batch 14 deferrals (`docs/UI_VISUAL_REFINEMENT_BATCH_14_AUDIT.md` §7 item 4).
3. Single-file scope (`Pricing/Index.cshtml`) with no modal JS coupling — lowest risk after prerequisite merge.
4. Tabbed layout validates that `.vpl-table-dense` works without monolithic card refactor.
5. Unblocks Customer/BOM batches using the same proven helper set.

**Exit criteria for 15.2:**

- Batch 14 helpers present in `global-styles.css`
- Pricing Index uses `.vpl-page`, `.vpl-page-header`, `.vpl-page-title`, `.vpl-table-dense`
- `dotnet build VPureLux.slnx` succeeds
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --filter FullyQualifiedName~Pricing` — all pass

---

## References

- `docs/UI_VISUAL_REFINEMENT_BATCH_14_AUDIT.md` — Batch 14 deferrals (Customers, Pricing, BOM, Warehouses, Inventory hub)
- `feature/ui-visual-refinement-batch-14` @ `a613436` — Batch 14 implementation (tokens + polished pages)
- `src/VPureLux.Web/wwwroot/global-styles.css` — target helper definitions after prerequisite merge

---

**Status:** Batch 15.1 audit/plan **complete**. Batch 15.2A (Pricing, Customers, CustomerGroups list chrome) **complete**. Batch 15.3a (BOM Index, BOM Product list chrome) **complete** — applied `.vpl-page`, `.vpl-page-header`, `.vpl-page-title`, `.vpl-card-dense`, `.vpl-table-dense`, and `.vpl-btn-toolbar` to BOM launcher and product history pages. Next: Batch 15.4 Inventory hub / Warehouses chrome.
