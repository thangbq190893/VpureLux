# Batch 14 — VPURELUX UI Visual Refinement — PR Summary

**Title:** feat(web): VPURELUX UI visual refinement — dense ERP tokens, tables, toolbars, page chrome  
**Branch:** `feature/ui-visual-refinement-batch-14`  
**Base branch:** `main` (@ `eaab285`)  
**Document type:** Merge-request / pull-request summary

---

## Scope summary

Batch 14 improves **visual density and consistency** across key VPURELUX ERP list/inquiry pages after Batch 13 branding. All work is **Web Razor/CSS presentation only** — opt-in helper classes and class additions on existing markup.

Implemented phases:

| Phase | Focus |
|-------|--------|
| 14.1 | Audit and design-system plan |
| 14.2 | Global `--vpl-*` tokens and opt-in helpers in `global-styles.css` |
| 14.3 | `.vpl-table-dense` on key lists; compact catalog thumbnails (40×40px) |
| 14.4 | `.vpl-btn-toolbar` and compact row/action buttons |
| 14.5a | Catalog Products/Components — `.vpl-page` header + `.vpl-card-dense` |
| 14.5b | Inventory/Sales/Audit inquiry pages — shared page chrome + dense cards |

**No backend, Domain, Application, EF Core, migration, API contract, permission, or business-rule changes.**

Visual changes are **Razor class additions** and **CSS helpers only**. Batch 13 branding (logo, login gradients, footer, Vietnamese formatting) is preserved.

---

## Commit list (oldest → newest)

| Commit | Message |
|--------|---------|
| `331bef4` | docs: add Batch 14 UI visual refinement audit |
| `43e0638` | feat(web): add VPURELUX visual refinement tokens |
| `830d9b8` | feat(web): apply dense table styling to key lists |
| `731ea86` | feat(web): polish toolbar and row action buttons |
| `92da3b7` | feat(web): polish Catalog list page chrome |
| `481f4e3` | feat(web): polish inquiry page chrome and card density |
| _(wrap-up, latest)_ | docs: add Batch 14 UI visual refinement PR summary |

---

## Files changed by area (`main...HEAD`)

| Area | Files |
|------|-------|
| **Docs** | `docs/UI_VISUAL_REFINEMENT_BATCH_14_AUDIT.md`, `docs/UI_VISUAL_REFINEMENT_BATCH_14_PR_SUMMARY.md` |
| **Theme / tokens** | `wwwroot/global-styles.css` |
| **Catalog lists** | `Pages/Catalog/Products/Index.cshtml`, `Pages/Catalog/Components/Index.cshtml` |
| **Inventory inquiry** | `Pages/Inventory/Ledger.cshtml`, `Balances.cshtml`, `Lots.cshtml` |
| **Sales** | `Pages/Sales/Index.cshtml`, `Pages/Sales/Details.cshtml` |
| **Audit inquiry** | `Pages/Audit/Index.cshtml` |

**Total:** 11 paths vs `main` before wrap-up doc commit (1 audit doc added, 9 modified Razor/CSS, 1 PR summary added at wrap-up).

**Not touched:** Home (`Index.cshtml`/`Index.css`), login/account pages, Catalog create/edit/details/modals, Inventory posting forms, Pricing, Customers, BOM, backend layers, tests (no test file changes required).

---

## What changed

### 14.2 — Global visual tokens (`global-styles.css`)

- CSS variables: `--vpl-radius-*`, `--vpl-space-*`, `--vpl-border-subtle`, `--vpl-shadow-subtle`, `--vpl-font-size-table`, `--vpl-font-size-meta`
- Opt-in helpers: `.vpl-card-dense`, `.vpl-table-dense`, `.vpl-btn-toolbar`, `.vpl-page`, `.vpl-page-header`, `.vpl-page-title`, `.vpl-page-subtitle`, `.vpl-empty-state`, `.vpl-catalog-thumbnail*`
- Batch 13 rules unchanged: logo variables, login gradients, `.vpl-inquiry-*`

### 14.3 — Table density

- `class="vpl-table-dense"` on `<abp-table>` for Catalog, Inventory inquiry trio, Sales Index, Audit Index
- Catalog thumbnails reduced from 56px to 40px via `.vpl-catalog-thumbnail` (reusable CSS; inline size styles removed)

### 14.4 — Button / action polish

- `.vpl-btn-toolbar` on Catalog create headers, Sales Index toolbar, Audit Index toolbar, Sales Details action block
- Sales Index row Details: `btn-outline-primary` → `btn-sm btn-outline-secondary`
- Sales Details: `btn-sm` on Edit/Confirm/Cancel/Back (semantic colors preserved)

### 14.5a — Catalog list chrome

