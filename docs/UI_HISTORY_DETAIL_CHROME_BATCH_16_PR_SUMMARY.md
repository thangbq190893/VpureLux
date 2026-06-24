# Batch 16B — Sales/Pricing History & Detail Page Chrome — PR Summary

**Title:** Batch 16B: Sales and Pricing history/detail page chrome polish  
**Branch:** `feature/ui-history-detail-chrome-batch-16`  
**Base branch:** `main` (@ `7810f53`)  
**Document type:** Merge-request / pull-request summary

---

## Scope summary

Batch 16B applies **existing Batch 14/15 opt-in visual helpers** to deferred **Sales history/detail** and **Pricing history/detail** pages. All work is **Web Razor presentation only** — wrapper markup and CSS class additions on existing structure.

Implemented phases:

| Phase | Focus |
|-------|--------|
| 16B.1 | Audit and implementation plan (`UI_HISTORY_DETAIL_CHROME_BATCH_16_PLAN.md`) |
| 16B.2a | Sales History list chrome |
| 16B.2b | Sales CustomerHistory report chrome |
| 16B.2c | Sales Details order chrome |
| 16B.3 | Pricing Components/Products History chrome |
| 16B.4 | Full Web.Tests validation and this PR summary |

**No backend, Domain, Application, EF Core, migration, API contract, permission, or business-rule changes.**

Visual changes are **Razor class/layout additions only** using helpers already defined in Batch 14 `global-styles.css`. **Sales and Pricing date/money/number formatting was not changed.** Batch 13 branding and prior Batch 14/15 list/inquiry polish are preserved.

---

## Commit list (oldest → newest on branch vs `main`)

| Commit | Message |
|--------|---------|
| `0bdd105` | docs: add Batch 16 history detail chrome plan |
| `d83960a` | feat(web): polish Sales history page chrome |
| `7d159da` | feat(web): polish Sales customer history chrome |
| `0859986` | feat(web): polish Sales details page chrome |
| `3959121` | feat(web): polish Pricing history page chrome |
| _(wrap-up, latest)_ | docs: add Batch 16 history detail chrome PR summary |

---

## Files changed by area (`main...HEAD`)

| Area | Files |
|------|-------|
| **Docs** | `docs/UI_HISTORY_DETAIL_CHROME_BATCH_16_PLAN.md`, `docs/UI_HISTORY_DETAIL_CHROME_BATCH_16_PR_SUMMARY.md` |
| **Sales history/detail** | `Pages/Sales/History.cshtml`, `CustomerHistory.cshtml`, `Details.cshtml` |
| **Pricing history** | `Pages/Pricing/Components/History.cshtml`, `Pages/Pricing/Products/History.cshtml` |

**Total:** 7 paths vs `main` before wrap-up doc commit (1 plan doc added, 5 Razor pages modified, 1 PR summary added at wrap-up).

**Not touched:** `global-styles.css`, PageModel `.cshtml.cs` files, JavaScript, Sales Index/Create/Edit, Pricing Index/Create, other modules, backend layers, tests (no test file changes required).

---

## What changed

### Shared pattern

- `.vpl-page` page wrapper
- `.vpl-page-header` + `.vpl-page-title` (localization keys unchanged)
- `.vpl-card-dense` on list/summary/history containers
- `.vpl-table-dense` on tabular content where applicable
- `.vpl-btn-toolbar` on existing action headers (Details, Pricing history Create)
- `.vpl-page-subtitle` / `.vpl-empty-state` where plan specified (CustomerHistory, Pricing history)

### 16B.2 — Sales

| Page | Chrome applied |
|------|----------------|
| **History** | Page header; dense card + table; Details btn `outline-secondary` |
| **CustomerHistory** | Dense filter card; subtitle for selected customer; dense summary stat cards; `.vpl-empty-state` for no purchases; dense results table |
| **Details** | Page chrome on `#SalesDetailsPage`; existing toolbar preserved; dense summary card + dense line-item table |

### 16B.3 — Pricing history

| Page | Chrome applied |
|------|----------------|
| **Components/History** | Header + toolbar; context subtitle; dense current-version and lookup cards; dense timeline card; `.vpl-empty-state` for no-version states |
| **Products/History** | Same pattern as component history |

Pricing **vertical timeline** markup preserved (not converted to table).

---

## What explicitly did not change