- `.vpl-page` + `.vpl-page-header` + `.vpl-page-title` external to card
- `.vpl-card-dense` on single list card (filter + table unchanged structurally)

### 14.5b — Inquiry page chrome

- Combined `.vpl-page vpl-inquiry-page` wrapper
- `.vpl-page-header` / `.vpl-page-title` (replacing inquiry-only title classes on target pages)
- `.vpl-card-dense` on filter and results cards where present
- Filter/results split, forms, and table content unchanged

---

## What explicitly did not change

- **Backend / Domain / Application / EF Core / migrations / DB schema**
- **API contracts** and **PageModel query logic**
- **Routes, handlers, form field names, GET/POST bindings**
- **Permissions** and authorization policies
- **Business rules:** Inventory posting, FIFO, balances, lots, ledger calculations
- **Sales:** order create/confirm/cancel, revenue/cost/profit **calculations** and **visibility gates**
- **Catalog/Pricing** module behavior; modal/create/edit/details/image/status flows
- **Audit** export/report logic and permission gates
- **Batch 13** Vietnamese display formatting (money, quantity, dates)
- **JavaScript** (`CatalogIndex.js`, `SalesProductContext.js`, etc.)
- **Home** welcome page, **login** shell, **LeptonX** package sources
- **Tests** — no Web.Tests source changes; full suite passes unchanged

Layout and density changes are **class-only** on existing Razor structure. No global overrides of all `.card`, `.table`, or `.btn` elements.

---

## Permission / business-rule safety notes

| Concern | Status |
|---------|--------|
| Permission gates removed | **No** — all `@if` / `Authorize` checks preserved |
| Sales cost/profit exposed without permission | **No** — `canViewCost` / `canViewProfit` unchanged on Details |
| Catalog pricing visibility | **No change** — `CanViewPricingContext` still gates price columns |
| Inventory posting / FIFO | **No change** — inquiry display only |
| Catalog/Pricing business behavior | **No change** |
| Audit export gated | **No change** — `CanExport` preserved |
| New or removed UI actions | **No** — same links, dropdowns, handlers |

---

## Test results

Wrap-up validation (2026-06-24):

```bash
dotnet build VPureLux.slnx --no-restore
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build
```

| Check | Result |
|-------|--------|
| Build | **Succeeded** — 0 errors, 3 pre-existing warnings (Domain nullability, Test SDK entry point) |
| Full Web.Tests | **Passed** — 120/120, 0 failed, 0 skipped (~3m 33s) |

Per-phase filters were also run during implementation: Catalog (20), Inventory (31), Sales (15), Audit (11).

---

## Known deferred items

Not in this MR (see `docs/UI_VISUAL_REFINEMENT_BATCH_14_AUDIT.md`):

- **Home launcher density** — module card grid still uses Batch 13 layout (`Index.cshtml` / `Index.css`)
- **Empty-state polish** — `.vpl-empty-state` helper exists but not wired; Inventory inquiry pages still use `alert alert-light border`
- **Sales Details** summary card alignment with inquiry chrome (toolbar only touched in 14.4)
- **Remaining list pages** — Customers, CustomerGroups, BOM, Pricing Index, Inventory hub
- **Pricing display alignment** — Pricing Index still uses `N0` + currency code vs Catalog/Sales `₫` formatting
- **Login gradient** desaturation (optional CSS-only tweak)
- **Dark-theme** token verification pass across all refined pages

---

## Suggested PR/MR description

```markdown
## Summary

Batch 14 VPURELUX UI visual refinement — dense internal ERP styling on key list/inquiry pages.

Scoped Web-only changes:
- Global `--vpl-*` CSS tokens and opt-in density helpers
- Dense tables and compact catalog thumbnails on Catalog/Inventory/Sales/Audit lists
- Compact toolbar and row action buttons
- Shared `.vpl-page` header + `.vpl-card-dense` on Catalog and inquiry pages

Builds on Batch 13 branding and Vietnamese formatting without changing behavior.

## Safety

- No backend, Domain, Application, EF, migration, or API changes
- No business rule, route, handler, or permission changes
- Sales cost/profit remain permission-gated
- Visual changes are Razor/CSS class-only

## Test plan

- [x] `dotnet build VPureLux.slnx --no-restore`
- [x] `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build`
- [ ] Manual smoke: Catalog Products/Components, Inventory Ledger/Balances/Lots, Sales Index/Details, Audit Index

## Deferred

Home launcher density, empty-state wiring, remaining list pages — see `docs/UI_VISUAL_REFINEMENT_BATCH_14_AUDIT.md`
```

---

**Wrap-up status:** PR summary complete; ready for MR/PR from `feature/ui-visual-refinement-batch-14` → `main`.