- **Backend / Domain / Application / EF Core / migrations / DB schema**
- **API contracts** and **PageModel query logic**
- **Routes, handlers, form field names, GET/POST bindings**
- **Permissions** and authorization policies — no gates removed
- **Sales order lifecycle** — Confirm/Cancel/Edit behavior unchanged
- **Sales revenue/cost/profit calculations** and **visibility gates** (`ViewCost`, `ViewProfit`)
- **Sales CustomerHistory** aggregations, filter logic, conditional rendering
- **Pricing lookup/history behavior** — GET lookup, version timeline, Create permission unchanged
- **Pricing date/money/number formatting** — `N2`, `PricingDateUi.Format`, currency codes preserved
- **Pricing component/product price semantics**
- **Create/edit** pages (Sales Create/Edit, Pricing Create)
- **JavaScript** (`Sales/Details.js`, etc.)
- **Batch 14/15** already-polished pages (Sales Index, Pricing Index, Catalog, etc.)
- **Tests** — no Web.Tests source changes

Layout changes are **class-only** on existing Razor structure. No new CSS tokens or global overrides.

---

## Permission / business-rule safety notes

| Concern | Status |
|---------|--------|
| Permission gates removed | **No** — all `@if` / `AuthorizeAsync` checks preserved |
| Sales profit on History | **No change** — `canViewProfit` column gate preserved |
| Sales cost/profit on Details | **No change** — `canViewCost` / `canViewProfit` column gates preserved |
| Sales Confirm/Cancel/Edit | **No change** — handlers, idempotency key, `data-sales-action-form` preserved |
| CustomerHistory report data | **No change** — summary cards and product table columns preserved |
| Pricing Create on history pages | **No change** — `Model.CanCreate` preserved |
| Pricing historical lookup | **No change** — GET form, validation, lookup result logic preserved |
| History entries hidden | **No** — all timeline/table rows preserved |
| New or removed UI actions | **No** |

**Out of scope for this visual PR:** any separate permission/security hardening beyond preserving existing gates.

---

## Test results

Wrap-up validation (2026-06-24):

```bash
dotnet build VPureLux.slnx --no-restore -m:2
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1
```

| Check | Result |
|-------|--------|
| Build | **Succeeded** — 0 errors, 3 pre-existing warnings (Domain nullability, Test SDK entry point) |
| Full Web.Tests | **Passed** — 120/120, 0 failed, 0 skipped (~5m 8s) |

Per-phase filters run during implementation:

| Filter | Result |
|--------|--------|
| `FullyQualifiedName~Sales` | 15/15 passed (each 16B.2 batch) |
| `FullyQualifiedName~Pricing` | 15/15 passed (16B.3) |

---

## Known deferred items

Not in this MR (see `docs/UI_HISTORY_DETAIL_CHROME_BATCH_16_PLAN.md`):

| Item | Batch |
|------|-------|
| **Home launcher density** | 16A |
| **Empty-state wiring outside Sales/Pricing history/detail** (e.g. Inventory inquiry alerts) | 16A |
| **Remaining form/create/edit density polish** (Sales Create/Edit, Pricing Create, posting forms) | Future |
| **Pricing display alignment** — Index/history `N0`/`N2` + currency vs Catalog/Sales `₫` | Separate approval |
| **Dark-theme verification** across refined pages | Future |

---

## Suggested PR/MR description

**Title:** Batch 16B: Sales and Pricing history/detail page chrome polish

```markdown
## Summary

Batch 16B applies Batch 14/15 VPURELUX page chrome helpers to Sales and Pricing history/detail pages:

- Sales History, CustomerHistory, Details
- Pricing Components/Products History

Web-only Razor class/layout changes. No backend, permission, route, handler, or formatting changes.

## Checklist

- [x] Build passed
- [x] Web.Tests passed
- [x] No backend/domain/application/EF/migration/API changes
- [x] No business-rule changes
- [x] No permission gates removed
- [x] No route/handler/form binding changes
- [x] No Sales revenue/cost/profit calculation changes
- [x] No Sales cost/profit visibility changes
- [x] No Pricing lookup/history behavior changes
- [x] No Pricing date/money/number formatting changes
- [x] Visual changes are Razor class/layout-only plus documentation

## Test plan

- [x] `dotnet build VPureLux.slnx --no-restore`
- [x] `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build`
- [ ] Manual smoke: Sales History/CustomerHistory/Details, Pricing component/product history (lookup + timeline)

## Deferred

Batch 16A (Home launcher, global empty-state wiring), create/edit form density — see `docs/UI_HISTORY_DETAIL_CHROME_BATCH_16_PLAN.md`
```

---

**Wrap-up status:** PR summary complete; ready for MR/PR from `feature/ui-history-detail-chrome-batch-16` → `main`.
